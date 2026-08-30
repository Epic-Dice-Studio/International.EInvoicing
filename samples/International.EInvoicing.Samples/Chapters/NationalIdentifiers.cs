using International.EInvoicing.Countries.Belgium.Identifiers;
using International.EInvoicing.Countries.France.Identifiers;
using International.EInvoicing.Countries.Germany.Identifiers;

namespace International.EInvoicing.Samples.Chapters;

/// <summary>
/// The identifiers that decide whether an invoice reaches anyone.
/// </summary>
/// <remarks>
/// These carry check digits, so a typo can be caught before the invoice leaves rather than after it is
/// delivered to the wrong company. Pattern-matching them would not: a Leitweg-ID with one digit wrong is
/// still the right shape, and the invoice simply goes somewhere else.
/// </remarks>
internal static class NationalIdentifiers
{
    public static void Run()
    {
        Report.Chapter("National identifiers, checked rather than matched");

        France();
        Germany();
        Belgium();
    }

    private static void France()
    {
        Report.Say("France");
        Report.Fact("  SIREN 732829320", FrSiren.IsValid("732829320"));
        Report.Fact("  one digit changed", FrSiren.IsValid("732829321"));
        Report.Fact("  written on paper", FrSiren.Parse("732829320").ToFormattedString());
        Report.Fact("  SIRET 73282932000074", FrSiret.IsValid("73282932000074"));
        Report.Fact("  its establishment", FrSiret.Parse("73282932000074").EstablishmentNumber);
    }

    private static void Germany()
    {
        // The coarse address says which authority, the fine address which part of it; the check follows.
        DeLeitwegId routing = DeLeitwegId.Create("04011000", "1234512345");
        string broken = routing.ToString().Replace("04011000", "04011001", StringComparison.Ordinal);

        Report.Say("Germany");
        Report.Fact("  built with its check digits", routing.ToString());
        Report.Fact("  and it verifies", DeLeitwegId.IsValid(routing.ToString()));
        Report.Fact("  one digit changed", DeLeitwegId.IsValid(broken));
        Report.Note("This is BT-10 on a German public-sector invoice. Wrong, it is delivered elsewhere.");
    }

    private static void Belgium()
    {
        BeEnterpriseNumber company = BeEnterpriseNumber.Parse("0203.201.340");
        BeStructuredCommunication payment = BeStructuredCommunication.ForInvoice(2026_000_001);

        Report.Say("Belgium");
        Report.Fact("  enterprise number", company.Value);
        Report.Fact("  as a VAT number", company.VatNumber);
        Report.Fact("  structured communication", payment.ToString());
        Report.Note("The +++/+++ reference a Belgian bank transfer is reconciled by.");
    }
}
