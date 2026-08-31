using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Model;
using International.EInvoicing.Values;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Croatia.Tests;

/// <summary>
/// CIUS-HR 2025 with its extension — what <em>Fiskalizacija 2.0</em> exchanges — and how far an invoice this
/// library writes gets through the rules that judge it.
/// </summary>
/// <remarks>
/// Croatia's rules were recorded here as unobtainable. They were unfetched: the publisher's compiled XSLT is
/// aggregated by phive-rules, which this repository already fetches for four other countries. Seventy-four
/// assertions, and the specification identifier that had been missing, came out of the same file.
/// </remarks>
public class CiusHrTests
{
    private static readonly string Artefacts =
        Path.Combine(RepositoryRoot(), "specs", "national", "eracun", "schematron");

    [Fact]
    public void TheIdentifierIsTheOneThePublishedRulesTest()
    {
        Assert.SkipWhen(!Directory.Exists(Artefacts), "run build/fetch-specs.sh national");

        string rules = string.Concat(Directory
            .EnumerateFiles(Artefacts, "*.xslt", SearchOption.AllDirectories)
            .Select(File.ReadAllText));

        rules.ShouldContain(HrProfiles.CiusHrUbl.Id.Value);
        HrProfiles.CiusHrUbl.Id.Value.ShouldBe(
            "urn:cen.eu:en16931:2017#compliant#urn:mfin.gov.hr:cius-2025:1.0"
            + "#conformant#urn:mfin.gov.hr:ext-2025:1.0");
    }

    /// <summary>
    /// What CIUS-HR still asks for, named rule by rule.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An invoice this library writes satisfies seventy-one of the seventy-four assertions. The three it does
    /// not are all UBL elements that EN 16931 does not define, so the canonical model has nowhere to put them:
    /// </para>
    /// <list type="bullet">
    /// <item><c>HR-BR-2</c> — <c>cbc:IssueTime</c>, the time of issue (HR-BT-2).</item>
    /// <item><c>HR-BR-37</c> — <c>cac:SellerContact/cbc:Name</c>, the operator who issued it (HR-BT-4).</item>
    /// <item><c>HR-BR-9</c> — <c>cac:SellerContact/cbc:ID</c>, that operator's OIB (HR-BT-5).</item>
    /// </list>
    /// <para>
    /// This test is the record of that gap, and it fails the day any of it changes — including the day one of
    /// the three starts being written, which is the point.
    /// </para>
    /// </remarks>
    [Fact]
    public void WhatIsStillMissingIsThreeElementsTheNormDoesNotDefine()
    {
        ValidationReport report = Validate(AnInvoice());

        report.RuleSets.ShouldContain(
            outcome => outcome.Name.StartsWith("CIUS-HR", StringComparison.Ordinal) && outcome.Ran);

        report.OfAtLeast(RuleSeverity.Error)
            .Select(message => message.RuleIdentifier)
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToArray()
            .ShouldBe(["HR-BR-2", "HR-BR-37", "HR-BR-9"], customMessage: Describe(report));
    }

    /// <summary>
    /// The two Croatian demands the model <em>can</em> meet, proved by taking them away one at a time.
    /// </summary>
    [Fact]
    public void TheProcessCodeAndTheClassificationAreOursToGetRight()
    {
        EInvoice withoutProcess = AnInvoice();
        withoutProcess.BusinessProcessType = default;

        Failures(withoutProcess).ShouldContain("HR-BR-34");

        EInvoice withoutClassification = AnInvoice();
        withoutClassification.Lines[0].Item!.ClassificationCodes.Clear();

        Failures(withoutClassification).ShouldContain("HR-BR-25");

        EInvoice withAnInventedCode = AnInvoice();
        withAnInventedCode.Lines[0].Item!.ClassificationCodes[0] = new CodeField("70.22", ListId: "CG");

        // The rule carries the whole KPD list, so a plausible code that is not in it is still refused.
        Failures(withAnInventedCode).ShouldContain("HR-BR-CL-2");
    }

    [Fact]
    public void TheProcessCodeIsCheckedBeforeItIsWritten()
    {
        HrBusinessProcess.All.Count.ShouldBe(12);
        HrBusinessProcess.IsValid("P1").ShouldBeTrue();
        HrBusinessProcess.IsValid("P12").ShouldBeTrue();
        HrBusinessProcess.IsValid("P13").ShouldBeFalse();
        HrBusinessProcess.IsValid("P0").ShouldBeFalse();
        HrBusinessProcess.IsValid("P99").ShouldBeFalse();
        HrBusinessProcess.IsValid(HrBusinessProcess.ForBuyer("nabava-2026")).ShouldBeTrue();
        Should.Throw<ArgumentException>(() => HrBusinessProcess.ForBuyer(" "));
    }

    private static IEnumerable<string> Failures(EInvoice invoice) =>
        Validate(invoice).OfAtLeast(RuleSeverity.Error).Select(message => message.RuleIdentifier);

    private static ValidationReport Validate(EInvoice invoice)
    {
        Assert.SkipWhen(!Directory.Exists(Artefacts), "run build/fetch-specs.sh national");

        EInvoicing library = EInvoicing.Create(croatia => croatia
            .AddDefaults()
            .AddCroatia()
            .AddCroatianRulesFrom(Artefacts));

        return library.Validate(library.Write(invoice));
    }

    private static EInvoice AnInvoice() => EInvoiceBuilder
        .Create(HrProfiles.CiusHrUbl)
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .InCurrency("EUR")
        .WithBuyerReference("REF-2026-0001")
        .From(seller => seller
            .Named("Dobavljač d.o.o.")
            .WithVatIdentifier("HR" + HrOibTests.ValidNumbers[0])
            .WithLegalRegistration(HrOibTests.ValidNumbers[0])
            .WithElectronicAddress(HrOibTests.ValidNumbers[0], "9934")
            .WithAddress(address =>
            {
                address.Line1 = "Ilica 1";
                address.City = "Zagreb";
                address.PostCode = "10000";
                address.CountryCode = "HR";
            }))
        .To(buyer => buyer
            .Named("Kupac d.o.o.")
            .WithVatIdentifier("HR" + HrOibTests.ValidNumbers[1])
            .WithElectronicAddress(HrOibTests.ValidNumbers[1], "9934")
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
            .WithVat("S", 25m)
            // HR-BR-25 and HR-BR-CL-2: a KPD (CPA) code, under list CG, out of the 3 359 the rule carries.
            .Extend(line => line.Item!.ClassificationCodes.Add(new CodeField("70.20.11", ListId: "CG"))))
        .Extend(invoice =>
        {
            invoice.BusinessProcessType = "P1";
            invoice.Payment = new PaymentInstructions
            {
                MeansTypeCode = "30",
                CreditTransfers = { new CreditTransfer { AccountIdentifier = "HR1210010051863000160" } },
            };
        })
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
