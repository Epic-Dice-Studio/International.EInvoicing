using System.Text;
using International.EInvoicing.Building;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Tests;

/// <summary>
/// Getting the things a person opens out of an invoice, with the three of them named apart.
/// </summary>
/// <remarks>
/// An invoice carries a <em>readable rendition</em> — itself, in a form somebody can look at — and
/// <em>supporting documents</em>, which are something else entirely: a timesheet, a delivery note. A BG-24
/// entry that only gives a URI is neither, because fetching it is network I/O this library does not do. The
/// names matter more than the convenience: a caller who takes a delivery note for the invoice's readable
/// copy has mixed up two different things.
/// </remarks>
public class AttachedDocumentTests
{
    private static readonly EInvoicing Library = EInvoicing.Create(builder => builder.AddDefaults());

    [Theory]
    [InlineData("UBL")]
    [InlineData("CII")]
    public void AnAttachedDocumentComesBackReadyToOpen(string syntax)
    {
        EInvoice read = RoundTripped(syntax);

        SupportingDocument document = read.SupportingDocuments.ShouldHaveSingleItem();

        document.Identifier.ShouldBe("TIME-42");
        document.Description.ShouldBe("Timesheet");
        document.FileName.ShouldBe("timesheet.csv");
        document.MediaType.ShouldBe("text/csv");

        using var content = new StreamReader(document.OpenRead());
        content.ReadToEnd().ShouldBe("hours;rate\n8;120");
    }

    /// <summary>A URI is an address, not a document: this library will not fetch it, and says so by shape.</summary>
    [Theory]
    [InlineData("UBL")]
    [InlineData("CII")]
    public void ADocumentTheInvoiceOnlyPointsAtIsNotOneItCarries(string syntax)
    {
        EInvoice read = RoundTripped(syntax);

        SupportingDocumentLink link = read.SupportingDocumentLinks.ShouldHaveSingleItem();

        link.Location.ShouldBe("https://example.invalid/contracts/9.pdf");
        link.Identifier.ShouldBe("CONTRACT-9");
        read.SupportingDocuments.ShouldNotContain(document => document.Identifier == "CONTRACT-9");
    }

    /// <summary>
    /// A BG-24 entry that carries neither bytes nor an address is a reference by number alone.
    /// </summary>
    /// <remarks>
    /// It is a real thing to send — "see our order 4711" — and it is not something a caller can open, so it
    /// belongs in neither list. It is still on the invoice, where it always was.
    /// </remarks>
    [Theory]
    [InlineData("UBL")]
    [InlineData("CII")]
    public void AndAReferenceByNumberAloneIsInNeitherList(string syntax)
    {
        EInvoice read = RoundTripped(syntax);

        read.AdditionalDocuments.ShouldContain(document => document.Identifier.Value == "ORDER-4711");
        read.SupportingDocuments.ShouldNotContain(document => document.Identifier == "ORDER-4711");
        read.SupportingDocumentLinks.ShouldNotContain(link => link.Identifier == "ORDER-4711");
    }

    [Fact]
    public void AnInvoiceCarryingNothingHandsBackNothing()
    {
        EInvoice invoice = EInvoiceBuilder.Create(KnownProfiles.En16931Cii).WithNumber("2026-0002").Build();

        invoice.SupportingDocuments.ShouldBeEmpty();
        invoice.SupportingDocumentLinks.ShouldBeEmpty();
    }

    /// <summary>Two documents holding the same bytes are the same document, as everywhere else here.</summary>
    [Fact]
    public void ContentIsComparedByWhatItSaysRatherThanByWhereItIs()
    {
        var document = new SupportingDocument("hours"u8.ToArray(), "text/csv", "timesheet.csv");

        document.ShouldBe(new SupportingDocument("hours"u8.ToArray(), "text/csv", "timesheet.csv"));
        document.ShouldNotBe(new SupportingDocument("rates"u8.ToArray(), "text/csv", "timesheet.csv"));
    }

    private static EInvoice RoundTripped(string syntax)
    {
        DocumentFormat format = syntax == "UBL" ? DocumentFormat.Ubl : DocumentFormat.Cii;

        return Library.Read(Library.Write(AnInvoice(format), format)).RequireInvoice();
    }

    private static EInvoice AnInvoice(DocumentFormat format) =>
        EInvoiceBuilder
            .Create(format == DocumentFormat.Ubl ? KnownProfiles.En16931Ubl : KnownProfiles.En16931Cii)
            .WithNumber("2026-0002")
            .IssuedOn(new DateOnly(2026, 9, 3))
            .OfType(InvoiceTypeCodes.CommercialInvoice)
            .InCurrency("EUR")
            .From(seller => seller.Named("Vendeur SAS").WithVatIdentifier("FR40303265045"))
            .To(buyer => buyer.Named("Acheteur GmbH"))
            .Extend(invoice =>
            {
                invoice.Totals.DuePayableAmount = 960m;

                invoice.AdditionalDocuments.Add(new AdditionalDocument
                {
                    Identifier = "TIME-42",
                    Description = "Timesheet",
                    Attachment = new BinaryField(
                        Encoding.UTF8.GetBytes("hours;rate\n8;120"),
                        "text/csv",
                        "timesheet.csv"),
                });

                invoice.AdditionalDocuments.Add(new AdditionalDocument
                {
                    Identifier = "CONTRACT-9",
                    Description = "Framework contract",
                    ExternalLocation = "https://example.invalid/contracts/9.pdf",
                });

                invoice.AdditionalDocuments.Add(new AdditionalDocument { Identifier = "ORDER-4711" });
            })
            .Build();
}
