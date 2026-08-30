using System.Xml.Linq;
using International.EInvoicing.Validation.Schematron.XPath;

namespace International.EInvoicing.Validation.Schematron;

/// <summary>
/// A function a rule set defines for itself, in XSLT, and calls from its rules.
/// </summary>
/// <remarks>
/// Running these from the artefact rather than reimplementing them is the whole point: the French rule sets
/// define twenty of them and Peppol another eight — SIRET coherence, GLN and Luhn check digits, code list
/// membership — and a reimplementation would drift from the published version the first time it was revised.
/// </remarks>
internal sealed record SchematronFunction(
    string Name,
    IReadOnlyList<string> Parameters,
    IReadOnlyList<SchematronVariable> Variables,
    XPathNode Body)
{
    private static readonly XNamespace Xslt = "http://www.w3.org/1999/XSL/Transform";

    /// <summary>
    /// Reads the functions a rule set defines. A function whose body this reader cannot follow is left out,
    /// so a rule calling it reports that it could not be evaluated rather than passing on a guess.
    /// </summary>
    public static IReadOnlyDictionary<string, SchematronFunction> ReadAll(XElement root, Func<string, XPathNode> parse)
    {
        var functions = new Dictionary<string, SchematronFunction>(StringComparer.Ordinal);

        foreach (XElement declaration in root.Descendants(Xslt + "function"))
        {
            if (declaration.Attribute("name")?.Value is not { Length: > 0 } name)
            {
                continue;
            }

            if (ReadBody(declaration, parse) is not { } body)
            {
                continue;
            }

            List<string> parameters =
            [
                .. declaration
                    .Elements(Xslt + "param")
                    .Select(parameter => parameter.Attribute("name")?.Value)
                    .OfType<string>(),
            ];

            functions[name] = new SchematronFunction(name, parameters, ReadVariables(declaration, parse), body);
        }

        return functions;
    }

    /// <summary>
    /// What the block returns. A block either returns a sequence outright or chooses between branches, and
    /// a choice is an expression here rather than control flow.
    /// </summary>
    private static XPathNode? ReadBody(XElement block, Func<string, XPathNode> parse)
    {
        if (Returned(block) is { } returned)
        {
            return parse(returned);
        }

        XElement? choose = block.Element(Xslt + "choose");

        return choose is null ? null : ReadChoice(choose, parse);
    }

    private static XPathNode? ReadChoice(XElement choose, Func<string, XPathNode> parse)
    {
        XElement? otherwise = choose.Element(Xslt + "otherwise");
        XPathNode? result = otherwise is null ? null : ReadBody(otherwise, parse);

        foreach (XElement when in choose.Elements(Xslt + "when").Reverse())
        {
            if (when.Attribute("test")?.Value is not { Length: > 0 } test || ReadBody(when, parse) is not { } branch)
            {
                return null;
            }

            result = new ConditionalNode(parse(test), branch, result ?? new LiteralNode(XPathValue.Empty));
        }

        return result;
    }

    /// <summary>
    /// The variables a function declares, in document order.
    /// </summary>
    /// <remarks>
    /// Variables declared inside a branch are hoisted with the rest, so they are evaluated whether or not
    /// their branch is taken. These are pure expressions over the parameters, and one that cannot be
    /// evaluated is left empty rather than failing the call — the branch that would have used it is not the
    /// one being taken.
    /// </remarks>
    private static List<SchematronVariable> ReadVariables(XElement declaration, Func<string, XPathNode> parse)
    {
        List<SchematronVariable> variables = [];

        foreach (XElement variable in declaration.Descendants(Xslt + "variable"))
        {
            if (variable.Attribute("name")?.Value is not { Length: > 0 } name)
            {
                continue;
            }

            // A variable is computed from a select expression, from a sequence inside it, or spelled out as
            // content — the published rule sets use all three.
            string? select = variable.Attribute("select")?.Value ?? Returned(variable);

            variables.Add(select is { Length: > 0 }
                ? new SchematronVariable(name, parse(select))
                : new SchematronVariable(name, new LiteralNode(XPathValue.Text(variable.Value))));
        }

        return variables;
    }

    private static string? Returned(XElement block) =>
        block.Elements(Xslt + "sequence").LastOrDefault()?.Attribute("select")?.Value;
}
