using International.EInvoicing.Building;
using International.EInvoicing.Countries.Croatia.Identifiers;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Croatia.Tests;

/// <summary>
/// What the Croatian shortcut promises, and what can honestly be measured about it.
/// </summary>
/// <remarks>
/// Croatia's mandate needs the OIB of both parties. What is measured here is that an invoice this library
/// writes carries them and still satisfies the layer underneath the mandate — Peppol BIS and EN 16931 —
/// since Croatia publishes its own HR-FISK rules where this repository cannot fetch them.
/// </remarks>
public class CroatianEInvoicingTests
{
    private static readonly CroatianEInvoicing Hrvatska = CroatianEInvoicing.Create();

    [Fact]
    public void AnInvoiceBuiltHereDeclaresPeppolBillingAndItsBusinessProcess()
    {
        EInvoice invoice = AnInvoice();

        invoice.SpecificationIdentifier.Value.ShouldBe(HrProfiles.PeppolBillingUbl.Id.Value);
        invoice.BusinessProcessType.Value.ShouldBe(PeppolBusinessProcess.Billing);
        invoice.CurrencyCode.Value.ShouldBe("EUR");
    }

    [Fact]
    public void BothPartiesCarryTheirOib()
    {
        EInvoice invoice = AnInvoice();

        invoice.Seller!.LegalRegistrationIdentifier.Value.ShouldBe(Seller);
        invoice.Buyer!.LegalRegistrationIdentifier.Value.ShouldBe(Buyer);
        invoice.Seller.VatIdentifier.Value.ShouldBe("HR" + Seller);

        // 9934 is an EAS code, so it belongs on the endpoint. BR-CL-11 wants an ISO 6523 ICD code on a
        // registration identifier, and refuses the invoice when it finds an EAS one there instead.
        invoice.Seller.ElectronicAddress.SchemeId.ShouldBe(HrOib.Scheme);
        invoice.Seller.LegalRegistrationIdentifier.SchemeId.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void ANumberThatIsNotAnOibIsRefusedHere() =>
        Should.Throw<FormatException>(
            () => Hrvatska.Invoice().From(seller => Hrvatska.Describe(seller, "12345678901", "Krivo d.o.o.")));

    /// <summary>The measurement: the rules the mandate builds on, over an invoice this library wrote.</summary>
    [Fact]
    public void AnInvoiceThisLibraryWritesSatisfiesTheRulesUnderneathTheMandate()
    {
        ValidationReport report = new SchematronValidator().Validate(Hrvatska.Write(AnInvoice()), PeppolRules());

        report.IsValid.ShouldBeTrue(
            string.Join(
                Environment.NewLine,
                report.OfAtLeast(RuleSeverity.Error).Select(message => $"  {message.RuleIdentifier}: {message.Message}")));
    }

    [Fact]
    public void WhatItWritesItReadsBack() =>
        Hrvatska.Read(Hrvatska.Write(AnInvoice())).RequireInvoice().Number.Value.ShouldBe("2026-0001");

    [Fact]
    public void TheWholeLibraryStaysReachable() => Hrvatska.Library.Ubl.Syntax.ShouldBe(DocumentSyntax.Ubl);

    private static string Seller => HrOibTests.ValidNumbers[0];

    private static string Buyer => HrOibTests.ValidNumbers[1];

    private static SchematronRuleSet PeppolRules()
    {
        string path = Path.Combine(RepositoryRoot(), "specs", "peppol", "rules", "PEPPOL-EN16931-UBL.sch");

        Assert.SkipWhen(
            !File.Exists(path),
            "The Peppol artefacts are not present; run build/fetch-specs.sh peppol.");

        return SchematronRuleSet.Load(File.ReadAllText(path), "Peppol BIS Billing 3.0 (UBL)", "3.0");
    }

    private static EInvoice AnInvoice() => Hrvatska.Invoice()
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .WithBuyerReference("REF-2026-0001")
        .From(seller => Hrvatska.Describe(seller, Seller, "Dobavljač d.o.o.")
            .WithAddress(address =>
            {
                address.Line1 = "Ilica 1";
                address.City = "Zagreb";
                address.PostCode = "10000";
                address.CountryCode = "HR";
            }))
        .To(buyer => Hrvatska.Describe(buyer, Buyer, "Kupac d.o.o.")
            .WithAddress(address =>
            {
                address.Line1 = "Riva 2";
                address.City = "Split";
                address.PostCode = "21000";
                address.CountryCode = "HR";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Savjetovanje")
            .WithQuantity(3m, "HUR")
            .WithNetPrice(1000m)
            .WithNetAmount(3000m)
            .WithVat("S", 25m))
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "HR1210010051863000160" } },
        })
        .WithComputedVatBreakdown()
        .WithComputedTotals()
        .Build();

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
