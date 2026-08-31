using System.Xml.Linq;
using International.EInvoicing.Validation.Schematron.XPath;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Validation.Schematron;

/// <summary>
/// Reads a rule set from a Schematron that was compiled to XSLT.
/// </summary>
/// <remarks>
/// <para>
/// Some publishers ship only the compiled form. OpenPEPPOL does for PINT, which is what every Peppol
/// jurisdiction outside Europe validates against — so without this, those countries have profiles and no
/// rules.
/// </para>
/// <para>
/// <b>This is not a translation.</b> A compiled Schematron still contains every original assertion verbatim:
/// the rule context is the template's <c>match</c>, and each assertion's own test, identifier, severity and
/// message are attributes and text on the <c>svrl:failed-assert</c> the compiler emits for it. What happens
/// here is reading the same rules out of a different serialisation — the same thing this library does with a
/// <c>.sch</c> file, which is also a serialisation of rules it did not write.
/// </para>
/// <para>
/// That distinction matters because the alternative — a human rewriting compiled rules by hand — produces a
/// rule set nobody can compare against its publisher's, which is worse than no rule set. This one is
/// comparable, and compared: <c>CompiledSchematronTests</c> reads the compiled EN 16931 artefact and requires
/// it to yield exactly the assertions this library parses from the source Schematron of the same version.
/// </para>
/// </remarks>
public static class CompiledSchematron
{
    private static readonly XNamespace Xslt = "http://www.w3.org/1999/XSL/Transform";
    private static readonly XNamespace Svrl = "http://purl.oclc.org/dsdl/svrl";

    /// <summary>Reads a rule set from a compiled Schematron stylesheet.</summary>
    /// <param name="stylesheet">The stylesheet, as published.</param>
    /// <param name="name">What a validation report should call the rule set.</param>
    /// <param name="version">Which version it is, so a report can be reproduced later.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="XPathException">The stylesheet holds an expression this engine cannot parse.</exception>
    public static SchematronRuleSet Read(string stylesheet, string name, string version)
    {
        ArgumentNullException.ThrowIfNull(stylesheet);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(version);

        using var reader = SecureXml.CreateReader(stylesheet, DocumentLimits.Unlimited);
        XElement root = XElement.Load(reader);

        return SchematronRuleSet.FromCompiled(
            name,
            version,
            Namespaces(root),
            [.. Patterns(root)],
            root);
    }

    /// <summary>
    /// The prefixes the rules are written against, taken from the stylesheet's own declarations.
    /// </summary>
    /// <remarks>
    /// A compiled Schematron has no <c>sch:ns</c> elements: the compiler turns them into ordinary namespace
    /// declarations on the stylesheet, because that is what its own XPath expressions then resolve against.
    /// The tooling's own prefixes come along too, which costs nothing — no rule refers to them.
    /// </remarks>
    private static Dictionary<string, string> Namespaces(XElement root) => root
        .Attributes()
        .Where(attribute => attribute.IsNamespaceDeclaration && attribute.Name.LocalName != "xmlns")
        .ToDictionary(
            attribute => attribute.Name.LocalName,
            attribute => attribute.Value,
            StringComparer.Ordinal);

    /// <summary>
    /// One pattern per mode. The compiler gives each Schematron pattern a mode of its own, which is what
    /// keeps "the first matching rule in a pattern claims the node" true after compilation — so grouping by
    /// mode recovers the patterns without depending on the comments the compiler also emits.
    /// </summary>
    private static IEnumerable<(string? Identifier, IReadOnlyList<XElement> Templates)> Modes(XElement root)
    {
        var order = new List<string>();
        var byMode = new Dictionary<string, List<XElement>>(StringComparer.Ordinal);

        foreach (XElement template in root.Elements(Xslt + "template"))
        {
            if (template.Attribute("match") is null || !template.Descendants(Svrl + "fired-rule").Any())
            {
                continue;
            }

            string mode = template.Attribute("mode")?.Value ?? string.Empty;

            if (!byMode.TryGetValue(mode, out List<XElement>? templates))
            {
                templates = [];
                byMode[mode] = templates;
                order.Add(mode);
            }

            templates.Add(template);
        }

        foreach (string mode in order)
        {
            yield return (mode.Length == 0 ? null : mode, byMode[mode]);
        }
    }

    private static IEnumerable<(string? Identifier, IReadOnlyList<XElement> Templates)> Patterns(XElement root) =>
        Modes(root);

    /// <summary>Every assertion a compiled rule emits, in the order the source declared them.</summary>
    internal static IEnumerable<XElement> AssertionsOf(XElement template) => template
        .Descendants()
        .Where(element => element.Name == Svrl + "failed-assert" || element.Name == Svrl + "successful-report");

    /// <summary>Whether this emission is a Schematron <c>report</c> rather than an <c>assert</c>.</summary>
    internal static bool IsReport(XElement emission) => emission.Name == Svrl + "successful-report";

    /// <summary>
    /// An attribute the compiler writes as an <c>xsl:attribute</c> child rather than as a literal.
    /// </summary>
    /// <remarks>
    /// The identifier and the flag are constant in the source, and the compiler still emits them this way.
    /// Anything computed at run time is not a constant and is deliberately not recovered — a rule whose
    /// severity depended on the document would be reported at its declared severity, not a guessed one.
    /// </remarks>
    internal static string? ConstantAttribute(XElement emission, string name) => emission
        .Elements(Xslt + "attribute")
        .FirstOrDefault(attribute =>
            string.Equals(attribute.Attribute("name")?.Value, name, StringComparison.Ordinal))
        ?.Nodes()
        .OfType<XText>()
        .Aggregate(new System.Text.StringBuilder(), (text, node) => text.Append(node.Value))
        .ToString()
        .Trim() is { Length: > 0 } value
        ? value
        : null;

    /// <summary>The message, which is the text the compiler kept from the source.</summary>
    internal static string MessageOf(XElement emission)
    {
        XElement? text = emission.Element(Svrl + "text");

        return text is null ? string.Empty : text.Value;
    }

    /// <summary>The rule-level variables, which the compiler writes as ordinary stylesheet variables.</summary>
    internal static IEnumerable<XElement> VariablesOf(XElement template) =>
        template.Elements(Xslt + "variable");

    /// <summary>The stylesheet's own top-level variables, which were the rule set's global lets.</summary>
    internal static IEnumerable<XElement> GlobalVariablesOf(XElement root) =>
        root.Elements(Xslt + "variable");

    /// <summary>The context a compiled rule applies to.</summary>
    internal static string ContextOf(XElement template) => template.Attribute("match")!.Value;
}
