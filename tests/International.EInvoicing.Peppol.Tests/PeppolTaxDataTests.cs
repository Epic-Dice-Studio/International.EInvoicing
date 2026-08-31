using International.EInvoicing.Building;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol.TaxData;
using International.EInvoicing.Peppol.TaxData.Model;
using International.EInvoicing.Peppol.TaxData.Writing;
using International.EInvoicing.Validation;
using International.EInvoicing.Values;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Peppol.Tests;

/// <summary>
/// The tax data document, judged by the assertions OpenPeppol publishes per jurisdiction.
/// </summary>
/// <remarks>
/// <para>
/// No schema is published with those rules, so the element order this library writes is the one the rules
/// themselves enumerate. That is why nothing here compares the output to a fixture: a fixture would only
/// prove this library agrees with itself. What is measured is what the publisher's rules say.
/// </para>
/// <para>
/// Slovakia's rule set and the EU's ViDA one differ by a single assertion out of eighty-eight, and by a
/// namespace and an identifier. That is why one writer serves both, and why both are run here rather than
/// one being trusted to stand for the other.
/// </para>
/// </remarks>
public class PeppolTaxDataTests
{
    private static readonly string Slovakia = Artefacts("sk");

    private static readonly string ViDA = Artefacts("vida");

    [Fact]
    public void ATaxDataDocumentThisLibraryWritesSatisfiesThePublishedRules()
    {
        ValidationReport report = Validate(new PeppolTaxDataWriter().WriteToString(ATaxDataDocument()));

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
        string document = new PeppolTaxDataWriter().WriteToString(ATaxDataDocument());
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
        PeppolTaxData taxData = ATaxDataDocument();
        taxData.ReportedDocument!.BuyerReference = "PO-2026-77";
        taxData.ReportedDocument.PaymentTerms = "30 dní";
        taxData.ReportedDocument.DueDate = new DateOnly(2026, 10, 1);

        string document = new PeppolTaxDataWriter().WriteToString(taxData);

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
        PeppolTaxData taxData = ATaxDataDocument();
        taxData.ReceivingParty.SchemeId = "0158";

        ValidationReport report = Validate(new PeppolTaxDataWriter().WriteToString(taxData));

        report.OfAtLeast(RuleSeverity.Error)
            .ShouldContain(message => message.RuleIdentifier == "ibr-tdd-20", Describe(report));
        PeppolTaxDataEndpoint.ServiceProviderScheme.ShouldBe("0242");
    }

    /// <summary>The same document, reported to the EU instead, and the ViDA rules accept it too.</summary>
    [Fact]
    public void TheSameWriterServesViDa()
    {
        PeppolTaxData taxData = ATaxDataDocument();
        taxData.Jurisdiction = PeppolTaxDataJurisdiction.ViDA;

        string document = new PeppolTaxDataWriter().WriteToString(taxData);

        document.ShouldContain("urn:peppol:schema:vida-taxdata:1.0");
        document.ShouldContain("<cbc:CustomizationID>urn:peppol:taxdata:vida-1</cbc:CustomizationID>");

        ValidationReport report = Validate(document, ViDA, PeppolTaxDataJurisdiction.ViDA);

        report.IsValid.ShouldBeTrue(Describe(report));
    }

    /// <summary>
    /// A document put in front of another jurisdiction's rules is not judged, and the report says so.
    /// </summary>
    /// <remarks>
    /// The two rule sets are the same rules in different namespaces, so the ViDA set matches nothing at all
    /// in a Slovak document. "Valid" would be the worst possible answer, and it was the answer until the
    /// validator started reporting a rule set that claimed no node as one that did not run.
    /// </remarks>
    [Fact]
    public void AndAJurisdictionPutInFrontOfTheWrongRulesIsNotJudged()
    {
        string slovak = new PeppolTaxDataWriter().WriteToString(ATaxDataDocument());

        ValidationReport report = Validate(slovak, ViDA, PeppolTaxDataJurisdiction.ViDA);

        report.IsComplete.ShouldBeFalse();
        report.RuleSets.ShouldContain(set => !set.Ran && set.SkippedBecause!.Contains("matched"));
    }

    [Fact]
    public void TheCodeListsAreTheOnesTheRulesCarry()
    {
        foreach (PeppolTaxDataJurisdiction jurisdiction in PeppolTaxDataJurisdiction.All)
        {
            jurisdiction.TaxDataTypes.ShouldBe(["S", "R", "D"]);
            jurisdiction.DocumentScopes.ShouldBe(["D", "IC", "INTL"]);
            jurisdiction.ReporterRoles.ShouldBe(["C2", "C3"]);
            jurisdiction.CustomizationId.ShouldStartWith("urn:peppol:taxdata:");
        }

        PeppolTaxDataJurisdiction slovakia = PeppolTaxDataJurisdiction.Slovakia;

        PeppolTaxDataJurisdiction.IsValid(slovakia.ReporterRoles, "C3").ShouldBeTrue();
        PeppolTaxDataJurisdiction.IsValid(slovakia.ReporterRoles, "c3").ShouldBeFalse();
        PeppolTaxDataJurisdiction.IsValid(slovakia.ReporterRoles, null).ShouldBeFalse();
    }

    /// <summary>The time of issue carries its offset, which <c>ibr-tdd-05</c> requires and a date never has.</summary>
    [Fact]
    public void TheIssueTimeCarriesItsOffsetAndTheIssueDateDoesNot()
    {
        string document = new PeppolTaxDataWriter().WriteToString(ATaxDataDocument());

        document.ShouldContain("<cbc:IssueDate>2026-09-01</cbc:IssueDate>");
        document.ShouldContain("<cbc:IssueTime>09:15:00+02:00</cbc:IssueTime>");
    }

    private static ValidationReport Validate(string document) =>
        Validate(document, Slovakia, PeppolTaxDataJurisdiction.Slovakia);

    private static ValidationReport Validate(
        string document,
        string artefacts,
        PeppolTaxDataJurisdiction jurisdiction)
    {
        Assert.SkipWhen(!Directory.Exists(artefacts), "run build/fetch-specs.sh national");

        return PeppolTaxDataValidator.LoadFrom(artefacts, jurisdiction).Validate(document);
    }

    private static string Artefacts(string jurisdiction) => Path.Combine(
        RepositoryRoot(), "specs", "national", "peppol-taxdata", "schematron", "tdd", jurisdiction, "1.0.0");

    private static PeppolTaxData ATaxDataDocument() => new()
    {
        Uuid = "0f3a2d64-9d21-4a7e-8f2f-2f2a3f0f1a11",
        IssuedAt = new DateTimeOffset(2026, 9, 1, 9, 15, 0, TimeSpan.FromHours(2)),
        TaxDataTypeCode = "S",
        DocumentScope = "D",
        ReporterRole = "C2",
        Authority = new PeppolTaxAuthority { Id = "SK-FS", Name = "Finančné riaditeľstvo Slovenskej republiky" },
        ReportingParty = new PeppolTaxDataEndpoint { Id = "0000000000", SchemeId = "0158" },
        ReceivingParty = new PeppolTaxDataEndpoint { Id = "1111111111", SchemeId = PeppolTaxDataEndpoint.ServiceProviderScheme },
        ReportedDocumentUuid = "1a2b3c4d-5e6f-4071-8a9b-0c1d2e3f4a5b",
        ReportedDocument = AnInvoice(),
    };

    private static EInvoice AnInvoice() => EInvoiceBuilder
        .Create(PeppolProfiles.BillingUbl)
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
