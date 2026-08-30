using System.Xml.Linq;
using International.EInvoicing.Validation.Schematron.XPath;

namespace International.EInvoicing.Validation.Schematron;

/// <summary>
/// A function a rule set defines for itself, in XSLT, and calls from its rules.
/// </summary>
/// <remarks>
/// Running these from the artefact rather than reimplementing them is the whole point: the French rule sets
/// define twenty of them — SIRET coherence, decimal precision, code list membership — and a reimplementation
/// would drift from the published version the first time it was revised.
/// </remarks>
internal sealed record SchematronFunction(
    string Name,
    IReadOnlyList<string> Parameters,
    IReadOnlyList<SchematronVariable> Variables,
    XPathNode Body)
{
    private static readonly XNamespace Xslt = "http://www.w3.org/1999/XSL/Transform";

    /// <summary>
    /// Reads the functions a rule set defines. Anything shaped differently from
    /// "optional variables, then a returned sequence" is left out, so a rule calling it reports that it could
    /// not be evaluated rather than passing on a guess.
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

            XElement? returned = declaration.Elements(Xslt + "sequence").LastOrDefault();
            if (returned?.Attribute("select")?.Value is not { Length: > 0 } body)
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

            List<SchematronVariable> variables = [];
            foreach (XElement variable in declaration.Elements(Xslt + "variable"))
            {
                if (variable.Attribute("name")?.Value is { Length: > 0 } variableName
                    && variable.Attribute("select")?.Value is { Length: > 0 } select)
                {
                    variables.Add(new SchematronVariable(variableName, parse(select)));
                }
            }

            functions[name] = new SchematronFunction(name, parameters, variables, parse(body));
        }

        return functions;
    }
}
