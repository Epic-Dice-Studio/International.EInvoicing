using System.Diagnostics.CodeAnalysis;
using International.EInvoicing.Building;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Testing;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.En16931.Tests;

/// <summary>
/// The totals rules, judged by the official artefact over generated invoices.
/// </summary>
/// <remarks>
/// <para>
/// <c>BR-CO-10</c> through <c>BR-CO-17</c> and the per-category <c>BR-S-08</c>/<c>BR-S-09</c> compare totals
/// to sums of lines, and they are where implementations break. Not because the arithmetic is hard, but
/// because rounding is: two decimals per line and two decimals per category do not commute, and a hand-written
/// test with round numbers never notices.
/// </para>
/// <para>
/// So the cases are generated. The seed is fixed, so a failure is reproducible and the suite is
/// deterministic; it is printed in every failure message so a case found here can be pinned as its own test.
/// No property-based framework is pulled in for this — the generator is twenty lines and shrinking would not
/// help much when every failing case is already one invoice you can print.
/// </para>
/// </remarks>
[SuppressMessage(
    "Security",
    "CA5394:Do not use insecure randomness",
    Justification = "A seeded generator is the point: a deterministic suite, and a reproducible failure.")]
public class RoundingPropertyTests
{
    private const int Seed = 20260901;
    private const int Cases = 300;

    [Theory]
    [InlineData(DocumentFormat.Ubl)]
    [InlineData(DocumentFormat.Cii)]
    public void ComputedTotalsSatisfyTheOfficialRulesOverGeneratedInvoices(DocumentFormat format)
    {
        EInvoicing library = Library();
        var random = new Random(Seed);
        List<string> failures = [];

        for (int index = 0; index < Cases; index++)
        {
            int caseSeed = random.Next();
            EInvoice invoice = Generate(new Random(caseSeed), format);

            ValidationReport report = library.Validate(library.Write(invoice, format));

            if (!report.IsConforming)
            {
                failures.Add($"case seed {caseSeed}:{Environment.NewLine}{report}");
            }
        }

        failures.ShouldBeEmpty(
            $"{failures.Count}/{Cases} generated invoices did not conform:{Environment.NewLine}"
            + string.Join(Environment.NewLine, failures.Take(3)));
    }

    /// <summary>
    /// The amounts come back exactly, in both syntaxes.
    /// </summary>
    /// <remarks>
    /// A total that validates and then reads back as a different number is the worse failure of the two: the
    /// document is accepted and the books do not match.
    /// </remarks>
    [Theory]
    [InlineData(DocumentFormat.Ubl)]
    [InlineData(DocumentFormat.Cii)]
    public void AndSurviveBeingWrittenAndReadBack(DocumentFormat format)
    {
        EInvoicing library = Library();
        var random = new Random(Seed);

        for (int index = 0; index < Cases; index++)
        {
            int caseSeed = random.Next();
            EInvoice invoice = Generate(new Random(caseSeed), format);

            EInvoice again = library.Read(library.Write(invoice, format)).RequireInvoice();

            again.Totals.LineTotalAmount.Value.ShouldBe(invoice.Totals.LineTotalAmount.Value, $"seed {caseSeed}");
            again.Totals.TaxExclusiveAmount.Value.ShouldBe(invoice.Totals.TaxExclusiveAmount.Value, $"seed {caseSeed}");
            again.Totals.TaxAmount.Value.ShouldBe(invoice.Totals.TaxAmount.Value, $"seed {caseSeed}");
            again.Totals.TaxInclusiveAmount.Value.ShouldBe(invoice.Totals.TaxInclusiveAmount.Value, $"seed {caseSeed}");
            again.Totals.DuePayableAmount.Value.ShouldBe(invoice.Totals.DuePayableAmount.Value, $"seed {caseSeed}");
            again.VatBreakdown.Count.ShouldBe(invoice.VatBreakdown.Count, $"seed {caseSeed}");
        }
    }

    /// <summary>
    /// The identity every one of the BR-CO totals rules is a restatement of.
    /// </summary>
    /// <remarks>
    /// Checked against the model rather than through the artefact, so a failure points at the arithmetic
    /// rather than at an XPath.
    /// </remarks>
    [Fact]
    public void TheTotalsAgreeWithTheLinesTheyClaimToSummarise()
    {
        var random = new Random(Seed);

        for (int index = 0; index < Cases; index++)
        {
            int caseSeed = random.Next();
            EInvoice invoice = Generate(new Random(caseSeed), DocumentFormat.Ubl);
            DocumentTotals totals = invoice.Totals;

            decimal lines = invoice.Lines.Sum(line => line.NetAmount.Value ?? 0m);
            decimal allowances = totals.AllowanceTotalAmount.Value ?? 0m;
            decimal charges = totals.ChargeTotalAmount.Value ?? 0m;
            decimal tax = invoice.VatBreakdown.Sum(entry => entry.TaxAmount.Value ?? 0m);

            totals.LineTotalAmount.Value.ShouldBe(lines, $"BR-CO-10, seed {caseSeed}");
            totals.TaxExclusiveAmount.Value.ShouldBe(lines - allowances + charges, $"BR-CO-13, seed {caseSeed}");
            totals.TaxAmount.Value.ShouldBe(tax, $"BR-CO-14, seed {caseSeed}");
            totals.TaxInclusiveAmount.Value.ShouldBe(
                (totals.TaxExclusiveAmount.Value ?? 0m) + tax,
                $"BR-CO-15, seed {caseSeed}");
        }
    }

    /// <summary>
    /// Builds one invoice out of the generator, with the arithmetic left to the library.
    /// </summary>
    /// <remarks>
    /// The quantities and prices are chosen to land <em>off</em> two decimals as often as on them — three
    /// decimal places on a quantity, four on a price, and a base quantity that is not one — because a case
    /// where the multiplication comes out exact proves nothing about rounding.
    /// </remarks>
    private static EInvoice Generate(Random random, DocumentFormat format)
    {
        EInvoiceBuilder builder = EInvoiceBuilder
            .Create(format == DocumentFormat.Cii ? KnownProfiles.En16931Cii.Id : KnownProfiles.En16931Ubl.Id)
            .WithNumber($"GEN-{random.Next(1, 99999)}")
            .OfType(InvoiceTypeCodes.CommercialInvoice)
            .IssuedOn(new DateOnly(2026, 9, 1))
            .DueOn(new DateOnly(2026, 10, 1))
            .InCurrency("EUR")
            .WithBuyerReference("GENERATED")
            .From(seller => seller
                .Named("Seller Ltd")
                .WithVatIdentifier("FR32732829320")
                .WithAddress(address =>
                {
                    address.Line1 = "12 rue de la Paix";
                    address.City = "Paris";
                    address.PostCode = "75002";
                    address.CountryCode = "FR";
                }))
            .To(buyer => buyer
                .Named("Buyer SA")
                .WithVatIdentifier("FR89552081317")
                .WithAddress(address =>
                {
                    address.Line1 = "3 avenue des Champs";
                    address.City = "Lyon";
                    address.PostCode = "69002";
                    address.CountryCode = "FR";
                }));

        int lineCount = random.Next(1, 9);
        decimal[] rates = [0m, 2.1m, 5.5m, 7m, 10m, 19m, 20m, 21m, 25.5m];
        List<decimal> used = [];

        for (int line = 1; line <= lineCount; line++)
        {
            decimal rate = rates[random.Next(rates.Length)];
            decimal quantity = Round(random.Next(1, 100_000) / 1000m, 3);
            decimal price = Round(random.Next(1, 1_000_000) / 10_000m, 4);
            decimal baseQuantity = random.Next(4) switch { 0 => 1m, 1 => 10m, 2 => 100m, _ => 1000m };
            decimal net = Round(quantity * price / baseQuantity, 2);

            used.Add(rate);
            builder.AddLine(item => item
                .WithIdentifier(line.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .WithItem($"Item {line}")
                .WithQuantity(quantity, "C62")
                .WithNetPrice(price, baseQuantity)
                .WithNetAmount(net)
                .WithVat(rate == 0m ? VatCategoryCodes.ZeroRated : VatCategoryCodes.Standard, rate));
        }

        // A document-level allowance or charge, on a rate one of the lines already uses: EN 16931 requires it
        // to reduce the base it was taken from, and getting that wrong is what BR-S-08 catches.
        if (random.Next(3) == 0)
        {
            decimal rate = used[random.Next(used.Count)];
            bool isCharge = random.Next(2) == 0;

            builder.Extend(invoice => invoice.AllowancesAndCharges.Add(new AllowanceCharge
            {
                IsCharge = isCharge,
                Amount = Round(random.Next(1, 20_000) / 100m, 2),
                Reason = isCharge ? "Handling" : "Volume discount",
                VatCategoryCode = rate == 0m ? VatCategoryCodes.ZeroRated : VatCategoryCodes.Standard,
                VatRate = rate,
            }));
        }

        return builder.WithComputedVatBreakdown().WithComputedTotals().Build();
    }

    private static decimal Round(decimal value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);

    private static EInvoicing Library() =>
        EInvoicing.Create(einvoicing => einvoicing.AddDefaults());
}
