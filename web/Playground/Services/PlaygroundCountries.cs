using International.EInvoicing.Building;
using International.EInvoicing.Countries.Belgium;
using International.EInvoicing.Countries.Belgium.Identifiers;
using International.EInvoicing.Countries.Croatia;
using International.EInvoicing.Countries.Croatia.Identifiers;
using International.EInvoicing.Countries.Denmark;
using International.EInvoicing.Countries.Denmark.Identifiers;
using International.EInvoicing.Countries.France;
using International.EInvoicing.Countries.France.Invoicing;
using International.EInvoicing.Countries.Germany;
using International.EInvoicing.Countries.Germany.Identifiers;
using International.EInvoicing.Countries.Iceland;
using International.EInvoicing.Countries.Iceland.Identifiers;
using International.EInvoicing.Countries.Netherlands;
using International.EInvoicing.Countries.Netherlands.Identifiers;
using International.EInvoicing.Countries.Norway;
using International.EInvoicing.Countries.Norway.Identifiers;
using International.EInvoicing.Countries.Sweden;
using International.EInvoicing.Countries.Sweden.Identifiers;
using International.EInvoicing.FacturX;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Playground.Services;

/// <summary>
/// Every country this library has a package for, as the playground offers them.
/// </summary>
/// <remarks>
/// The identifiers below are computed to satisfy their own check digits, not taken from a business register:
/// a valid-looking identifier that belongs to a real company has no place in a demo.
/// </remarks>
public static class PlaygroundCountries
{
    private static readonly PlaygroundRuleSet En16931 = new("EN 16931", Embedded: true);

    private static readonly PlaygroundRuleSet PeppolRules = new(
        "Peppol BIS Billing 3.0",
        Embedded: false,
        "OpenPEPPOL publishes it under no licence that allows redistribution, so it is fetched, not shipped.");

    private static readonly PlaygroundRuleSet FrenchRules = new(
        "EXTENDED CTC FR and BR-FR",
        Embedded: false,
        "The DGFiP artefacts declare no licence, so they are fetched, not shipped.");

    /// <summary>All of them, the country-neutral entry first.</summary>
    public static IReadOnlyList<PlaygroundCountry> All { get; } =
    [
        Neutral(),
        France(),
        Germany(),
        Belgium(),
        Netherlands(),
        Norway(),
        Sweden(),
        Denmark(),
        Iceland(),
        Croatia(),
    ];

    /// <summary>The country with that code, or the neutral entry.</summary>
    public static PlaygroundCountry ByCode(string? code) =>
        All.FirstOrDefault(country => country.Code == code) ?? All[0];

    private static PlaygroundCountry Neutral() => new()
    {
        Code = "--",
        Name = "No country — EN 16931 itself",
        Currency = "EUR",
        Profiles =
        [
            new("EN 16931 (UBL)", KnownProfiles.En16931Ubl),
            new("EN 16931 (CII)", KnownProfiles.En16931Cii),
            new("Factur-X BASIC", KnownProfiles.FacturXBasic),
            new("Factur-X EN 16931", FacturXProfiles.En16931),
            new("Factur-X EXTENDED", KnownProfiles.FacturXExtended),
        ],
        SellerVat = "FR32732829320",
        BuyerVat = "FR89552081317",
        RuleSets = [En16931],
    };

    private static PlaygroundCountry France() => new()
    {
        Code = "FR",
        Name = "France",
        Currency = "EUR",
        Facade = "FrenchEInvoicing",
        Profiles =
        [
            new("Extended CTC FR (UBL)", FrProfiles.ExtendedCtcFrUbl),
            new("Extended CTC FR (CII)", FrProfiles.ExtendedCtcFrCii),
            new("Factur-X EN 16931", FacturXProfiles.En16931),
            new("EN 16931 (UBL)", KnownProfiles.En16931Ubl),
        ],
        SellerIdentifier = "732829320",
        BuyerIdentifier = "552081317",
        Prepare = builder => builder.ForFrance(),
        Describe = (party, siren, name) => party
            .Named(name)
            .WithLegalRegistration(siren, "0002")
            .WithVatIdentifier(FrVat(siren)),
        CreationSnippet = "FrenchEInvoicing france = FrenchEInvoicing.Create();",
        DescribeSnippet = (siren, name) =>
            $"seller => seller.Named(\"{name}\").WithLegalRegistration(\"{siren}\", \"0002\")",
        RuleSets = [En16931, FrenchRules],
        Trap = "BT-23 carries the invoicing case, and three mentions are mandatory whatever else is right "
            + "(BR-FR-05). ForFrance() adds all four.",
        ExtraDocuments = ["A lifecycle status (CDAR)", "An e-reporting transmission (flux 10)"],
    };

    private static PlaygroundCountry Germany() => new()
    {
        Code = "DE",
        Name = "Germany",
        Currency = "EUR",
        Facade = "GermanEInvoicing",
        Profiles =
        [
            new("XRechnung 3.0 (UBL)", DeProfiles.XRechnungUbl),
            new("XRechnung 3.0 (CII)", DeProfiles.XRechnungCii),
            new("XRechnung Extension (UBL)", DeProfiles.XRechnungExtensionUbl),
            new("Factur-X / ZUGFeRD EN 16931", FacturXProfiles.En16931),
        ],
        SellerIdentifier = "DE123456789",
        BuyerIdentifier = "DE987654321",
        SellerVat = "DE123456789",
        Prepare = builder => builder.WithBuyerReference(DeLeitwegId.Create("04011000", "1234512345").ToString()),

        // BR-DE-2 makes the seller contact group mandatory, which EN 16931 leaves optional.
        Describe = (party, vat, name) => party
            .Named(name)
            .WithVatIdentifier(vat)
            .WithContact(contact =>
            {
                contact.Name = "Rechnungsstelle";
                contact.Telephone = "+49 30 123456";
                contact.Email = "rechnung@example.de";
            }),
        CreationSnippet = "GermanEInvoicing germany = GermanEInvoicing.Create();",
        DescribeSnippet = (vat, name) => $"seller => seller.Named(\"{name}\").WithVatIdentifier(\"{vat}\")",
        RuleSets = [En16931, new PlaygroundRuleSet("XRechnung 3.x", Embedded: true)],
        Trap = "A public body is reached by its Leitweg-ID in BT-10. InvoiceToPublicBody parses and checks "
            + "it before writing it, so a wrong check digit fails here rather than on arrival.",
    };

    private static PlaygroundCountry Belgium() => new()
    {
        Code = "BE",
        Name = "Belgium",
        Currency = "EUR",
        Facade = "BelgianEInvoicing",
        Profiles =
        [
            new("Peppol BIS Billing 3.0 (UBL)", BeProfiles.PeppolBillingUbl),
            new("Peppol BIS Billing 3.0 (CII)", BeProfiles.PeppolBillingCii),
        ],
        SellerIdentifier = "0776914174",
        BuyerIdentifier = "0403170701",
        Prepare = builder => builder.ForPeppol(),
        Describe = (party, number, name) => BelgianEInvoicing.Create().Describe(party, number, name),
        CreationSnippet = "BelgianEInvoicing belgium = BelgianEInvoicing.Create();",
        DescribeSnippet = (number, name) => $"seller => belgium.Describe(seller, \"{number}\", \"{name}\")",
        RuleSets = [En16931, PeppolRules],
        Trap = "The enterprise number is checked modulo 97 and written in scheme 0208, the one Peppol "
            + "reserves for it. And BT-23 must carry the Peppol business process, which EN 16931 never asks for.",
    };

    private static PlaygroundCountry Netherlands() => new()
    {
        Code = "NL",
        Name = "Netherlands",
        Currency = "EUR",
        Facade = "DutchEInvoicing",
        Profiles =
        [
            new("Peppol BIS Billing 3.0 (UBL)", NlProfiles.PeppolBillingUbl),
            new("Peppol BIS Billing 3.0 (CII)", NlProfiles.PeppolBillingCii),
        ],
        SellerIdentifier = "12345678",
        BuyerIdentifier = "87654321",
        SellerVat = "NL123456789B01",
        BuyerVat = "NL987654321B01",
        Prepare = builder => builder.ForPeppol(),
        Describe = (party, kvk, name) => DutchEInvoicing.Create().Describe(party, kvk, name),
        CreationSnippet = "DutchEInvoicing nederland = DutchEInvoicing.Create();",
        DescribeSnippet = (kvk, name) => $"seller => nederland.Describe(seller, \"{kvk}\", \"{name}\")",
        RuleSets = [En16931, PeppolRules],
        Trap = "NL-R-003 and NL-R-005 are fatal: both parties' legal entity identifiers must carry scheme "
            + "0106 (KvK) or 0190 (OIN). An invoice naming both companies perfectly and omitting the scheme "
            + "is refused.",
    };

    private static PlaygroundCountry Norway() => new()
    {
        Code = "NO",
        Name = "Norway",
        Currency = "NOK",
        Facade = "NorwegianEInvoicing",
        Profiles =
        [
            new("EHF 3.0 (UBL)", NoProfiles.Ehf3Ubl),
            new("EHF 3.0 (CII)", NoProfiles.Ehf3Cii),
            new("Peppol BIS Billing 3.0 (UBL)", NoProfiles.PeppolBillingUbl),
        ],
        SellerIdentifier = Valid(NoOrganisationNumber.IsValid, 910_000_000, 999_999_999),
        BuyerIdentifier = Valid(NoOrganisationNumber.IsValid, 920_000_000, 999_999_999),
        Prepare = builder => builder.ForPeppol(),
        Describe = (party, number, name) => NorwegianEInvoicing.Create().Describe(party, number, name),
        CreationSnippet = "NorwegianEInvoicing norge = NorwegianEInvoicing.Create();",
        DescribeSnippet = (number, name) => $"seller => norge.Describe(seller, \"{number}\", \"{name}\")",
        RuleSets = [En16931, PeppolRules],
        Trap = "EHF 3.0 is a CIUS of Peppol BIS, which is a CIUS of EN 16931 — so BT-24 names all three. "
            + "The organisation number is checked modulo 11 and written in scheme 0192.",
    };

    private static PlaygroundCountry Sweden() => new()
    {
        Code = "SE",
        Name = "Sweden",
        Currency = "SEK",
        Facade = "SwedishEInvoicing",
        Profiles =
        [
            new("Peppol BIS Billing 3.0 (UBL)", SeProfiles.PeppolBillingUbl),
            new("Peppol BIS Billing 3.0 (CII)", SeProfiles.PeppolBillingCii),
        ],
        SellerIdentifier = Valid(SeOrganisationNumber.IsValid, 5_560_000_000, 5_569_999_999),
        BuyerIdentifier = Valid(SeOrganisationNumber.IsValid, 5_565_000_000, 5_569_999_999),
        Prepare = builder => builder.ForPeppol(),
        Describe = (party, number, name) => SwedishEInvoicing.Create().Describe(party, number, name),
        CreationSnippet = "SwedishEInvoicing sverige = SwedishEInvoicing.Create();",
        DescribeSnippet = (number, name) => $"seller => sverige.Describe(seller, \"{number}\", \"{name}\")",
        RuleSets = [En16931, PeppolRules],
        Trap = "The organisation number's last digit is a Luhn check, and Peppol enforces it on scheme 0007.",
    };

    private static PlaygroundCountry Denmark() => new()
    {
        Code = "DK",
        Name = "Denmark",
        Currency = "DKK",
        Facade = "DanishEInvoicing",
        Profiles =
        [
            new("Peppol BIS Billing 3.0 (UBL)", DkProfiles.PeppolBillingUbl),
            new("Peppol BIS Billing 3.0 (CII)", DkProfiles.PeppolBillingCii),
        ],
        SellerIdentifier = "12345670",
        BuyerIdentifier = "25313763",
        Prepare = builder => builder.ForPeppol(),
        Describe = (party, number, name) => DanishEInvoicing.Create().Describe(party, number, name),
        CreationSnippet = "DanishEInvoicing danmark = DanishEInvoicing.Create();",
        DescribeSnippet = (number, name) => $"seller => danmark.Describe(seller, \"{number}\", \"{name}\")",
        RuleSets = [En16931, PeppolRules],
        Trap = "Payment means code 30 — plain credit transfer — is valid EN 16931 and refused between two "
            + "Danish parties by DK-R-005. DkPaymentMeans carries the codes Denmark does accept.",
    };

    private static PlaygroundCountry Iceland() => new()
    {
        Code = "IS",
        Name = "Iceland",
        Currency = "ISK",
        Facade = "IcelandicEInvoicing",
        Profiles =
        [
            new("Peppol BIS Billing 3.0 (UBL)", IsProfiles.PeppolBillingUbl),
            new("Peppol BIS Billing 3.0 (CII)", IsProfiles.PeppolBillingCii),
        ],
        SellerIdentifier = ValidKennitala(12_000_000),
        BuyerIdentifier = ValidKennitala(12_011_100),
        SellerVat = "IS12345",
        BuyerVat = "IS54321",
        Prepare = builder => builder.ForPeppol(),
        Describe = (party, number, name) => IcelandicEInvoicing.Create().Describe(party, number, name),
        CreationSnippet = "IcelandicEInvoicing island = IcelandicEInvoicing.Create();",
        DescribeSnippet = (number, name) => $"seller => island.Describe(seller, \"{number}\", \"{name}\")",
        RuleSets = [En16931, PeppolRules],
        Trap = "IS-R-002 and IS-R-004 are fatal: both parties need a legal entity identifier carrying "
            + "scheme 0196, the kennitala.",
    };

    private static PlaygroundCountry Croatia() => new()
    {
        Code = "HR",
        Name = "Croatia",
        Currency = "EUR",
        Facade = "CroatianEInvoicing",
        Profiles =
        [
            new("Peppol BIS Billing 3.0 (UBL)", HrProfiles.PeppolBillingUbl),
            new("Peppol BIS Billing 3.0 (CII)", HrProfiles.PeppolBillingCii),
        ],
        SellerIdentifier = "69435151530",
        BuyerIdentifier = ValidOib(12_345_678_90L),
        Prepare = builder => builder.ForPeppol(),
        Describe = (party, oib, name) => CroatianEInvoicing.Create().Describe(party, oib, name),
        CreationSnippet = "CroatianEInvoicing hrvatska = CroatianEInvoicing.Create();",
        DescribeSnippet = (oib, name) => $"seller => hrvatska.Describe(seller, \"{oib}\", \"{name}\")",
        RuleSets =
        [
            En16931,
            PeppolRules,
            new PlaygroundRuleSet(
                "HR-FISK 2.0",
                Embedded: false,
                "Croatia publishes its CIUS identifier and Schematron where this repository cannot read "
                    + "them, so neither is asserted here."),
        ],
        Trap = "Fiskalizacija 2.0 has been live since 1 January 2026 and wants three things per invoice. "
            + "This is one of them: the OIB of both parties. The other two — an advanced electronic seal, "
            + "and a fiscalisation report from each party — are a signature and a transport, neither of "
            + "which this library does.",
    };

    /// <summary>An OIB whose eleventh digit closes ISO/IEC 7064 MOD 11,10 over the first ten.</summary>
    private static string ValidOib(long tenDigits)
    {
        string body = tenDigits.ToString("D10", System.Globalization.CultureInfo.InvariantCulture);
        int remainder = 10;

        foreach (char digit in body)
        {
            remainder = (remainder + (digit - '0')) % 10;
            remainder = (remainder == 0 ? 10 : remainder) * 2 % 11;
        }

        return body + ((11 - remainder) % 10).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>The first number in the range that satisfies the country's own check.</summary>
    private static string Valid(Func<string, bool> isValid, long from, long to)
    {
        for (long candidate = from; candidate <= to; candidate++)
        {
            string text = candidate.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (isValid(text))
            {
                return text;
            }
        }

        return string.Empty;
    }

    private static string ValidKennitala(int from)
    {
        for (int body = from; body < from + 1000; body++)
        {
            string text = body.ToString("D8", System.Globalization.CultureInfo.InvariantCulture);

            for (int check = 0; check <= 9; check++)
            {
                string candidate = text + check + "0";

                if (IsKennitala.IsValid(candidate))
                {
                    return candidate;
                }
            }
        }

        return string.Empty;
    }

    /// <summary>The French VAT number a SIREN implies: a two-digit key, then the SIREN.</summary>
    private static string FrVat(string siren) =>
        long.TryParse(siren, out long value)
            ? "FR" + ((12 + (3 * (value % 97))) % 97).ToString("D2", System.Globalization.CultureInfo.InvariantCulture) + siren
            : string.Empty;
}
