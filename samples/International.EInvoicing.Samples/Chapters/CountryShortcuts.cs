using International.EInvoicing.Building;
using International.EInvoicing.Countries.Belgium;
using International.EInvoicing.Countries.France;
using International.EInvoicing.Countries.France.EReporting;
using International.EInvoicing.Countries.France.EReporting.Model;
using International.EInvoicing.Countries.Germany;
using International.EInvoicing.Model;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Samples.Chapters;

/// <summary>
/// One country, one object — for the reader who invoices in exactly one place and would rather not learn
/// which profile, which business process and which rule set that place wants.
/// </summary>
/// <remarks>
/// Everything here is doable with the library underneath, and the chapter before this one does it that way.
/// The difference is how much you have to know first.
/// </remarks>
internal static class CountryShortcuts
{
    public static void Run()
    {
        Report.Chapter("One country, one object");

        France();
        Germany();
        Belgium();

        Report.Note("Each shortcut exposes .Library, so nothing underneath becomes unreachable.");
    }

    /// <summary>France exchanges four documents; this reads all four without being told which arrived.</summary>
    private static void France()
    {
        FrenchEInvoicing france = FrenchEInvoicing.Create();

        EInvoice invoice = france.Invoice()
            .WithNumber("F202600001")
            .IssuedOn(new DateOnly(2026, 9, 4))
            .From("Fournisseur SARL", "FR32732829320")
            .To("Client SA", "FR89552081317")
            .AddLine(line => line.WithItem("Conseil").WithNetAmount(1000m).WithVat("S", 20m))
            .WithComputedVatBreakdown()
            .WithComputedTotals()
            .Build();

        LifecycleStatusMessage status = france
            .StatusFromBuyer("200000008", "ACHETEUR")
            .SentBy("0003", "PA-E Acheteur")
            .ToSeller("100000009", "VENDEUR", "100000009_STATUTS")
            .About("F202600001", new DateOnly(2026, 9, 4))
            .Approved(new DateTimeOffset(2026, 9, 8, 10, 0, 0, TimeSpan.Zero));

        FrEReport report = france
            .ReportTransactions(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30))
            .From("0003", "PA-E Vendeur")
            .For("100000009", "VENDEUR")
            .Day(new DateOnly(2026, 9, 4), FrEReportCodes.RetailTransactions, day => day.At(20m, 1000m, 200m))
            .Build();

        Report.Say("France — the same object writes and recognises all four documents");
        Report.Fact("  invoice", france.Read(france.Write(invoice)).Kind);
        Report.Fact("  credit note", france.Read(france.Write(france.CreditNote().Build())).Kind);
        Report.Fact("  lifecycle status", france.Read(france.Write(status)).Kind);
        Report.Fact("  e-report (flux 10)", france.Read(france.Write(report)).Kind);
        Report.Note("Flux 10 carries no XML namespace: nothing but its root name says what it is.");
    }

    /// <summary>Germany: XRechnung declared, its rules registered, the Leitweg-ID checked before sending.</summary>
    private static void Germany()
    {
        GermanEInvoicing germany = GermanEInvoicing.Create();

        EInvoice invoice = germany.InvoiceToPublicBody("04011000-1234512345-06")
            .WithNumber("RE-2026-001")
            .IssuedOn(new DateOnly(2026, 9, 1))
            .Build();

        Report.Say("Germany — the routing identifier is checked here, not by the receiving desk");
        Report.Fact("  profile", invoice.SpecificationIdentifier.Value);
        Report.Fact("  buyer reference (BT-10)", invoice.BuyerReference.Value);
        Report.Fact("  a wrong check digit is refused", Refused(() => germany.InvoiceToPublicBody("04011000-1234512345-07")));
        Report.Fact("  rules run by Validate", string.Join(", ", germany.Validate(germany.Write(invoice)).RuleSets.Select(set => set.Name)));
    }

    /// <summary>Belgium: Peppol BIS, the business process the network requires, the enterprise number.</summary>
    private static void Belgium()
    {
        BelgianEInvoicing belgium = BelgianEInvoicing.Create();

        EInvoice invoice = belgium.Invoice()
            .WithNumber("2026-0001")
            .IssuedOn(new DateOnly(2026, 9, 1))
            .From(seller => belgium.Describe(seller, "0776.914.174", "Epic Dice Studio BV"))
            .To("Klant NV", "BE0403170701")
            .Build();

        Report.Say("Belgium — Peppol BIS, with what Peppol asks for and EN 16931 does not");
        Report.Fact("  profile", invoice.SpecificationIdentifier.Value);
        Report.Fact("  business process (BT-23)", invoice.BusinessProcessType.Value);
        Report.Fact("  seller endpoint", $"{invoice.Seller!.ElectronicAddress.Value} (scheme {invoice.Seller.ElectronicAddress.SchemeId})");
        Report.Fact("  a wrong enterprise number is refused", Refused(() => belgium.Invoice().From(seller => belgium.Describe(seller, "0776914151", "Fout BV"))));
        Report.Fact("  structured communication", belgium.StructuredCommunication(123456789));
    }

    private static bool Refused(Action attempt)
    {
        try
        {
            attempt();
            return false;
        }
        catch (FormatException)
        {
            return true;
        }
    }
}
