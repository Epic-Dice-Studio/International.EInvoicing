using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using International.EInvoicing.Validation.Schematron.XPath;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Validation.Schematron;

/// <summary>
/// Runs a Schematron rule set against a document.
/// </summary>
/// <remarks>
/// Schematron's rule is that within a pattern, the <em>first</em> rule whose context matches a node claims
/// it, and later rules in that pattern do not see it. Missing that makes a validator report rules that should
/// never have fired.
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "An instance API so a caller can replace this validator through the registry.")]
public sealed class SchematronValidator
{
    /// <summary>Validates <paramref name="document"/> against <paramref name="ruleSet"/>.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public ValidationReport Validate(XDocument document, SchematronRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(ruleSet);

        var evaluator = new XPathEvaluator(ruleSet.Namespaces);
        var messages = new List<ValidationMessage>();

        foreach (SchematronPattern pattern in ruleSet.Patterns)
        {
            RunPattern(pattern, document, evaluator, ruleSet.Name, messages);
        }

        return new ValidationReport(
            messages,
            [new RuleSetOutcome(ruleSet.Name, ruleSet.Version, Ran: true)]);
    }

    /// <summary>Validates XML text against <paramref name="ruleSet"/>.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public ValidationReport Validate(string xml, SchematronRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(xml);

        using var reader = SecureXml.CreateReader(xml, DocumentLimits.Unlimited);
        return Validate(XDocument.Load(reader, LoadOptions.SetLineInfo), ruleSet);
    }

    /// <summary>Validates a stream against <paramref name="ruleSet"/>. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public ValidationReport Validate(Stream stream, SchematronRuleSet ruleSet, DocumentLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = SecureXml.CreateReader(stream, limits ?? DocumentLimits.Default);
        return Validate(XDocument.Load(reader, LoadOptions.SetLineInfo), ruleSet);
    }

    private static void RunPattern(
        SchematronPattern pattern,
        XDocument document,
        XPathEvaluator evaluator,
        string ruleSetName,
        List<ValidationMessage> messages)
    {
        var claimed = new HashSet<object>();

        foreach (SchematronRule rule in pattern.Rules)
        {
            IReadOnlyList<object> nodes = Select(rule.Context, document, evaluator);

            foreach (object node in nodes)
            {
                if (!claimed.Add(node))
                {
                    continue;
                }

                RunRule(rule, node, document, evaluator, ruleSetName, messages);
            }
        }
    }

    private static void RunRule(
        SchematronRule rule,
        object node,
        XDocument document,
        XPathEvaluator evaluator,
        string ruleSetName,
        List<ValidationMessage> messages)
    {
        var context = new XPathContext(node, document, new Dictionary<string, XPathValue>(StringComparer.Ordinal));

        foreach (SchematronAssertion assertion in rule.Assertions)
        {
            bool held;
            try
            {
                held = evaluator.Evaluate(assertion.Test, context).AsBoolean();
            }
            catch (XPathException exception)
            {
                messages.Add(new ValidationMessage(
                    assertion.Identifier,
                    RuleSeverity.Warning,
                    $"This rule could not be evaluated: {exception.Message}")
                {
                    Location = PathOf(node),
                    RuleSet = ruleSetName,
                });

                continue;
            }

            // An assert fires when its test is false; a report fires when its test is true.
            if (held != assertion.IsReport)
            {
                continue;
            }

            messages.Add(new ValidationMessage(assertion.Identifier, assertion.Severity, assertion.Message)
            {
                Location = PathOf(node),
                BusinessTerm = BusinessTermIn(assertion.Identifier, assertion.Message),
                RuleSet = ruleSetName,
            });
        }
    }

    private static IReadOnlyList<object> Select(XPathNode context, XDocument document, XPathEvaluator evaluator)
    {
        var root = new XPathContext(document, document, new Dictionary<string, XPathValue>(StringComparer.Ordinal));

        try
        {
            return evaluator.Evaluate(context, root).Items;
        }
        catch (XPathException)
        {
            return [];
        }
    }

    /// <summary>The business term a rule names, so a caller can point at the field rather than the rule.</summary>
    private static string? BusinessTermIn(string identifier, string message)
    {
        foreach (string source in (string[])[identifier, message])
        {
            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(
                source,
                @"\bB[TG]-\d+(-\d+)?\b",
                System.Text.RegularExpressions.RegexOptions.None,
                TimeSpan.FromSeconds(1));

            if (match.Success)
            {
                return match.Value;
            }
        }

        return null;
    }

    private static string PathOf(object node)
    {
        var segments = new Stack<string>();

        for (XElement? element = node as XElement; element is not null; element = element.Parent)
        {
            segments.Push(element.Name.LocalName);
        }

        return segments.Count == 0 ? "/" : "/" + string.Join('/', segments);
    }
}
