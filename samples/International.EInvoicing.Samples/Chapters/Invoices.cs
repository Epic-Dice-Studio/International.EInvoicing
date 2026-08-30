using International.EInvoicing.Building;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Samples.Chapters;

/// <summary>Building, writing, reading back and validating an invoice.</summary>
internal static class Invoices
{
    /// <summary>
    /// An invoice reads as the sentence it is: from a supplier, to a customer, these lines, that VAT.
    /// </summary>
    public static EInvoice Build(bool announce = true)
    {
        if (announce)
        {
            Report.Chapter("Building an invoice");
        }

        EInvoice invoice = EInvoiceBuilder
            .Create(KnownProfiles.En16931Ubl)
            .WithNumber("FA-2026-001")
            .IssuedOn(new DateOnly(2026, 9, 1))
            .DueOn(new DateOnly(2026, 10, 1))
            .OfType("380")                                       // BT-3: a commercial invoice
            .InCurrency("EUR")
            .WithBuyerReference("PO-4417")                        // BT-10
            .From(seller => seller                                // BG-4 — the supplier
                .Named("Epic Dice Studio")
                .WithVatIdentifier("FR32100000009")
                .WithElectronicAddress("100000009", "0009")
                .WithAddress(address =>
                {
                    address.Line1 = "1 rue de la Facture";
                    address.City = "Angers";
                    address.PostCode = "49000";
                    address.CountryCode = "FR";
                }))
            .To(buyer => buyer                                    // BG-7 — the customer
                .Named("Acme SA")
                .WithVatIdentifier("FR44200000008")
                .WithElectronicAddress("200000008", "0009")
                .WithAddress(address =>
                {
                    address.Line1 = "8 avenue des Clients";
                    address.City = "Nantes";
                    address.PostCode = "44000";
                    address.CountryCode = "FR";
                }))
            .AddLine(line => line
                .WithIdentifier("1")
                .WithItem("Conseil")
                .WithQuantity(3m, "HUR")                          // BT-129, BT-130
                .WithNetPrice(150m)                               // BT-146
                .WithNetAmount(450m)
                .WithVat("S", 20m))
            .AddLine(line => line
                .WithIdentifier("2")
                .WithItem("Documentation")
                .WithQuantity(4m, "C62")
                .WithNetPrice(50m)
                .WithNetAmount(200m)
                .WithVat("S", 5.5m))
            .WithComputedVatBreakdown()                           // BG-23, grouped from the lines
            .WithComputedTotals()                                 // BT-106 … BT-115, derived from them
            .Build();

        if (!announce)
        {
            return invoice;
        }

        Report.Fact("BT-1 number", invoice.Number.Value);
        Report.Fact("BT-106 sum of lines", invoice.Totals.LineTotalAmount.Value);
        Report.Fact("BT-110 VAT", invoice.Totals.TaxAmount.Value);
        Report.Fact("BT-112 total with VAT", invoice.Totals.TaxInclusiveAmount.Value);
        Report.Fact("BT-115 amount due", invoice.Totals.DuePayableAmount.Value);

        foreach (VatBreakdownEntry entry in invoice.VatBreakdown)
        {
            Report.Note($"{entry.Rate.Value}% on {entry.TaxableAmount.Value} → {entry.TaxAmount.Value}");
        }

        Report.Say("The totals are derived, so they cannot drift from the lines they summarise.");
        return invoice;
    }

    /// <summary>One model, two syntaxes. The profile settles which one, unless you say otherwise.</summary>
    public static string Write(EInvoicing einvoicing, EInvoice invoice)
    {
        Report.Chapter("Writing it");

        string ubl = einvoicing.Write(invoice, DocumentFormat.Ubl);
        string cii = einvoicing.Write(invoice, DocumentFormat.Cii);
        string chosen = einvoicing.Write(invoice);

        Report.Fact("as UBL", $"{ubl.Length} characters");
        Report.Fact("as CII", $"{cii.Length} characters");
        Report.Fact("syntax chosen from the profile", chosen.Length == ubl.Length ? "UBL" : "CII");
        Report.Snippet(ubl, lines: 8);

        return ubl;
    }

    /// <summary>Reading gives back the typed value and the text it came from, side by side.</summary>
    public static void ReadBack(EInvoicing einvoicing, string ubl)
    {
        Report.Chapter("Reading it back");

        DocumentResult result = einvoicing.Read(ubl);

        Report.Fact("detected as", result.Kind);
        Report.Fact("profile resolved exactly", result.Profile?.IsExact);

        if (!result.TryGetInvoice(out EInvoice? invoice))
        {
            Report.Say("Not an invoice — nothing to show.");
            return;
        }

        Report.Fact("BT-2 typed value", invoice.IssueDate.Value);
        Report.Fact("BT-2 as the file wrote it", invoice.IssueDate.Raw);
        Report.Fact("BT-2 where it was found", invoice.IssueDate.Location);
        Report.Fact("BT-112 amount", invoice.Totals.TaxInclusiveAmount.Value);
        Report.Fact("BT-112 currency attribute", invoice.Totals.TaxInclusiveAmount.CurrencyCode);
        Report.Say("Every field keeps its raw text and attributes, so re-writing it changes nothing you did not.");
    }

    /// <summary>A report says what ran as well as what failed.</summary>
    public static void Validate(EInvoicing einvoicing, string ubl)
    {
        Report.Chapter("Validating it");

        ValidationReport report = einvoicing.Validate(ubl);

        Report.Fact("valid", report.IsValid);
        Report.Fact("fully checked", report.IsComplete);
        Report.Fact("conforming", report.IsConforming);

        foreach (RuleSetOutcome outcome in report.RuleSets)
        {
            Report.Note(outcome.ToString());
        }

        foreach (ValidationMessage message in report.Errors.Take(5))
        {
            Report.Note($"{message.RuleIdentifier}: {message.Message}");
        }

        Report.Say("A document nothing checked is reported as unchecked, never as valid.");
    }
}
