using System.Collections.Frozen;

namespace International.EInvoicing.Model;

/// <summary>
/// The currency codes (BT-5, BT-6) EN 16931 accepts: ISO 4217 alpha-3, as the artefact lists them.
/// </summary>
/// <remarks>
/// Not quite ISO 4217 as published — it carries the funds codes and the metals, and it lags a rename by a
/// release or two. Which is exactly why it is taken from the artefact rather than transcribed: the list that
/// matters is the one <c>BR-CL-03</c>, <c>BR-CL-04</c> and <c>BR-CL-05</c> will judge you against, not the
/// one the standards body publishes.
/// </remarks>
public static class CurrencyCodes
{
    private static readonly string[] Codes =
    [
        "AED", "AFN", "ALL", "AMD", "AOA", "ARS", "AUD", "AWG", "AZN", "BAM", "BBD", "BDT",
        "BHD", "BIF", "BMD", "BND", "BOB", "BOV", "BRL", "BSD", "BTN", "BWP", "BYN", "BZD",
        "CAD", "CDF", "CHE", "CHF", "CHW", "CLF", "CLP", "CNH", "CNY", "COP", "COU", "CRC",
        "CUP", "CVE", "CZK", "DJF", "DKK", "DOP", "DZD", "EGP", "ERN", "ETB", "EUR", "FJD",
        "FKP", "GBP", "GEL", "GHS", "GIP", "GMD", "GNF", "GTQ", "GYD", "HKD", "HNL", "HTG",
        "HUF", "IDR", "ILS", "INR", "IQD", "IRR", "ISK", "JMD", "JOD", "JPY", "KES", "KGS",
        "KHR", "KMF", "KPW", "KRW", "KWD", "KYD", "KZT", "LAK", "LBP", "LKR", "LRD", "LSL",
        "LYD", "MAD", "MDL", "MGA", "MKD", "MMK", "MNT", "MOP", "MRU", "MUR", "MVR", "MWK",
        "MXN", "MXV", "MYR", "MZN", "NAD", "NGN", "NIO", "NOK", "NPR", "NZD", "OMR", "PAB",
        "PEN", "PGK", "PHP", "PKR", "PLN", "PYG", "QAR", "RON", "RSD", "RUB", "RWF", "SAR",
        "SBD", "SCR", "SDG", "SEK", "SGD", "SHP", "SLE", "SOS", "SRD", "SSP", "STD", "SVC",
        "SYP", "SZL", "THB", "TJS", "TMT", "TND", "TOP", "TRY", "TTD", "TWD", "TZS", "UAH",
        "UGX", "USD", "USN", "UYI", "UYU", "UYW", "UZS", "VES", "VED", "VND", "VUV", "WST",
        "XAF", "XAG", "XAU", "XBA", "XBB", "XBC", "XBD", "XCD", "XCG", "XDR", "XOF", "XPD",
        "XPF", "XPT", "XSU", "XTS", "XUA", "XXX", "YER", "ZAR", "ZMW", "ZWG",
    ];

    private static readonly FrozenSet<string> Known = Codes.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Every code, in the order the artefact lists them.</summary>
    public static IReadOnlyList<string> All => Codes;

    /// <summary>Whether a currency code is one EN 16931 accepts.</summary>
    public static bool IsKnown(string? code) => code is not null && Known.Contains(code);
}
