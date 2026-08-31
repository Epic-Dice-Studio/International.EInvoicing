using International.EInvoicing.Building;
using International.EInvoicing.Countries.Slovakia.TaxData;
using International.EInvoicing.Countries.Slovakia.TaxData.Model;
using International.EInvoicing.Countries.Slovakia.TaxData.Writing;
using International.EInvoicing.Countries.Slovakia.Validation;
using International.EInvoicing.Model;
using International.EInvoicing.Validation;
using International.EInvoicing.Values;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Slovakia.Tests;

/// <summary>
/// The Slovak tax data document, judged by the 88 assertions OpenPeppol publishes for it.
/// </summary>
/// <remarks>
/// No schema is published with those rules, so the element order this library writes is the one the rules
/// themselves enumerate. That is why nothing here compares the output to a fixture: a fixture would only
/// prove this library agrees with itself. What is measured is what the publisher's rules say.
/// </remarks>
public class SkTaxDataTests
{
    private static readonly string Artefacts = Path.Combine(
        RepositoryRoot(), "specs", "national", "peppol-taxdata", "schematron", "tdd", "sk", "1.0.0");

    [Fact]
    public void ATaxDataDocumentThisLibraryWritesSatisfiesThePublishedRules()
    {
        ValidationReport report = Validate(new SkTaxDataWriter().WriteToString(ATaxDataDocument()));

        report.IsValid.ShouldBeTrue(Describe(report));
    }

    /// <summary>The proof the rules ran: the same document, with one thing wrong, is refused for that thing.</summary>
    [Theory]
    [InlineData("urn:peppol:taxdata:sk-1", "urn:peppol:taxdata:xx-1", "ibr-tdd-01")]
    [InlineData("<pxs:TaxDataTypeCode>S</pxs:TaxDataTypeCode>", "<pxs:TaxDataTypeCode>X</pxs:TaxDataTypeCode>", "ibr-tdd-06")]
    [InlineData("<pxs:DocumentScope>D</pxs:DocumentScope>", "<pxs:DocumentScope>XX</pxs:DocumentScope>", "ibr-tdd-08")]
    [InlineData("<pxs:ReporterRole>C2</pxs:ReporterRole>", "<pxs:ReporterRole>C9</pxs:ReporterRole>", "ibr-tdd-09")]
    public void AndWhereADocumentIsWrongTheRuleThatSaysSoFires(string original, string broken, string rule)
    {
        string document = new SkTaxDataWriter().WriteToString(ATaxDataDocument());
        document.ShouldContain(original);

        ValidationReport report = Validate(document.Replace(original, broken, StringComparison.Ordinal));

        report.OfAtLeast(RuleSeverity.Error)
            .ShouldContain(message => message.RuleIdentifier == rule, Describe(report));
    }

    /// <summary>
    /// The reported document is a projection, not a copy: what the invoice carries beyond the allowed set is
    /// dropped rather than passed through, because passing it through is what makes the document fail.
    /// </summary>
    [Fact]
    public void WhatTheInvoiceCarriesBeyondTheAllowedSetDoesNotTravel()
    {
        SkTaxData taxData = ATaxDataDocument();
        taxData.ReportedDocument!.BuyerReference = "PO-2026-77";
        taxData.ReportedDocument.PaymentTerms = "30 dní";
        taxData.ReportedDocument.DueDate = new DateOnly(2026, 10, 1);

        string document = new SkTaxDataWriter().WriteToString(taxData);

        document.ShouldNotContain("PO-2026-77");
        document.ShouldNotContain("30 dní");
        document.ShouldNotContain("DueDate");
        Validate(document).IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// The receiver of a tax data document is a service provider, and the rules say so by scheme.
    /// </summary>
    [Fact]
    public void TheReceivingPartyIsIdentifiedAsAServiceProvider()
    {
        SkTaxData taxData = ATaxDataDocument();
        taxData.ReceivingParty.SchemeId = "0158";

        ValidationReport report = Validate(new SkTaxDataWriter().WriteToString(taxData));

        report.OfAtLeast(RuleSeverity.Error)
            .ShouldContain(message => message.RuleIdentifier == "ibr-tdd-20", Describe(report));
        SkTaxDataEndpoint.ServiceProviderScheme.ShouldBe("0242");
    }

    [Fact]
    public void TheCodeListsAreTheOnesTheRulesCarry()
    {
        SkTaxDataCodes.TaxDataTypes.ShouldBe(["S", "R", "D"]);
        SkTaxDataCodes.DocumentScopes.ShouldBe(["D", "IC", "INTL"]);
        SkTaxDataCodes.ReporterRoles.ShouldBe(["C2", "C3"]);

        SkTaxDataCodes.IsValid(SkTaxDataCodes.ReporterRoles, "C3").ShouldBeTrue();
        SkTaxDataCodes.IsValid(SkTaxDataCodes.ReporterRoles, "c3").ShouldBeFalse();
        SkTaxDataCodes.IsValid(SkTaxDataCodes.ReporterRoles, null).ShouldBeFalse();
    }

    /// <summary>The time of issue carries its offset, which <c>ibr-tdd-05</c> requires and a date never has.</summary>
    [Fact]
    public void TheIssueTimeCarriesItsOffsetAndTheIssueDateDoesNot()
    {
        string document = new SkTaxDataWriter().WriteToString(ATaxDataDocument());

        document.ShouldContain("<cbc:IssueDate>2026-09-01</cbc:IssueDate>");
        document.ShouldContain("<cbc:IssueTime>09:15:00+02:00</cbc:IssueTime>");
    }

    private static ValidationReport Validate(string document)
    {
        Assert.SkipWhen(!Directory.Exists(Artefacts), "run build/fetch-specs.sh national");

        return SkTaxDataValidator.LoadFrom(Artefacts).Validate(document);
    }

    private static SkTaxData ATaxDataDocument() => new()
    {
        Uuid = "0f3a2d64-9d21-4a7e-8f2f-2f2a3f0f1a11",
        IssuedAt = new DateTimeOffset(2026, 9, 1, 9, 15, 0, TimeSpan.FromHours(2)),
        TaxDataTypeCode = "S",
        DocumentScope = "D",
        ReporterRole = "C2",
        Authority = new SkTaxAuthority { Id = "SK-FS", Name = "Finančné riaditeľstvo Slovenskej republiky" },
        ReportingParty = new SkTaxDataEndpoint { Id = "0000000000", SchemeId = "0158" },
        ReceivingParty = new SkTaxDataEndpoint { Id = "1111111111", SchemeId = SkTaxDataEndpoint.ServiceProviderScheme },
        ReportedDocumentUuid = "1a2b3c4d-5e6f-4071-8a9b-0c1d2e3f4a5b",
        ReportedDocument = AnInvoice(),
    };

    private static EInvoice AnInvoice() => EInvoiceBuilder
        .Create(SkProfiles.PeppolBillingUbl)
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .OfType("380")
        .InCurrency("EUR")
        .From(seller => seller
            .Named("Dodávateľ s.r.o.")
            .WithVatIdentifier("SK2020123456")
            .WithAddress(address => address.CountryCode = "SK"))
        .To(buyer => buyer
            .Named("Odberateľ s.r.o.")
            .WithVatIdentifier("SK2020654321")
            .WithAddress(address => address.CountryCode = "SK"))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Poradenstvo")
            .WithQuantity(3m, "HUR")
            .WithNetPrice(100m)
            .WithNetAmount(300m)
            .WithVat("S", 23m)
            .Extend(line => line.Item!.ClassificationCodes.Add(new CodeField("70.20.11", ListId: "CG"))))
        .WithComputedVatBreakdown()
        .WithComputedTotals()
        .Build();

    private static string Describe(ValidationReport report) =>
        string.Join(
            Environment.NewLine,
            report.OfAtLeast(RuleSeverity.Error).Select(message => $"  {message.RuleIdentifier}: {message.Message}"));

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
