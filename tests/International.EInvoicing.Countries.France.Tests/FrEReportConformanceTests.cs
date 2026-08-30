using International.EInvoicing.Countries.France.EReporting;
using International.EInvoicing.Countries.France.EReporting.Building;
using International.EInvoicing.Countries.France.EReporting.Model;
using International.EInvoicing.Countries.France.EReporting.Writing;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.France.Tests;

/// <summary>
/// Measures what the e-reporting builder produces against the DGFiP's own flux 10 rules.
/// </summary>
/// <remarks>
/// The artefacts are fetched, not redistributed — <c>build/fetch-specs.sh france</c> — so these tests skip
/// and say so when they are absent. The DGFiP publishes no sample transmissions, which is exactly why the
/// documents this library builds have to be measured against the rules rather than against an example.
/// </remarks>
public class FrEReportConformanceTests
{
    private static readonly DateOnly From = new(2026, 9, 1);
    private static readonly DateOnly To = new(2026, 9, 30);

    [Fact]
    public void ADayOfSalesSatisfiesTheFrenchRules()
    {
        FrEReport report = FrEReporting
            .Transactions(From, To)
            .From("0003", "PA-E Vendeur")
            .For("100000009", "VENDEUR")
            .Day(From, FrEReportCodes.RetailTransactions, split => split.At(20m, 1000m).At(5.5m, 200m))
            .Counting(42)
            .Build();

        ShouldBeAccepted(report, "a day of counter sales");
    }

    [Fact]
    public void EveryTransactionCategorySatisfiesTheFrenchRules()
    {
        foreach (string category in FrEReportCodes.TransactionCategories)
        {
            FrEReport report = FrEReporting
                .Transactions(From, To)
                .From("0003", "PA-E Vendeur")
                .For("100000009", "VENDEUR")
                .Day(From, category, split => split.At(20m, 1000m))
                .Build();

            ShouldBeAccepted(report, category);
        }
    }

    [Fact]
    public void AnInvoiceReportedToAPartyAbroadSatisfiesTheFrenchRules()
    {
        FrEReport report = FrEReporting
            .Transactions(From, To)
            .From("0003", "PA-E Vendeur")
            .For("100000009", "VENDEUR")
            .Invoice(invoice => invoice
                .Numbered("F202600001", new DateOnly(2026, 9, 4))
                .InProcess("B1")
                .TaxDueOn("5")
                .DueOn(new DateOnly(2026, 10, 4))
                .SoldBy("100000009", "FR32100000009")
                .BoughtAbroadBy("0223", "DE811569869", "DE", "DE811569869")
                .Taxed(20m, 1000m, 200m))
            .Build();

        ShouldBeAccepted(report, "an invoice to a buyer abroad");
    }

    [Fact]
    public void AnExemptInvoiceCarriesTheReasonTheRulesAskFor()
    {
        FrEReport report = FrEReporting
            .Transactions(From, To)
            .From("0003", "PA-E Vendeur")
            .For("100000009", "VENDEUR")
            .Invoice(invoice => invoice
                .Numbered("F202600002", new DateOnly(2026, 9, 5))
                .InProcess("B1")
                .SoldBy("100000009", "FR32100000009")
                .BoughtAbroadBy("0223", "DE811569869", "DE", "DE811569869")
                .Exempt(1000m, "VATEX-EU-IC", "Livraison intracommunautaire"))
            .Build();

        ShouldBeAccepted(report, "an exempt invoice");
    }

    [Fact]
    public void PaymentsSatisfyTheFrenchRules()
    {
        FrEReport report = FrEReporting
            .Payments(From, To)
            .From("0003", "PA-E Vendeur")
            .For("100000009", "VENDEUR")
            .ForInvoice("F202600001", new DateOnly(2026, 9, 4), new DateOnly(2026, 9, 20), split => split.At(20m, 1200m))
            .ForTransactions(new DateOnly(2026, 9, 21), split => split.At(5.5m, 211m))
            .Build();

        ShouldBeAccepted(report, "payments");
    }

    /// <summary>A transmission reports transactions or payments, never both and never neither.</summary>
    [Fact]
    public void TheRulesRejectATransmissionReportingBoth()
    {
        FrEReport report = FrEReporting
            .Transactions(From, To)
            .From("0003", "PA-E Vendeur")
            .For("100000009", "VENDEUR")
            .Day(From, FrEReportCodes.RetailTransactions, split => split.At(20m, 1000m))
            .Build();

        report.Payments = new FrPaymentsReport { Period = report.Transactions!.Period };

        Validate(report).IsValid.ShouldBeFalse();
    }

    /// <summary>A rule set that accepts everything proves nothing: a rate outside the French list must be caught.</summary>
    [Fact]
    public void TheRulesRejectARateFranceDoesNotHave()
    {
        FrEReport report = FrEReporting
            .Transactions(From, To)
            .From("0003", "PA-E Vendeur")
            .For("100000009", "VENDEUR")
            .Day(From, FrEReportCodes.RetailTransactions, split => split.At(17.5m, 1000m))
            .Build();

        Validate(report).IsValid.ShouldBeFalse();
    }

    private static void ShouldBeAccepted(FrEReport report, string what)
    {
        ValidationReport result = Validate(report);

        result.IsValid.ShouldBeTrue(
            $"{what} was rejected:{Environment.NewLine}"
            + string.Join(
                Environment.NewLine,
                result.Messages.Select(message => $"  {message.RuleIdentifier}: {message.Message}")));

        result.Messages
            .Where(message => message.Message.StartsWith("This rule could not be evaluated", StringComparison.Ordinal))
            .ShouldBeEmpty($"{what} left rules unevaluated");
    }

    private static ValidationReport Validate(FrEReport report) =>
        new SchematronValidator().Validate(new FrEReportWriter().WriteToString(report), Rules());

    private static SchematronRuleSet Rules()
    {
        string directory = Path.Combine(RepositoryRoot(), "specs", "fr-dse", "rules", "flux10");
        string? path = Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*.sch", SearchOption.AllDirectories).FirstOrDefault()
            : null;

        Assert.SkipWhen(path is null, "The French artefacts are not present; run build/fetch-specs.sh france.");

        return SchematronRuleSet.Load(File.ReadAllText(path!), "PPF Flux 10", "1.0");
    }

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
