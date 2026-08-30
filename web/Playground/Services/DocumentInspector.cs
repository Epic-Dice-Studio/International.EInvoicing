using System.Xml.Linq;
using International.EInvoicing.Cdar;
using International.EInvoicing.Cdar.Model;
using International.EInvoicing.Cdar.Reading;
using International.EInvoicing.Cii;
using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Configuration;
using International.EInvoicing.Countries.France;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl;
using International.EInvoicing.Ubl.Reading;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.En16931;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.Playground.Services;

/// <summary>
/// Reads and validates whatever the visitor drops in, entirely in their browser.
/// </summary>
/// <remarks>
/// The format is detected rather than asked for, because a person holding an invoice usually does not know
/// whether it is UBL or CII, and should not have to.
/// </remarks>
public sealed class DocumentInspector
{
    private readonly EInvoicingOptions _options = new();
    private readonly IProfileResolver _profiles;
    private readonly SchematronValidator _validator = new();

    /// <summary>Creates an inspector knowing every profile this library implements.</summary>
    public DocumentInspector()
    {
        var registry = new ProfileRegistry(KnownProfiles.All);

        foreach (Profile profile in FrProfiles.All.Concat(CdarProfiles.All))
        {
            registry.Register(profile);
        }

        _profiles = new ProfileResolver(registry);
    }

    /// <summary>What a document looks like, judged by its root element rather than its file name.</summary>
    public static DocumentKind Detect(string content)
    {
        if (content.TrimStart().StartsWith("%PDF-", StringComparison.Ordinal))
        {
            return DocumentKind.Pdf;
        }

        try
        {
            XName root = XDocument.Parse(content).Root?.Name ?? XName.Get("none");

            if (root.Namespace == CdarNames.Rsm)
            {
                return DocumentKind.Cdar;
            }

            if (root.Namespace == CiiNames.Rsm)
            {
                return DocumentKind.Cii;
            }

            return root.Namespace == UblNames.Invoice || root.Namespace == UblNames.CreditNote
                ? DocumentKind.Ubl
                : DocumentKind.Unknown;
        }
        catch (System.Xml.XmlException)
        {
            return DocumentKind.Unknown;
        }
    }

    /// <summary>Reads a document and, when it is an invoice, validates it against EN 16931.</summary>
    public InspectionResult Inspect(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        return Detect(content) switch
        {
            DocumentKind.Ubl => InspectInvoice(content, DocumentKind.Ubl),
            DocumentKind.Cii => InspectInvoice(content, DocumentKind.Cii),
            DocumentKind.Cdar => InspectStatus(content),
            DocumentKind.Pdf => new InspectionResult
            {
                Kind = DocumentKind.Pdf,
                Failure = "This is a PDF. Reading the invoice inside one needs a PDF reader, which the "
                    + "browser build leaves out — paste the XML instead.",
            },
            _ => new InspectionResult
            {
                Kind = DocumentKind.Unknown,
                Failure = "This does not look like UBL, CII or a lifecycle message. Check the root element "
                    + "and its namespace.",
            },
        };
    }

    private InspectionResult InspectInvoice(string content, DocumentKind kind)
    {
        ParseResult<EInvoice> result = kind == DocumentKind.Ubl
            ? new UblInvoiceReader(_options, _profiles).Read(content)
            : new CiiInvoiceReader(_options, _profiles).Read(content);

        if (result.Value is not { } invoice)
        {
            return new InspectionResult
            {
                Kind = kind,
                Diagnostics = result.Diagnostics,
                Failure = "Nothing could be read from this document.",
            };
        }

        return new InspectionResult
        {
            Kind = kind,
            Invoice = invoice,
            Diagnostics = result.Diagnostics,
            Validation = Validate(content, kind, invoice),
        };
    }

    /// <summary>
    /// Validates, and says what could not be validated. A profile the library does not implement means the
    /// document was measured against EN 16931 only, which the report must not hide.
    /// </summary>
    private ValidationReport Validate(string content, DocumentKind kind, EInvoice invoice)
    {
        DocumentSyntax syntax = kind == DocumentKind.Ubl ? DocumentSyntax.Ubl : DocumentSyntax.Cii;

        ValidationReport report;
        try
        {
            report = _validator.Validate(content, En16931Rules.For(syntax));
        }
        catch (Exception exception) when (exception is International.EInvoicing.Validation.Schematron.XPath.XPathException or System.Xml.XmlException)
        {
            return new ValidationReport(
                [],
                [new RuleSetOutcome("EN 16931", En16931Rules.ArtefactVersion, Ran: false, exception.Message)]);
        }

        if (invoice.Profile is not { IsExact: false } resolution)
        {
            return report;
        }

        return report.And(new ValidationReport(
            [],
            [
                new RuleSetOutcome(
                    resolution.Declared.ToString(),
                    "—",
                    Ran: false,
                    "this library implements no rules for that profile, so only EN 16931 was checked"),
            ]));
    }

    private InspectionResult InspectStatus(string content)
    {
        ParseResult<LifecycleStatusMessage> result = new CdarReader(_options, _profiles).Read(content);

        return new InspectionResult
        {
            Kind = DocumentKind.Cdar,
            Status = result.Value,
            Diagnostics = result.Diagnostics,
            Failure = result.Value is null ? "Nothing could be read from this document." : null,
        };
    }
}
