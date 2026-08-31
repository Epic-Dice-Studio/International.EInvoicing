using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using International.EInvoicing.Validation;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Validation.Xsd;

/// <summary>Running a document past a schema, and saying what came back.</summary>
internal static class SchemaCheck
{
    public static ValidationReport Run(string document, XmlSchemaSet schemas, string name, string version)
    {
        var messages = new List<ValidationMessage>();

        XDocument parsed;
        try
        {
            using XmlReader reader = SecureXml.CreateReader(document, DocumentLimits.Unlimited);
            parsed = XDocument.Load(reader, LoadOptions.SetLineInfo);
        }
        catch (XmlException exception)
        {
            // A document that will not parse has no shape to judge, and saying so is the honest answer:
            // "valid" and "never looked at" must not read the same way.
            return new ValidationReport(
                [Message(name, "XSD-PARSE", exception.Message, exception.LineNumber, exception.LinePosition)],
                [new RuleSetOutcome(name, version, Ran: false, "the document is not XML")]);
        }

        parsed.Validate(
            schemas,
            (_, arguments) => messages.Add(Message(
                name,
                Identifier(arguments.Exception),
                arguments.Message,
                arguments.Exception?.LineNumber ?? 0,
                arguments.Exception?.LinePosition ?? 0)),
            addSchemaInfo: false);

        return new ValidationReport(messages, [new RuleSetOutcome(name, version, Ran: true)]);
    }

    /// <summary>
    /// What to call a schema failure, since a schema has no rule identifiers of its own.
    /// </summary>
    /// <remarks>
    /// A caller filtering a report by identifier needs something stable to filter on, and "the element order
    /// is wrong" and "that value is not allowed" are different problems to whoever has to fix them.
    /// </remarks>
    private static string Identifier(XmlSchemaException? exception) =>
        exception?.Message.Contains("has invalid child element", StringComparison.Ordinal) == true
            ? "XSD-SEQUENCE"
            : "XSD";

    private static ValidationMessage Message(string ruleSet, string identifier, string message, int line, int position) =>
        new(identifier, RuleSeverity.Error, message)
        {
            RuleSet = ruleSet,
            Location = line > 0
                ? string.Create(CultureInfo.InvariantCulture, $"line {line}, position {position}")
                : null,
        };
}
