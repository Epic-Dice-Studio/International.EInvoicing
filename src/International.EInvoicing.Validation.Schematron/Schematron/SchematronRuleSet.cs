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

/// <summary>A rule: a context expression, and the assertions that apply to nodes matching it.</summary>
internal sealed record SchematronRule(XPathNode Context, IReadOnlyList<SchematronAssertion> Assertions);

/// <summary>A pattern: an ordered group of rules. Within a pattern, the first matching rule claims a node.</summary>
internal sealed record SchematronPattern(string? Identifier, IReadOnlyList<SchematronRule> Rules);

/// <summary>
/// A parsed Schematron rule set, ready to run.
/// </summary>
/// <remarks>
/// Parsed from the published artefact rather than generated from it, so replacing the file replaces the
/// rules. The preprocessed form is what to load: its abstract patterns are already resolved.
/// </remarks>
public sealed class SchematronRuleSet
{
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

    /// <summary>Loads a rule set from Schematron XML.</summary>
    /// <param name="schematron">The <c>.sch</c> content, preferably the preprocessed form.</param>
    /// <param name="name">The rule set's name.</param>
    /// <param name="version">Its version.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="XPathException">An expression in the rule set could not be read.</exception>
    public static SchematronRuleSet Load(string schematron, string name, string version)
    {
        ArgumentNullException.ThrowIfNull(schematron);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(version);

        using var reader = SecureXml.CreateReader(schematron, DocumentLimits.Unlimited);
        XElement root = XElement.Load(reader);

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

        return new SchematronRuleSet(name, version, namespaces, patterns);
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

        return new SchematronRule(Parse(context), assertions);
    }

    private static SchematronAssertion ReadAssertion(XElement assertion, bool isReport)
    {
        string test = assertion.Attribute("test")?.Value
            ?? throw new XPathException("A Schematron assertion has no test.");

        string identifier = assertion.Attribute("id")?.Value ?? "(unnamed)";
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
