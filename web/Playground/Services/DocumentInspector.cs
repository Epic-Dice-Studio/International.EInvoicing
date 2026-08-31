using International.EInvoicing.Countries.Australia;
using International.EInvoicing.Countries.Belgium;
using International.EInvoicing.Countries.Croatia;
using International.EInvoicing.Countries.Denmark;
using International.EInvoicing.Countries.France;
using International.EInvoicing.Countries.Germany;
using International.EInvoicing.Countries.Iceland;
using International.EInvoicing.Countries.Netherlands;
using International.EInvoicing.Countries.NewZealand;
using International.EInvoicing.Countries.Norway;
using International.EInvoicing.Countries.Sweden;
using International.EInvoicing.Model;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.En16931;
using International.EInvoicing.Validation.XRechnung;

namespace International.EInvoicing.Playground.Services;

/// <summary>
/// Reads and validates whatever the visitor drops in, entirely in their browser.
/// </summary>
/// <remarks>
/// <para>
/// This is the library's own facade, assembled the way a real integration would assemble it: every syntax,
/// every country package, and the rule sets that may be redistributed. The site does not reimplement the
/// detection or the validation — it calls <c>Read</c> and <c>Validate</c>, which is the whole point of
/// showing it.
/// </para>
/// <para>
/// The format is detected rather than asked for, because a person holding an invoice usually does not know
/// whether it is UBL or CII, and should not have to.
/// </para>
/// </remarks>
public sealed class DocumentInspector
{
    private readonly EInvoicing _library = EInvoicing.Create(library => library
        .AddDefaults()
        .AddEn16931Rules()
        .AddXRechnungRules()
        .AddFrance()
        .AddGermany()
        .AddBelgium()
        .AddNetherlands()
        .AddNorway()
        .AddSweden()
        .AddDenmark()
        .AddIceland()
        .AddCroatia()
        .AddAustralia()
        .AddNewZealand());

    /// <summary>The library this page is running, for the panels that want to ask it what it knows.</summary>
    public EInvoicing Library => _library;

    /// <summary>What a document looks like, judged by its content rather than its file name.</summary>
    public DocumentKind Detect(string content)
    {
        if (content.TrimStart().StartsWith("%PDF-", StringComparison.Ordinal))
        {
            return DocumentKind.Pdf;
        }

        if (FrenchEInvoicing.Over(_library).Read(content) is { Kind: FrenchDocumentKind.EReport })
        {
            return DocumentKind.EReport;
        }

        return _library.Read(content).Kind switch
        {
            International.EInvoicing.DocumentKind.Ubl or International.EInvoicing.DocumentKind.UblCreditNote =>
                DocumentKind.Ubl,
            International.EInvoicing.DocumentKind.Cii => DocumentKind.Cii,
            International.EInvoicing.DocumentKind.Cdar => DocumentKind.Cdar,
            _ => DocumentKind.Unknown,
        };
    }

    /// <summary>Reads a document and validates it against every rule set registered for its profile.</summary>
    public InspectionResult Inspect(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.TrimStart().StartsWith("%PDF-", StringComparison.Ordinal))
        {
            return new InspectionResult
            {
                Kind = DocumentKind.Pdf,
                Failure = "This is a PDF. Reading the invoice inside one needs a PDF reader, which the "
                    + "browser build leaves out — paste the XML instead.",
            };
        }

        FrenchDocument french = FrenchEInvoicing.Over(_library).Read(content);

        if (french.Kind == FrenchDocumentKind.EReport)
        {
            return new InspectionResult
            {
                Kind = DocumentKind.EReport,
                EReport = french.EReport,
                Diagnostics = french.Diagnostics,
            };
        }

        DocumentResult result = _library.Read(content);

        if (!result.IsUsable)
        {
            return new InspectionResult
            {
                Kind = DocumentKind.Unknown,
                Diagnostics = result.Diagnostics,
                Failure = "This does not look like UBL, CII, a lifecycle message or a flux 10 report. Check "
                    + "the root element and its namespace.",
            };
        }

        DocumentKind kind = result.Kind switch
        {
            International.EInvoicing.DocumentKind.Cii => DocumentKind.Cii,
            International.EInvoicing.DocumentKind.Cdar => DocumentKind.Cdar,
            _ => DocumentKind.Ubl,
        };

        return new InspectionResult
        {
            Kind = kind,
            Invoice = result.Invoice,
            Status = result.LifecycleStatus,
            Diagnostics = result.Diagnostics,
            Validation = result.Invoice is null ? null : Validate(content),
        };
    }

    /// <summary>
    /// Validates against everything registered, and says what did not run. A profile with no rule set means
    /// the document was measured against less than it claims to be, which the report must not hide.
    /// </summary>
    private ValidationReport Validate(string content)
    {
        try
        {
            return _library.Validate(content);
        }
        catch (Exception exception)
            when (exception is Validation.Schematron.XPath.XPathException or System.Xml.XmlException)
        {
            return new ValidationReport(
                [],
                [new RuleSetOutcome("the registered rule sets", "—", Ran: false, exception.Message)]);
        }
    }
}
