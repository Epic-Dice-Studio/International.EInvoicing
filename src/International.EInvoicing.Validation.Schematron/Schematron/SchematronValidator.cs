using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
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
public sealed partial class SchematronValidator
{
    [GeneratedRegex(@"\bB[TG]-\d+(-\d+)?\b")]
    private static partial Regex BusinessTerm();

    /// <summary>Validates <paramref name="document"/> against <paramref name="ruleSet"/>.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public ValidationReport Validate(XDocument document, SchematronRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(ruleSet);

        var evaluator = new XPathEvaluator(ruleSet.Namespaces, ruleSet.Functions);
        var messages = new List<ValidationMessage>();

        // The rule set's own variables are evaluated once, against the document, and are in scope everywhere.
        IReadOnlyDictionary<string, XPathValue> globals = Evaluate(
            ruleSet.GlobalVariables,
            new XPathContext(document, document, new Dictionary<string, XPathValue>(StringComparer.Ordinal)),
            evaluator);

        int matched = 0;

        foreach (SchematronPattern pattern in ruleSet.Patterns)
        {
            matched += RunPattern(pattern, document, evaluator, ruleSet.Name, globals, messages);
        }

        // A rule set none of whose contexts matched has judged nothing, and saying "valid" would be a lie of
        // the worst kind: it is what a document in the wrong vocabulary looks like — a Slovak tax data
        // document put in front of the ViDA rules, which are the same rules in another namespace.
        return new ValidationReport(
            messages,
            [
                matched > 0
                    ? new RuleSetOutcome(ruleSet.Name, ruleSet.Version, Ran: true)
                    : new RuleSetOutcome(
                        ruleSet.Name,
                        ruleSet.Version,
                        Ran: false,
                        "no rule in this set matched anything in the document"),
            ]);
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

    /// <summary>Evaluates variables in order, each one able to use those declared before it.</summary>
    private static IReadOnlyDictionary<string, XPathValue> Evaluate(
        IReadOnlyList<SchematronVariable> variables,
        XPathContext context,
        XPathEvaluator evaluator)
    {
        if (variables.Count == 0)
        {
            return context.Variables;
        }

        var values = new Dictionary<string, XPathValue>(context.Variables, StringComparer.Ordinal);

        foreach (SchematronVariable variable in variables)
        {
            try
            {
                values[variable.Name] = evaluator.Evaluate(
                    variable.Expression,
                    context with { Variables = values });
            }
            catch (XPathException)
            {
                // A variable that cannot be evaluated leaves the rules using it unevaluable, which they
                // report for themselves. Failing the whole rule set here would hide that.
                values[variable.Name] = XPathValue.Empty;
            }
        }

        return values;
    }

    /// <summary>Runs one pattern, and answers how many nodes its rules claimed.</summary>
    private static int RunPattern(
        SchematronPattern pattern,
        XDocument document,
        XPathEvaluator evaluator,
        string ruleSetName,
        IReadOnlyDictionary<string, XPathValue> globals,
        List<ValidationMessage> messages)
    {
        var claimed = new HashSet<object>();

        foreach (SchematronRule rule in pattern.Rules)
        {
            IReadOnlyList<object> nodes = Select(rule.Context, document, evaluator, globals);

            foreach (object node in nodes)
            {
                if (!claimed.Add(node))
                {
                    continue;
                }

                RunRule(rule, node, document, evaluator, ruleSetName, globals, messages);
            }
        }

        return claimed.Count;
    }

    private static void RunRule(
        SchematronRule rule,
        object node,
        XDocument document,
        XPathEvaluator evaluator,
        string ruleSetName,
        IReadOnlyDictionary<string, XPathValue> globals,
        List<ValidationMessage> messages)
    {
        var context = new XPathContext(node, document, globals);
        context = context with { Variables = Evaluate(rule.Variables, context, evaluator) };

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

    private static IReadOnlyList<object> Select(
        XPathNode context,
        XDocument document,
        XPathEvaluator evaluator,
        IReadOnlyDictionary<string, XPathValue> globals)
    {
        var root = new XPathContext(document, document, globals);

        try
        {
            return evaluator.Evaluate(AsMatchPattern(context), root).Items;
        }
        catch (XPathException)
        {
            return [];
        }
    }

    /// <summary>
    /// A rule's context is a match pattern, not a path from the document: <c>ram:IssuerTradeParty</c> claims
    /// every element of that name wherever it sits, and <c>a/b</c> every <c>b</c> whose parent is an
    /// <c>a</c>. Reading a relative context as a path instead would silently match nothing — which is how the
    /// French lifecycle rules, all of them written relative, appeared to pass without ever running.
    /// </summary>
    private static XPathNode AsMatchPattern(XPathNode context) => context switch
    {
        PathNode { Absolute: false, Start: null, Steps.Count: > 0 } path => path with
        {
            Absolute = true,
            Steps = [path.Steps[0] with { DescendantOrSelf = true }, .. path.Steps.Skip(1)],
        },
        BinaryNode { Operator: "|" } union => union with
        {
            Left = AsMatchPattern(union.Left),
            Right = AsMatchPattern(union.Right),
        },
        _ => context,
    };

    /// <summary>The business term a rule names, so a caller can point at the field rather than the rule.</summary>
    private static string? BusinessTermIn(string identifier, string message)
    {
        foreach (string source in (string[])[identifier, message])
        {
            Match match = BusinessTerm().Match(source);

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
