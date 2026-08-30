using International.EInvoicing.Building;
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.Countries.Belgium.Identifiers;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl.Writing;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.En16931;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Peppol.Tests;

/// <summary>
/// A Peppol invoice this library <em>writes</em>, put in front of the Peppol rules.
/// </summary>
/// <remarks>
/// The unit corpus proves the engine reads Peppol's rules the way Peppol means them. This asks the other
/// question — whether what we produce survives them — which is where an implementation is most likely to be
/// wrong on its own terms, since no document from elsewhere is there to disagree.
/// </remarks>
public class GeneratedInvoiceTests
{
    public static TheoryData<string> Syntaxes => new("UBL", "CII");

    [Theory]
    [MemberData(nameof(Syntaxes))]
    public void APeppolInvoiceThisLibraryWritesSatisfiesThePeppolRules(string syntax)
    {
        string directory = Path.Combine(RepositoryRoot(), "specs", "peppol", "rules");

        Assert.SkipWhen(
            !Directory.Exists(directory),
            "The Peppol artefacts are not present; run build/fetch-specs.sh peppol.");

        string xml = Write(ABelgianInvoice(syntax), syntax);
        var validator = new SchematronValidator();
        var ran = 0;

        // Both of Peppol's rule sets apply: its own, and its copy of the EN 16931 ones.
        foreach (string name in PeppolProfiles.RuleSetFileNames.Where(file => file.Contains(syntax, StringComparison.Ordinal)))
        {
            string path = Path.Combine(directory, name);

            if (!File.Exists(path))
            {
                continue;
            }

            SchematronRuleSet rules = SchematronRuleSet.Load(File.ReadAllText(path), name, "3.0");
            ValidationReport report = validator.Validate(xml, rules);

            report.IsValid.ShouldBeTrue(Describe(name, report));
            ran++;
        }

        ran.ShouldBe(2, $"both Peppol {syntax} rule sets should have run");
    }

    [Theory]
    [MemberData(nameof(Syntaxes))]
    public void AndTheBaseRulesItRestricts(string syntax)
    {
        DocumentSyntax which = syntax == "UBL" ? DocumentSyntax.Ubl : DocumentSyntax.Cii;

        ValidationReport report = new SchematronValidator()
            .Validate(Write(ABelgianInvoice(syntax), syntax), En16931Rules.For(which));

        report.IsValid.ShouldBeTrue(Describe($"EN 16931 ({syntax})", report));
    }

    /// <summary>Belgium identifies both parties by enterprise number, in the Peppol scheme for it.</summary>
    [Fact]
    public void TheBelgianEnterpriseNumberTravelsInItsOwnScheme()
    {
        EInvoice invoice = ABelgianInvoice("UBL");

        invoice.Seller!.ElectronicAddress.SchemeId.ShouldBe(PeppolEndpointScheme.BelgianEnterprise);
        BeEnterpriseNumber.IsValid(invoice.Seller.ElectronicAddress.Value).ShouldBeTrue();
    }

    private static EInvoice ABelgianInvoice(string syntax)
    {
        PeppolParticipant seller = PeppolParticipant.Create(PeppolEndpointScheme.BelgianEnterprise, "0203201340");
        PeppolParticipant buyer = PeppolParticipant.Create(PeppolEndpointScheme.BelgianEnterprise, "0776914174");

        return EInvoiceBuilder
            .Create(syntax == "UBL" ? PeppolProfiles.BillingUbl : PeppolProfiles.BillingCii)
            .WithNumber("FA-2026-001")
            .IssuedOn(new DateOnly(2026, 9, 1))
            .DueOn(new DateOnly(2026, 10, 1))
            .OfType("380")
            .InCurrency("EUR")
            .WithBuyerReference("PO-4417")
            .ForPeppol()
            .From(party => party
                .Named("Verkoper BV")
                .WithVatIdentifier("BE0203201340")
                .WithElectronicAddress(seller.Value, seller.Scheme)
                .WithAddress(address =>
                {
                    address.Line1 = "Nijverheidsstraat 1";
                    address.City = "Antwerpen";
                    address.PostCode = "2000";
                    address.CountryCode = "BE";
                }))
            .To(party => party
                .Named("Koper NV")
                .WithVatIdentifier("BE0776914174")
                .WithElectronicAddress(buyer.Value, buyer.Scheme)
                .WithAddress(address =>
                {
                    address.Line1 = "Havenlaan 2";
                    address.City = "Brussel";
                    address.PostCode = "1000";
                    address.CountryCode = "BE";
                }))
            .AddLine(line => line
                .WithIdentifier("1")
                .WithItem("Advies")
                .WithQuantity(3m, "HUR")
                .WithNetPrice(150m)
                .WithNetAmount(450m)
                .WithVat("S", 21m))
            .Extend(invoice => invoice.Payment = new PaymentInstructions
            {
                MeansTypeCode = "30",
                RemittanceInformation = BeStructuredCommunication.ForInvoice(2026_000_001).ToString(),
                CreditTransfers = { new CreditTransfer { AccountIdentifier = "BE68539007547034" } },
            })
            .WithComputedVatBreakdown()
            .WithComputedTotals()
            .Build();
    }

    private static string Write(EInvoice invoice, string syntax) =>
        syntax == "UBL"
            ? new UblInvoiceWriter().WriteToString(invoice)
            : new CiiInvoiceWriter().WriteToString(invoice);

    private static string Describe(string what, ValidationReport report) =>
        $"{what} rejected the invoice this library wrote:{Environment.NewLine}"
        + string.Join(
            Environment.NewLine,
            report.OfAtLeast(RuleSeverity.Error).Select(message => $"  {message.RuleIdentifier}: {message.Message}"));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
