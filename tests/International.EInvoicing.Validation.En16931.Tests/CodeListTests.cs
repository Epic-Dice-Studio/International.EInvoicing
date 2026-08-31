using System.Text.RegularExpressions;
using International.EInvoicing.Model;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.En16931.Tests;

/// <summary>
/// The code lists the model offers, held to the artefact they came from.
/// </summary>
/// <remarks>
/// <para>
/// A library that can only punish a wrong code is half a library: the caller has to find the right one
/// somewhere else. So the codes are in the model — and, like the Peppol scheme list before them, they are
/// taken from the shipped artefact rather than transcribed from the standard, because which subset of
/// UNTDID a profile allows is a question only the artefact answers.
/// </para>
/// <para>
/// This test is what makes that true rather than aspirational. It found the credit-note list five codes
/// short: 420, 458, 502, 503 and 532 were missing, so a credit note carrying one of them was read as an
/// invoice.
/// </para>
/// </remarks>
public partial class CodeListTests
{
    [Fact]
    public void TheInvoiceAndCreditNoteTypeCodesAreTheOnesTheRuleTests()
    {
        (string[] invoices, string[] creditNotes) = TypeCodesInArtefact();

        InvoiceTypeCodes.ForInvoices.ShouldBe(invoices, ignoreOrder: false);
        InvoiceTypeCodes.ForCreditNotes.ShouldBe(creditNotes, ignoreOrder: false);
    }

    [Fact]
    public void TheVatCategoryCodesAreTheOnesTheRuleTests() =>
        VatCategoryCodes.All.ShouldBe(ListBefore("BR-CL-17"), ignoreOrder: false);

    [Fact]
    public void ThePaymentMeansCodesAreTheOnesTheRuleTests() =>
        PaymentMeansCodes.All.ShouldBe(ListBefore("BR-CL-16"), ignoreOrder: false);

    [Fact]
    public void TheCurrencyCodesAreTheOnesTheRuleTests() =>
        CurrencyCodes.All.ShouldBe(ListBefore("BR-CL-04"), ignoreOrder: false);

    [Fact]
    public void TheCountryCodesAreTheOnesTheRuleTests() =>
        CountryCodes.All.ShouldBe(ListBefore("BR-CL-14"), ignoreOrder: false);

    [Fact]
    public void TheIcdSchemeCodesAreTheOnesTheRuleTests() =>
        IcdSchemeCodes.All.ShouldBe(ListBefore("BR-CL-21"), ignoreOrder: false);

    /// <summary>The same list judges four different identifiers, and drifting between them would be a defect.</summary>
    [Theory]
    [InlineData("BR-CL-10")]
    [InlineData("BR-CL-11")]
    [InlineData("BR-CL-26")]
    public void AndTheSameOnesTheOtherIdentifierRulesTest(string ruleIdentifier) =>
        IcdSchemeCodes.All.ShouldBe(ListBefore(ruleIdentifier), ignoreOrder: false);

    [Fact]
    public void TheItemClassificationSchemeCodesAreTheOnesTheRuleTests() =>
        ItemClassificationSchemeCodes.All.ShouldBe(ListBefore("BR-CL-13"), ignoreOrder: false);

    [Fact]
    public void TheAllowanceAndChargeReasonCodesAreTheOnesTheRulesTest()
    {
        AllowanceReasonCodes.All.ShouldBe(ListBefore("BR-CL-19"), ignoreOrder: false);
        ChargeReasonCodes.All.ShouldBe(ListBefore("BR-CL-20"), ignoreOrder: false);
    }

    [Fact]
    public void TheVatExemptionReasonCodesAreTheOnesTheRuleTests() =>
        VatExemptionReasonCodes.All.ShouldBe(ListBefore("BR-CL-22"), ignoreOrder: false);

    /// <summary>
    /// The two additions to ISO 3166-1 that a transcribed list would have missed.
    /// </summary>
    /// <remarks>
    /// <c>XI</c> is Northern Ireland, which the Windsor Framework requires on invoices for goods, and
    /// <c>1A</c> is Kosovo. Neither is in ISO 3166-1, and both are in the list you are judged against.
    /// </remarks>
    [Fact]
    public void TheCountryListCarriesWhatIsoDoesNot()
    {
        CountryCodes.IsKnown("XI").ShouldBeTrue();
        CountryCodes.IsKnown("1A").ShouldBeTrue();
        CountryCodes.IsKnown("UK").ShouldBeFalse("the United Kingdom is GB");
    }

    /// <summary>
    /// The ICD list and the Peppol electronic-address list look alike and are not the same.
    /// </summary>
    /// <remarks>
    /// Both are four-digit scheme identifiers, both give <c>0088</c> to a GLN, and picking from the wrong one
    /// is a mistake nothing in the type system catches. The <c>99xx</c> block is the giveaway: those are
    /// Peppol's own additions for electronic addresses and belong to no ICD.
    /// </remarks>
    [Fact]
    public void TheIcdListIsNotTheElectronicAddressList()
    {
        IcdSchemeCodes.IsKnown("0088").ShouldBeTrue();
        IcdSchemeCodes.IsKnown("9925").ShouldBeFalse("9925 is a Peppol endpoint scheme, not an ICD");
        IcdSchemeCodes.IsKnown("9930").ShouldBeFalse();
    }

    /// <summary>A shared code answers to both, and the reading is deliberate.</summary>
    [Fact]
    public void ACodeInBothListsIsBothAndSaysSo()
    {
        InvoiceTypeCodes.IsCreditNote("81").ShouldBeTrue();
        InvoiceTypeCodes.IsInvoice("81").ShouldBeTrue();
        InvoiceTypeCodes.IsCreditNote("380").ShouldBeFalse();
        InvoiceTypeCodes.IsKnown("999").ShouldBeFalse();
    }

    /// <summary>The categories that charge no VAT are the ones that must explain why.</summary>
    [Fact]
    public void TheCategoriesThatChargeNoVatNeedAReason()
    {
        VatCategoryCodes.NeedsExemptionReason(VatCategoryCodes.Standard).ShouldBeFalse();
        VatCategoryCodes.NeedsExemptionReason(VatCategoryCodes.ZeroRated).ShouldBeFalse();

        foreach (string code in new[] { "E", "AE", "G", "K", "O" })
        {
            VatCategoryCodes.NeedsExemptionReason(code).ShouldBeTrue(code);
        }
    }

    private static (string[] Invoices, string[] CreditNotes) TypeCodesInArtefact()
    {
        string[] lists = ListsIn(Assertion("BR-CL-01"));

        lists.Length.ShouldBe(2, "BR-CL-01 tests one list per document kind");

        return (lists[0].Split(' ', StringSplitOptions.RemoveEmptyEntries),
            lists[1].Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string[] ListBefore(string ruleIdentifier) =>
        ListsIn(Assertion(ruleIdentifier))[^1].Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>The assertion that carries a rule, as one line of whitespace-collapsed text.</summary>
    private static string Assertion(string ruleIdentifier)
    {
        string codes = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "specs", "en16931", "ubl", "schematron", "codelist",
            "EN16931-UBL-codes.sch"));

        int rule = codes.IndexOf($"id=\"{ruleIdentifier}\"", StringComparison.Ordinal);
        rule.ShouldBeGreaterThan(0, ruleIdentifier);

        int opened = codes.LastIndexOf("<assert", rule, StringComparison.Ordinal);

        return Whitespace().Replace(codes[opened..rule], " ");
    }

    /// <summary>The space-delimited lists an assertion tests membership of.</summary>
    private static string[] ListsIn(string assertion) =>
        [.. CodeList().Matches(assertion).Select(match => match.Groups[1].Value.Trim())];

    [GeneratedRegex(@"contains\(\s*'\s([^']{6,3000}?)\s'")]
    private static partial Regex CodeList();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

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
