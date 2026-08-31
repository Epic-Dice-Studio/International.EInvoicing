using International.EInvoicing.Building;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Tests;

/// <summary>
/// The business terms UBL used to lose, in both directions.
/// </summary>
/// <remarks>
/// <para>
/// Each of these is an EN 16931 term the model has always held and the UBL side handled in neither
/// direction: a caller who set one got a document without it, and a document that carried one was read with
/// the field empty. The CII side read and wrote most of them, so converting CII to UBL dropped them too.
/// </para>
/// <para>
/// They were found by validating a read-then-write of the official examples against the schema: an element
/// nobody maps is kept verbatim and written back at the end of its node, which UBL refuses. The shape defect
/// was the symptom; this is what was actually missing.
/// </para>
/// </remarks>
public class RecoveredTermsTests
{
    private static readonly EInvoicing Library = EInvoicing.Create(builder => builder.AddDefaults());

    [Theory]
    [InlineData("UBL")]
    [InlineData("CII")]
    public void EveryTermSurvivesBeingWrittenAndReadBack(string syntax)
    {
        DocumentFormat format = syntax == "UBL" ? DocumentFormat.Ubl : DocumentFormat.Cii;

        EInvoice read = Library.Read(Library.Write(AnInvoice(format), format)).RequireInvoice();

        read.DespatchAdviceReference.Value.ShouldBe("DESP-9", "BT-16");
        read.ReceivingAdviceReference.Value.ShouldBe("RECV-9", "BT-15");
        read.Payment!.DirectDebit!.MandateReference.Value.ShouldBe("MANDATE-9", "BT-89");
        read.Payment.DirectDebit.DebitedAccountIdentifier.Value.ShouldBe("FR7630006000011234567890189", "BT-91");
        read.Lines[0].ObjectIdentifier.Value.ShouldBe("OBJ-9", "BT-128");
    }

    /// <summary>BT-17 and BT-111, which UBL carries and CII files elsewhere or not at all.</summary>
    [Fact]
    public void AndTheTwoThatOnlyUblCarries()
    {
        EInvoice read = Library
            .Read(Library.Write(AnInvoice(DocumentFormat.Ubl), DocumentFormat.Ubl))
            .RequireInvoice();

        read.TenderOrLotReference.Value.ShouldBe("LOT-9", "BT-17");
        read.Totals.TaxAmountInAccountingCurrency.Value.ShouldBe(190m, "BT-111");
    }

    /// <summary>
    /// An attachment is written once.
    /// </summary>
    /// <remarks>
    /// It was read into the model and kept as extension data as well, so every rewrite carried it twice —
    /// which for a scanned delivery note is megabytes, and a cardinality the schema refuses.
    /// </remarks>
    [Fact]
    public void AnAttachmentIsNotWrittenTwice()
    {
        EInvoice invoice = AnInvoice(DocumentFormat.Ubl);
        invoice.AdditionalDocuments.Add(new AdditionalDocument
        {
            Identifier = "DOC-1",
            Attachment = new BinaryField([1, 2, 3, 4], "application/pdf", "note.pdf"),
        });

        string once = Library.Write(invoice, DocumentFormat.Ubl);
        string twice = Library.Write(Library.Read(once).RequireInvoice(), DocumentFormat.Ubl);

        Count(twice, "EmbeddedDocumentBinaryObject").ShouldBe(Count(once, "EmbeddedDocumentBinaryObject"));
        Library.Read(twice).RequireInvoice().AdditionalDocuments.Count(document => document.Attachment.IsSet)
            .ShouldBe(1);
    }

    private static int Count(string text, string needle) => text.Split(needle).Length - 1;

    private static EInvoice AnInvoice(DocumentFormat format) => EInvoiceBuilder
        .Create(format == DocumentFormat.Ubl ? KnownProfiles.En16931Ubl : KnownProfiles.En16931Cii)
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType(InvoiceTypeCodes.CommercialInvoice)
        .InCurrency("EUR")
        .WithBuyerReference("REF-2026-0001")
        .From(seller => seller
            .Named("Vendeur SAS")
            .WithVatIdentifier("FR40303265045")
            .WithElectronicAddress("seller@example.fr", "EM")
            .WithAddress(address =>
            {
                address.Line1 = "1 rue de la Paix";
                address.City = "Paris";
                address.PostCode = "75002";
                address.CountryCode = "FR";
            }))
        .To(buyer => buyer
            .Named("Acheteur GmbH")
            .WithElectronicAddress("buyer@example.de", "EM")
            .WithAddress(address =>
            {
                address.Line1 = "Musterstraße 1";
                address.City = "Berlin";
                address.PostCode = "10115";
                address.CountryCode = "DE";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Prestation")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m)
            .WithVat("S", 19m)
            .Extend(line => line.ObjectIdentifier = "OBJ-9"))
        .Extend(invoice =>
        {
            invoice.DespatchAdviceReference = "DESP-9";
            invoice.ReceivingAdviceReference = "RECV-9";
            invoice.TenderOrLotReference = "LOT-9";
            invoice.TaxAccountingCurrencyCode = "EUR";
            invoice.Payment = new PaymentInstructions
            {
                MeansTypeCode = "59",
                CreditTransfers = { new CreditTransfer { AccountIdentifier = "FR7630006000011234567890189" } },
                DirectDebit = new DirectDebit
                {
                    MandateReference = "MANDATE-9",
                    DebitedAccountIdentifier = "FR7630006000011234567890189",
                },
            };
        })
        .WithComputedVatBreakdown()
        .WithComputedTotals()
        .Extend(invoice => invoice.Totals.TaxAmountInAccountingCurrency = 190m)
        .Build();
}
