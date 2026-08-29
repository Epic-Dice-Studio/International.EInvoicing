namespace International.EInvoicing.Profiles;

/// <summary>
/// The profiles this library knows exist, whether or not it implements them. Knowing that a profile is a
/// published standard is what separates "we have never heard of this" from "this is a real profile we do not
/// support yet" — and the caller deserves to be told which of the two happened.
/// </summary>
/// <remarks>
/// Identifiers are transcribed from the published specifications listed in <c>docs/standards/</c>. They are
/// re-checked against the artefacts under <c>specs/</c> by the conformance tests once those are fetched.
/// </remarks>
public static class KnownProfiles
{
    /// <summary>The EN 16931 core invoice model, the profile every CIUS restricts.</summary>
    public static Profile En16931Cii { get; } =
        new(new ProfileIdentifier("urn:cen.eu:en16931:2017"), "EN 16931", DocumentSyntax.Cii);

    /// <summary>The EN 16931 core invoice model, in UBL.</summary>
    public static Profile En16931Ubl { get; } =
        new(new ProfileIdentifier("urn:cen.eu:en16931:2017"), "EN 16931", DocumentSyntax.Ubl);

    /// <summary>Factur-X MINIMUM. Not an EN 16931 invoice: header data and totals only.</summary>
    public static Profile FacturXMinimum { get; } = new(
        new ProfileIdentifier("urn:factur-x.eu:1p0:minimum"),
        "Factur-X MINIMUM",
        DocumentSyntax.Cii);

    /// <summary>Factur-X BASIC WL. Adds the VAT breakdown, still without invoice lines.</summary>
    public static Profile FacturXBasicWl { get; } = new(
        new ProfileIdentifier("urn:factur-x.eu:1p0:basicwl"),
        "Factur-X BASIC WL",
        DocumentSyntax.Cii,
        FacturXMinimum.Id);

    /// <summary>Factur-X BASIC. Adds invoice lines.</summary>
    public static Profile FacturXBasic { get; } = new(
        new ProfileIdentifier("urn:cen.eu:en16931:2017#compliant#urn:factur-x.eu:1p0:basic"),
        "Factur-X BASIC",
        DocumentSyntax.Cii,
        En16931Cii.Id);

    /// <summary>Factur-X EXTENDED. Adds elements beyond EN 16931.</summary>
    public static Profile FacturXExtended { get; } = new(
        new ProfileIdentifier("urn:cen.eu:en16931:2017#conformant#urn:factur-x.eu:1p0:extended"),
        "Factur-X EXTENDED",
        DocumentSyntax.Cii,
        En16931Cii.Id);

    /// <summary>Peppol BIS Billing 3.0, in UBL.</summary>
    public static Profile PeppolBisBilling3Ubl { get; } = new(
        new ProfileIdentifier("urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0"),
        "Peppol BIS Billing 3.0",
        DocumentSyntax.Ubl,
        En16931Ubl.Id);

    /// <summary>Peppol BIS Billing 3.0, in CII.</summary>
    public static Profile PeppolBisBilling3Cii { get; } = new(
        new ProfileIdentifier("urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0"),
        "Peppol BIS Billing 3.0",
        DocumentSyntax.Cii,
        En16931Cii.Id);

    /// <summary>XRechnung 3.x, the German CIUS.</summary>
    public static Profile XRechnung3Cius { get; } = new(
        new ProfileIdentifier("urn:cen.eu:en16931:2017#compliant#urn:xoev-de:kosit:standard:xrechnung_3.0"),
        "XRechnung 3.0 (CIUS)",
        DocumentSyntax.Ubl,
        En16931Ubl.Id);

    /// <summary>XRechnung 3.x Extension, which adds elements beyond EN 16931.</summary>
    public static Profile XRechnung3Extension { get; } = new(
        new ProfileIdentifier("urn:cen.eu:en16931:2017#conformant#urn:xoev-de:kosit:extension:xrechnung_3.0"),
        "XRechnung 3.0 (Extension)",
        DocumentSyntax.Ubl,
        XRechnung3Cius.Id);

    /// <summary>Every profile listed here.</summary>
    public static IReadOnlyList<Profile> All { get; } =
    [
        En16931Cii,
        En16931Ubl,
        FacturXMinimum,
        FacturXBasicWl,
        FacturXBasic,
        FacturXExtended,
        PeppolBisBilling3Ubl,
        PeppolBisBilling3Cii,
        XRechnung3Cius,
        XRechnung3Extension,
    ];

    /// <summary>Finds a published profile by identifier and syntax.</summary>
    public static Profile? Find(ProfileIdentifier id, DocumentSyntax syntax) =>
        All.FirstOrDefault(p => p.Id == id && (!syntax.IsKnown || p.Syntax == syntax));
}
