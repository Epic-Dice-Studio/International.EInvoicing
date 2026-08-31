using System.Text.RegularExpressions;
using System.Xml.Linq;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron.XPath;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Validation.Schematron;

/// <summary>One assertion of a rule: an expression, and what to say when it does not hold.</summary>
internal sealed record SchematronAssertion(
    string Identifier,
    XPathNode Test,
    string Message,
    RuleSeverity Severity,
    bool IsReport);

/// <summary>A named expression a rule set defines once and reuses, Schematron's <c>let</c>.</summary>
internal sealed record SchematronVariable(string Name, XPathNode Expression);

/// <summary>A rule: a context expression, its own variables, and the assertions that apply to it.</summary>
internal sealed record SchematronRule(
    XPathNode Context,
    IReadOnlyList<SchematronVariable> Variables,
    IReadOnlyList<SchematronAssertion> Assertions);

/// <summary>A pattern: an ordered group of rules. Within a pattern, the first matching rule claims a node.</summary>
internal sealed record SchematronPattern(string? Identifier, IReadOnlyList<SchematronRule> Rules);

/// <summary>
/// A parsed Schematron rule set, ready to run.
/// </summary>
/// <remarks>
/// Parsed from the published artefact rather than generated from it, so replacing the file replaces the
/// rules. The preprocessed form is what to load: its abstract patterns are already resolved.
/// </remarks>
public sealed partial class SchematronRuleSet
{
    [GeneratedRegex(@"^\[([^\]]{1,40})\]")]
    private static partial Regex LeadingCode();

    private static readonly XNamespace Schematron = "http://purl.oclc.org/dsdl/schematron";

    private SchematronRuleSet(
        string name,
        string version,
        IReadOnlyDictionary<string, string> namespaces,
        IReadOnlyList<SchematronPattern> patterns)
    {
        Name = name;
        Version = version;
        Namespaces = namespaces;
        Patterns = patterns;
    }

    /// <summary>The rule set's name, as a reader would recognise it.</summary>
    public string Name { get; }

    /// <summary>Which version of it.</summary>
    public string Version { get; }

    /// <summary>How many assertions it carries. Useful for confirming an artefact loaded in full.</summary>
    public int AssertionCount => Patterns.Sum(pattern => pattern.Rules.Sum(rule => rule.Assertions.Count));

    internal IReadOnlyDictionary<string, string> Namespaces { get; }

    internal IReadOnlyList<SchematronPattern> Patterns { get; }

    /// <summary>Variables the rule set declares once and every rule can use.</summary>
    internal IReadOnlyList<SchematronVariable> GlobalVariables { get; private set; } = [];

    /// <summary>Functions the rule set defines for itself, run from the artefact rather than reimplemented.</summary>
    internal IReadOnlyDictionary<string, SchematronFunction> Functions { get; private set; } =
        new Dictionary<string, SchematronFunction>(StringComparer.Ordinal);

    /// <summary>Loads a rule set from Schematron XML.</summary>
    /// <param name="schematron">The <c>.sch</c> content, preferably the preprocessed form.</param>
    /// <param name="name">The rule set's name.</param>
    /// <param name="version">Its version.</param>
    /// <param name="include">
    /// Resolves an <c>include</c> by href. Rule sets published in parts — the German ones keep their global
    /// variables in a separate file — need this; the EN 16931 preprocessed artefacts do not.
    /// </param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="XPathException">An expression in the rule set could not be read.</exception>
    public static SchematronRuleSet Load(
        string schematron,
        string name,
        string version,
        Func<string, string?>? include = null)
    {
        ArgumentNullException.ThrowIfNull(schematron);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(version);

        using var reader = SecureXml.CreateReader(schematron, DocumentLimits.Unlimited);
        XElement root = XElement.Load(reader);

        Resolve(root, include);

        Dictionary<string, string> namespaces = root
            .Descendants(Schematron + "ns")
            .Where(ns => ns.Attribute("prefix") is not null && ns.Attribute("uri") is not null)
            .ToDictionary(
                ns => ns.Attribute("prefix")!.Value,
                ns => ns.Attribute("uri")!.Value,
                StringComparer.Ordinal);

        List<SchematronPattern> patterns =
        [
            .. root.Descendants(Schematron + "pattern").Select(ReadPattern),
        ];

        return new SchematronRuleSet(name, version, namespaces, patterns)
        {
            GlobalVariables = [.. ReadVariables(root.Descendants(Schematron + "let").Where(IsGlobal))],
            Functions = SchematronFunction.ReadAll(root, Parse),
        };
    }

    /// <summary>Replaces every include with what it points at, so the rule set is whole before it is read.</summary>
    private static void Resolve(XElement root, Func<string, string?>? include)
    {
        foreach (XElement element in root.Descendants(Schematron + "include").ToList())
        {
            string? href = element.Attribute("href")?.Value;
            string? content = href is null ? null : include?.Invoke(href);

            if (content is null)
            {
                element.Remove();
                continue;
            }

            using var reader = SecureXml.CreateReader(content, DocumentLimits.Unlimited);
            element.ReplaceWith(XElement.Load(reader));
        }
    }

    /// <summary>A variable outside any rule belongs to the whole rule set.</summary>
    private static bool IsGlobal(XElement let) =>
        let.Ancestors(Schematron + "rule").Any() is false;

    private static IEnumerable<SchematronVariable> ReadVariables(IEnumerable<XElement> lets)
    {
        foreach (XElement let in lets)
        {
            string? name = let.Attribute("name")?.Value;
            string? value = let.Attribute("value")?.Value;

            if (name is not null && value is not null)
            {
                yield return new SchematronVariable(name, Parse(value));
            }
        }
    }

    /// <summary>
    /// Builds a rule set from a Schematron that was compiled to XSLT.
    /// </summary>
    /// <remarks>
    /// The parsing of expressions, the severities and the message handling are the same code the source form
    /// uses, which is the point: a rule read from either serialisation has to become the same rule, and
    /// <c>CompiledSchematronTests</c> holds the two to that.
    /// </remarks>
    internal static SchematronRuleSet FromCompiled(
        string name,
        string version,
        Dictionary<string, string> namespaces,
        IReadOnlyList<(string? Identifier, IReadOnlyList<XElement> Templates)> patterns,
        XElement root)
    {
        List<SchematronPattern> read =
        [
            .. patterns.Select(pattern => new SchematronPattern(
                pattern.Identifier,
                [.. pattern.Templates.Select(ReadCompiledRule)])),
        ];

        return new SchematronRuleSet(name, version, namespaces, read)
        {
            GlobalVariables = [.. CompiledVariables(CompiledSchematron.GlobalVariablesOf(root))],
            Functions = SchematronFunction.ReadAll(root, Parse),
        };
    }

    private static SchematronRule ReadCompiledRule(XElement template) =>
        new(
            Parse(CompiledSchematron.ContextOf(template)),
            [.. CompiledVariables(CompiledSchematron.VariablesOf(template))],
            [.. CompiledSchematron.AssertionsOf(template).Select(ReadCompiledAssertion)]);

    private static SchematronAssertion ReadCompiledAssertion(XElement emission)
    {
        string test = emission.Attribute("test")?.Value
            ?? throw new XPathException("A compiled Schematron assertion has no test.");

        string message = NormalizeMessage(CompiledSchematron.MessageOf(emission));
        string identifier = CompiledSchematron.ConstantAttribute(emission, "id")
            ?? CodeIn(message)
            ?? "(unnamed)";

        string flag = CompiledSchematron.ConstantAttribute(emission, "flag") ?? "fatal";

        RuleSeverity severity = flag.ToUpperInvariant() switch
        {
            "WARNING" => RuleSeverity.Warning,
            "INFO" or "INFORMATION" => RuleSeverity.Information,
            _ => RuleSeverity.Error,
        };

        return new SchematronAssertion(
            identifier,
            Parse(test),
            message,
            severity,
            CompiledSchematron.IsReport(emission));
    }

    private static IEnumerable<SchematronVariable> CompiledVariables(IEnumerable<XElement> variables)
    {
        foreach (XElement variable in variables)
        {
            if (variable.Attribute("name")?.Value is not { Length: > 0 } name
                || variable.Attribute("select")?.Value is not { Length: > 0 } select)
            {
                continue;
            }

            yield return new SchematronVariable(name, Parse(select));
        }
    }

    private static SchematronPattern ReadPattern(XElement pattern) =>
        new(
            pattern.Attribute("id")?.Value,
            [.. pattern.Elements(Schematron + "rule").Where(HasContext).Select(ReadRule)]);

    private static bool HasContext(XElement rule) =>
        !string.IsNullOrWhiteSpace(rule.Attribute("context")?.Value);

    private static SchematronRule ReadRule(XElement rule)
    {
        string context = rule.Attribute("context")!.Value;

        List<SchematronAssertion> assertions =
        [
            .. rule.Elements()
                .Where(child => child.Name == Schematron + "assert" || child.Name == Schematron + "report")
                .Select(child => ReadAssertion(child, child.Name == Schematron + "report")),
        ];

        return new SchematronRule(
            Parse(context),
            [.. ReadVariables(rule.Elements(Schematron + "let"))],
            assertions);
    }

    private static SchematronAssertion ReadAssertion(XElement assertion, bool isReport)
    {
        string test = assertion.Attribute("test")?.Value
            ?? throw new XPathException("A Schematron assertion has no test.");

        string identifier = assertion.Attribute("id")?.Value ?? CodeIn(assertion.Value) ?? "(unnamed)";
        string flag = assertion.Attribute("flag")?.Value ?? "fatal";

        RuleSeverity severity = flag.ToUpperInvariant() switch
        {
            "WARNING" => RuleSeverity.Warning,
            "INFO" or "INFORMATION" => RuleSeverity.Information,
            _ => RuleSeverity.Error,
        };

        return new SchematronAssertion(
            identifier,
            Parse(test),
            NormalizeMessage(assertion.Value),
            severity,
            isReport);
    }

    /// <summary>
    /// The rule code a message opens with, for rule sets that name their rules in the message rather than in
    /// an attribute — the French e-reporting artefacts do, and a report saying "(unnamed)" helps nobody.
    /// </summary>
    private static string? CodeIn(string message)
    {
        Match match = LeadingCode().Match(message.TrimStart());

        return match.Success ? match.Groups[1].Value : null;
    }

    private static XPathNode Parse(string expression)
    {
        try
        {
            return XPathParser.Parse(expression);
        }
        catch (XPathException exception)
        {
            throw new XPathException($"Could not read '{Shorten(expression)}': {exception.Message}", exception);
        }
    }

    private static string NormalizeMessage(string message) =>
        string.Join(' ', message.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Shorten(string expression) =>
        expression.Length <= 120 ? expression : expression[..120] + "…";
}
