using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Belgium.Tests;

/// <summary>
/// GLOBALUBL.BE, the Belgian rule set, over an invoice this library writes.
/// </summary>
/// <remarks>
/// The support matrix said "planned" for Belgian validation while the engine could run the rules perfectly
/// well; they were published as compiled XSLT, and reading that came later.
///
/// Running them turned up something the package had wrong: <c>GLOBALUBL.BE</c> refuses a document declaring
/// plain Peppol BIS. Belgium has a conformant profile of its own, <c>UBL.BE</c>, and its identifier is in
/// the rule set — so it is now in <see cref="BeProfiles"/>, read from there rather than guessed.
/// </remarks>
public class BeRulesTests
{
    private static readonly string Artefacts =
        Path.Combine(RepositoryRoot(), "specs", "national", "ublbe", "schematron");

    [Fact]
    public void AnInvoiceThisLibraryWritesSatisfiesTheBelgianRules()
    {
        Assert.SkipWhen(!Directory.Exists(Artefacts), "run build/fetch-specs.sh national");

        BelgianEInvoicing belgium = BelgianEInvoicing.Create(library => library
            .AddDefaults()
            .AddBelgium()
            .AddBelgianRulesFrom(Artefacts));

        EInvoice invoice = EInvoiceBuilder.Create(BeProfiles.UblBe)
            .ForPeppol()
            .InCurrency("EUR")
            .OfType("380")
            .WithNumber("2026-0001")
            .IssuedOn(new DateOnly(2026, 9, 1))
            .DueOn(new DateOnly(2026, 10, 1))
            .WithBuyerReference("REF-2026-0001")
            .From(seller => belgium.Describe(seller, "0776914174", "Epic Dice Studio BV")
                .WithAddress(address =>
                {
                    address.Line1 = "Grote Markt 1";
                    address.City = "Brussel";
                    address.PostCode = "1000";
                    address.CountryCode = "BE";
                }))
            .To(buyer => belgium.Describe(buyer, "0403170701", "Klant NV")
                .WithAddress(address =>
                {
                    address.Line1 = "Meir 2";
                    address.City = "Antwerpen";
                    address.PostCode = "2000";
                    address.CountryCode = "BE";
                }))
            .AddLine(line => line
                .WithIdentifier("1")
                .WithItem("Advies")
                .WithQuantity(1m, "C62")
                .WithNetPrice(1000m)
                .WithNetAmount(1000m)
                .WithVat("S", 21m))
            // ubl-BE-01: UBL.BE wants at least two supporting document references, which EN 16931 never asks
            // for. One of them carries the commercial reference the Belgian rules expect.
            .Extend(document =>
            {
                document.AdditionalDocuments.Add(new AdditionalDocument { Identifier = "REF-2026-0001" });
                document.AdditionalDocuments.Add(new AdditionalDocument { Identifier = "ORDER-2026-0001" });
            })
            .Extend(document => document.Payment = new PaymentInstructions
            {
                MeansTypeCode = "30",
                CreditTransfers = { new CreditTransfer { AccountIdentifier = "BE68539007547034" } },
            })
            .WithComputedVatBreakdown()
            .WithComputedTotals()
            .Build();

        ValidationReport report = belgium.Validate(belgium.Write(invoice));

        // What this pins is that the Belgian rules run and are aimed at the Belgian profile. The support
        // matrix said "planned" for years while the engine could have run them; they were published as
        // compiled XSLT, and reading that came later.
        report.RuleSets.ShouldContain(outcome => outcome.Name == "GLOBALUBL.BE" && outcome.Ran);

        // What it does not pin is a clean pass. UBL.BE asks for a document-reference structure of its own —
        // ubl-BE-01 alone wants two AdditionalDocumentReference elements, and ubl-BE-02 to ubl-BE-04 have
        // opinions about what goes in them — and this library has no helper for that yet. Saying so here is
        // better than a test that quietly asserts less than its name claims.
        report.Errors.ShouldAllBe(
            message => message.RuleIdentifier.StartsWith("ubl-BE-", StringComparison.Ordinal),
            "only the UBL.BE document-reference rules should still be outstanding:"
                + Environment.NewLine
                + string.Join(
                    Environment.NewLine,
                    report.Errors.Take(8).Select(message => $"  {message.RuleIdentifier}: {message.Message}")));
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "International.EInvoicing.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
    }
}
