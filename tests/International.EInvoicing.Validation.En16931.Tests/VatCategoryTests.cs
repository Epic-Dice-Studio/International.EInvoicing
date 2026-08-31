using International.EInvoicing.Building;
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl.Writing;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.En16931.Tests;

/// <summary>
/// One invoice per VAT category, written by this library and judged by the official rules.
/// </summary>
/// <remarks>
/// <para>
/// EN 16931 devotes a whole family of rules to each category — <c>BR-E-*</c>, <c>BR-Z-*</c>, <c>BR-AE-*</c>,
/// <c>BR-IC-*</c>, <c>BR-G-*</c>, <c>BR-O-*</c> — and they disagree with one another on purpose: an exempt
/// invoice needs a reason and a zero rate, a reverse-charge invoice needs the buyer's VAT identifier, an
/// out-of-scope one must not carry a rate at all and may not be mixed with any other category.
/// </para>
/// <para>
/// Every invoice this library wrote in its own tests until now was standard-rated. That is one category out
/// of nine, and the eight left are where a writer is most likely to be wrong — which is what the neighbours'
/// sample sets are full of: intra-community supplies, exports, insurance without VAT, exempt medical work.
/// </para>
/// </remarks>
public class VatCategoryTests
{
    public static TheoryData<string, string> Cases => new()
    {
        { VatCategoryCodes.Standard, "UBL" },
        { VatCategoryCodes.Standard, "CII" },
        { VatCategoryCodes.ZeroRated, "UBL" },
        { VatCategoryCodes.ZeroRated, "CII" },
        { VatCategoryCodes.Exempt, "UBL" },
        { VatCategoryCodes.Exempt, "CII" },
        { VatCategoryCodes.ReverseCharge, "UBL" },
        { VatCategoryCodes.ReverseCharge, "CII" },
        { VatCategoryCodes.IntraCommunitySupply, "UBL" },
        { VatCategoryCodes.IntraCommunitySupply, "CII" },
        { VatCategoryCodes.Export, "UBL" },
        { VatCategoryCodes.Export, "CII" },
        { VatCategoryCodes.OutsideScope, "UBL" },
        { VatCategoryCodes.OutsideScope, "CII" },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void AnInvoiceInEveryVatCategorySatisfiesTheOfficialRules(string category, string syntax)
    {
        DocumentSyntax which = syntax == "UBL" ? DocumentSyntax.Ubl : DocumentSyntax.Cii;
        EInvoice invoice = AnInvoiceIn(category, which);

        string xml = which == DocumentSyntax.Ubl
            ? new UblInvoiceWriter().WriteToString(invoice)
            : new CiiInvoiceWriter().WriteToString(invoice);

        ValidationReport report = new SchematronValidator().Validate(xml, En16931Rules.For(which));

        report.IsValid.ShouldBeTrue(
            $"{category} in {syntax} was rejected:{Environment.NewLine}"
            + string.Join(
                Environment.NewLine,
                report.OfAtLeast(RuleSeverity.Error).Select(m => $"  {m.RuleIdentifier}: {m.Message}")));
    }

    /// <summary>
    /// The categories that must state why no VAT is due state it, and the ones that must not, do not.
    /// </summary>
    [Theory]
    [InlineData(VatCategoryCodes.Exempt, true)]
    [InlineData(VatCategoryCodes.ReverseCharge, true)]
    [InlineData(VatCategoryCodes.IntraCommunitySupply, true)]
    [InlineData(VatCategoryCodes.Export, true)]
    [InlineData(VatCategoryCodes.OutsideScope, true)]
    [InlineData(VatCategoryCodes.ZeroRated, false)]
    [InlineData(VatCategoryCodes.Standard, false)]
    public void AndTheReasonIsThereExactlyWhereTheNormAsksForIt(string category, bool needsReason)
    {
        VatCategoryCodes.NeedsExemptionReason(category).ShouldBe(needsReason);

        EInvoice invoice = AnInvoiceIn(category, DocumentSyntax.Ubl);

        invoice.VatBreakdown[0].ExemptionReason.IsSet.ShouldBe(needsReason);
    }

    private static EInvoice AnInvoiceIn(string category, DocumentSyntax syntax)
    {
        EInvoiceBuilder builder = EInvoiceBuilder
            .Create(syntax == DocumentSyntax.Ubl ? KnownProfiles.En16931Ubl : KnownProfiles.En16931Cii)
            .WithNumber("2026-0001")
            .IssuedOn(new DateOnly(2026, 9, 1))
            .DueOn(new DateOnly(2026, 10, 1))
            .OfType(InvoiceTypeCodes.CommercialInvoice)
            .InCurrency("EUR")
            .WithBuyerReference("REF-2026-0001")
            .From(seller => Seller(seller, category))
            .To(buyer => Buyer(buyer, category))
            .AddLine(line => Line(line, category))
            .Extend(invoice => invoice.Payment = new PaymentInstructions
            {
                MeansTypeCode = "30",
                CreditTransfers = { new CreditTransfer { AccountIdentifier = "FR7630006000011234567890189" } },
            })
            .WithComputedVatBreakdown()
            .WithComputedTotals();

        EInvoice built = builder.Build();

        if (VatCategoryCodes.NeedsExemptionReason(category))
        {
            // BR-E-10, BR-AE-10, BR-IC-10, BR-G-10 and BR-O-10: say why nothing is due, in words or by code.
            built.VatBreakdown[0].ExemptionReason = ReasonFor(category);
        }

        if (category == VatCategoryCodes.IntraCommunitySupply)
        {
            // BR-IC-11 and BR-IC-12: an intra-community supply says when and where it was delivered.
            built.Delivery = new DeliveryInformation
            {
                ActualDeliveryDate = new DateOnly(2026, 9, 1),
                Address = new PostalAddress { CountryCode = "DE" },
            };
        }

        return built;
    }

    private static InvoiceLineBuilder Line(InvoiceLineBuilder line, string category)
    {
        line
            .WithIdentifier("1")
            .WithItem("Prestation")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m);

        // "Not subject to VAT" is the one category that forbids a rate rather than requiring a zero.
        return VatCategoryCodes.ForbidsRate(category)
            ? line.WithVat(category)
            : line.WithVat(category, category == VatCategoryCodes.Standard ? 20m : 0m);
    }

    private static PartyBuilder Seller(PartyBuilder seller, string category)
    {
        seller
            .Named("Vendeur SAS")
            .WithElectronicAddress("seller@example.fr", "EM")
            .WithAddress(address =>
            {
                address.Line1 = "1 rue de la Paix";
                address.City = "Paris";
                address.PostCode = "75002";
                address.CountryCode = "FR";
            });

        // BR-O-02: an out-of-scope invoice carries no VAT identifier at all — not the seller's, not the
        // buyer's, not a tax representative's. The seller is identified some other way or not at all.
        return VatCategoryCodes.ForbidsRate(category)
            ? seller.WithLegalRegistration("303265045")
            : seller.WithVatIdentifier("FR40303265045");
    }

    private static PartyBuilder Buyer(PartyBuilder buyer, string category)
    {
        buyer
            .Named("Acheteur GmbH")
            .WithElectronicAddress("buyer@example.de", "EM")
            .WithAddress(address =>
            {
                address.Line1 = "Musterstraße 1";
                address.City = "Berlin";
                address.PostCode = "10115";
                address.CountryCode = category == VatCategoryCodes.Export ? "CH" : "DE";
            });

        // BR-AE-3 and BR-IC-3: reverse charge and intra-community supply need the buyer identified for VAT.
        return category is VatCategoryCodes.ReverseCharge or VatCategoryCodes.IntraCommunitySupply
            ? buyer.WithVatIdentifier("DE123456789")
            : buyer;
    }

    private static string ReasonFor(string category) => category switch
    {
        VatCategoryCodes.Exempt => "Exempt under article 261 of the French tax code",
        VatCategoryCodes.ReverseCharge => "Reverse charge",
        VatCategoryCodes.IntraCommunitySupply => "Intra-Community supply",
        VatCategoryCodes.Export => "Export outside the EU",
        _ => "Not subject to VAT",
    };
}
