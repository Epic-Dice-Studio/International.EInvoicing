using International.EInvoicing.Profiles;

namespace International.EInvoicing.Peppol;

/// <summary>
/// The Peppol International (PINT) billing profiles.
/// </summary>
/// <remarks>
/// <para>
/// PINT is the other half of Peppol, and the half this library was missing. BIS Billing 3.0 is a strict CIUS
/// of EN 16931 and was built for Europe; every jurisdiction that adopted Peppol outside it — the United Arab
/// Emirates, Malaysia, Singapore, Japan, Australia and New Zealand, Oman — runs on PINT instead, which has a
/// common core and one <em>specialisation</em> per jurisdiction. A caller who assumed
/// <see cref="PeppolProfiles"/> covered those countries was writing the wrong profile identifier.
/// </para>
/// <para>
/// The identifier carries the specialisation after an <c>@</c>: <c>urn:peppol:pint:billing-1@jp-1</c> is the
/// Japanese one. They are read from the artefacts OpenPEPPOL publishes rather than transcribed from prose,
/// and <c>PeppolPintProfilesTests</c> compares each one against the artefact it came from.
/// </para>
/// <para>
/// <b>The rules are not here.</b> PINT's validation artefacts are published as pre-compiled XSLT rather than
/// as source Schematron, and this library's engine executes Schematron. Fetch them with
/// <c>build/fetch-specs.sh pint</c> to have them on disk; running them needs an XSLT processor, which is an
/// open item in the roadmap. Until then a PINT document is read and mapped, and its jurisdiction rules are
/// reported as not run rather than silently skipped.
/// </para>
/// </remarks>
public static class PeppolPintProfiles
{
    private const string BillingPrefix = "urn:peppol:pint:billing-1";
    private const string SelfBillingPrefix = "urn:peppol:pint:selfbilling-1";

    /// <summary>PINT billing without a jurisdiction specialisation — the common core.</summary>
    public static Profile Billing { get; } = Of(BillingPrefix, "Peppol PINT Billing");

    /// <summary>The European specialisation, published beside BIS Billing rather than replacing it.</summary>
    public static Profile BillingEu { get; } = Of($"{BillingPrefix}@eu-1", "Peppol PINT Billing (EU)");

    /// <summary>The United Arab Emirates specialisation, which the 2026 mandate is built on.</summary>
    public static Profile BillingAe { get; } = Of($"{BillingPrefix}@ae-1", "Peppol PINT Billing (UAE)");

    /// <summary>The UAE self-billing specialisation.</summary>
    public static Profile SelfBillingAe { get; } =
        Of($"{SelfBillingPrefix}@ae-1", "Peppol PINT Self-Billing (UAE)");

    /// <summary>The Australian and New Zealand specialisation.</summary>
    public static Profile BillingAuNz { get; } = Of($"{BillingPrefix}@aunz-1", "Peppol PINT Billing (A-NZ)");

    /// <summary>The Japanese specialisation.</summary>
    public static Profile BillingJp { get; } = Of($"{BillingPrefix}@jp-1", "Peppol PINT Billing (Japan)");

    /// <summary>The Malaysian specialisation, which MyInvois exchanges over Peppol.</summary>
    public static Profile BillingMy { get; } = Of($"{BillingPrefix}@my-1", "Peppol PINT Billing (Malaysia)");

    /// <summary>The Omani specialisation.</summary>
    public static Profile BillingOm { get; } = Of($"{BillingPrefix}@om-1", "Peppol PINT Billing (Oman)");

    /// <summary>The Singaporean specialisation, which InvoiceNow exchanges.</summary>
    public static Profile BillingSg { get; } = Of($"{BillingPrefix}@sg-1", "Peppol PINT Billing (Singapore)");

    /// <summary>Every PINT profile, for registration.</summary>
    public static IReadOnlyList<Profile> All { get; } =
    [
        Billing,
        BillingEu,
        BillingAe,
        SelfBillingAe,
        BillingAuNz,
        BillingJp,
        BillingMy,
        BillingOm,
        BillingSg,
    ];

    /// <summary>The PINT profile a jurisdiction uses, or <c>null</c> when this library knows none.</summary>
    /// <param name="countryCode">An ISO 3166 alpha-2 code, or <c>AUNZ</c> and <c>EU</c> for the two that
    /// are not one country.</param>
    public static Profile? ForJurisdiction(string? countryCode) =>
        countryCode?.ToUpperInvariant() switch
        {
            "AE" => BillingAe,
            "AU" or "NZ" or "AUNZ" => BillingAuNz,
            "JP" => BillingJp,
            "MY" => BillingMy,
            "OM" => BillingOm,
            "SG" => BillingSg,
            "EU" => BillingEu,
            _ => null,
        };

    /// <summary>The folder names the published artefacts use, one per jurisdiction.</summary>
    /// <remarks>Used by <c>build/fetch-specs.sh pint</c>, and by the test that compares the two.</remarks>
    public static IReadOnlyDictionary<string, string> ArtefactFolders { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["pint-ae"] = $"{BillingPrefix}@ae-1",
            ["pint-aunz"] = $"{BillingPrefix}@aunz-1",
            ["pint-eu"] = $"{BillingPrefix}@eu-1",
            ["pint-jp"] = $"{BillingPrefix}@jp-1",
            ["pint-my"] = $"{BillingPrefix}@my-1",
            ["pint-om"] = $"{BillingPrefix}@om-1",
            ["pint-sg"] = $"{BillingPrefix}@sg-1",
        };

    /// <summary>PINT is carried in UBL; no CII binding is published.</summary>
    private static Profile Of(string identifier, string name) =>
        new(new ProfileIdentifier(identifier), name, DocumentSyntax.Ubl);
}
