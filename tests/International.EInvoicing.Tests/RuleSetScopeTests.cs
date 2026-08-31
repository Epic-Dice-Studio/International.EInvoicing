using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Tests;

/// <summary>
/// Which documents a rule set is allowed to judge.
/// </summary>
/// <remarks>
/// Not every UBL document is an EN 16931 invoice. Peppol PINT is built for tax systems EN 16931 was never
/// written for, and Factur-X MINIMUM says in its own specification that it is not an EN 16931 invoice.
/// Running the EN 16931 rules over either produces failures that are not failures — which is worse than
/// running nothing, because the caller cannot tell the difference. The report must say "not checked"
/// instead, and this is what holds it to that.
/// </remarks>
public class RuleSetScopeTests
{
    private static readonly EInvoicing Library =
        EInvoicing.Create(library => library.AddDefaults().AddPeppol());

    [Fact]
    public void APintDocumentIsNotJudgedByEn16931()
    {
        string xml = Library.Write(APintInvoice());

        ValidationReport report = Library.Validate(xml);

        report.RuleSets.ShouldAllBe(outcome => !outcome.Ran);
        report.IsComplete.ShouldBeFalse();
        report.Errors.ShouldBeEmpty("nothing governs this document here, so nothing may fail it");
    }

    /// <summary>The same document, put in front of EN 16931 deliberately, still can be.</summary>
    [Fact]
    public void ThoughYouCanStillAskForEn16931Explicitly()
    {
        string xml = Library.Write(APintInvoice());

        ValidationReport report = new Validation.Schematron.SchematronValidator()
            .Validate(xml, Validation.En16931.En16931Rules.For(DocumentSyntax.Ubl));

        report.RuleSets.ShouldHaveSingleItem().Ran.ShouldBeTrue();
    }

    /// <summary>And what EN 16931 does govern is still governed.</summary>
    [Fact]
    public void AnEn16931DocumentStillIs()
    {
        ValidationReport report = Library.Validate(Library.Write(AnEn16931Invoice()));

        report.RuleSets.ShouldContain(outcome => outcome.Ran);
        report.IsValid.ShouldBeTrue(
            string.Join(
                Environment.NewLine,
                report.Errors.Select(message => $"  {message.RuleIdentifier}: {message.Message}")));
    }

    /// <summary>
    /// Factur-X MINIMUM carries header data and totals only. Its own specification says it is not an
    /// EN 16931 invoice, and its identifier does not name the standard — so the rules stay out.
    /// </summary>
    [Fact]
    public void NorIsAProfileThatSaysItIsNotAnEn16931Invoice()
    {
        KnownProfiles.FacturXMinimum.Id.Value.ShouldNotContain("en16931");

        ValidationReport report = Library.Validate(Library.Write(
            EInvoiceBuilder.Create(KnownProfiles.FacturXMinimum)
                .WithNumber("FX-MIN-1")
                .IssuedOn(new DateOnly(2026, 9, 1))
                .InCurrency("EUR")
                .From("Fournisseur SARL", "FR32732829320")
                .To("Client SA", "FR89552081317")
                .Build()));

        report.RuleSets.ShouldAllBe(outcome => !outcome.Ran);
    }

    private static EInvoice APintInvoice() => EInvoiceBuilder
        .Create(PeppolPintProfiles.BillingSg)
        .ForPeppolPint()
        .WithNumber("SG-2026-001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .InCurrency("SGD")
        .From("Supplier Pte Ltd", "SG12345678A")
        .To("Customer Pte Ltd", "SG87654321B")
        .AddLine(line => line.WithItem("Consulting").WithNetAmount(1000m).WithVat("S", 9m))
        .WithComputedVatBreakdown()
        .WithComputedTotals()
        .Build();

    private static EInvoice AnEn16931Invoice() => EInvoiceBuilder
        .Create(KnownProfiles.En16931Ubl)
        .WithNumber("FA-2026-001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .OfType("380")
        .InCurrency("EUR")
        .From(seller => seller
            .Named("Fournisseur SARL")
            .WithVatIdentifier("FR32732829320")
            .WithAddress(address =>
            {
                address.Line1 = "1 rue de la Facture";     // BR-08 and BR-09: an address and a country
                address.City = "Angers";
                address.PostCode = "49000";
                address.CountryCode = "FR";
            }))
        .To(buyer => buyer
            .Named("Client SA")
            .WithVatIdentifier("FR89552081317")
            .WithAddress(address =>
            {
                address.City = "Nantes";
                address.CountryCode = "FR";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Conseil")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m)
            .WithVat("S", 20m))
        .WithComputedVatBreakdown()
        .WithComputedTotals()
        .Build();
}
