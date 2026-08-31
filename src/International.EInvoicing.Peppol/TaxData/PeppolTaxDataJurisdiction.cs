namespace International.EInvoicing.Peppol.TaxData;

/// <summary>
/// One jurisdiction's tax data document: the same envelope, with its own namespace and its own code lists.
/// </summary>
/// <remarks>
/// <para>
/// OpenPeppol publishes a tax data document per jurisdiction, and they are the same document. Slovakia's and
/// the EU's ViDA rule sets differ by a single assertion out of eighty-eight, and by these three strings — so
/// what varies is data, not code, and this is that data.
/// </para>
/// <para>
/// The Gulf ones are <em>not</em> in here, and deliberately: the Emirati and Omani documents require a source
/// document beside the reported one, a reporter's representative, and jurisdiction-specific content of their
/// own — an Emirati total in AED, an Omani date and time of receipt. They are a second dialect rather than
/// another set of three strings, and inventing one here would be worse than not having it.
/// </para>
/// </remarks>
/// <param name="Name">What to call this rule set in a validation report.</param>
/// <param name="Namespace">The XML namespace of <c>pxs:TaxData</c> for this jurisdiction.</param>
/// <param name="CustomizationId">The specification identifier the document declares (TDT-001).</param>
/// <param name="TaxDataTypes">The codes TDT-007 accepts.</param>
/// <param name="DocumentScopes">The codes TDT-006 accepts.</param>
/// <param name="ReporterRoles">The codes TDT-012 accepts.</param>
public sealed record PeppolTaxDataJurisdiction(
    string Name,
    string Namespace,
    string CustomizationId,
    IReadOnlyList<string> TaxDataTypes,
    IReadOnlyList<string> DocumentScopes,
    IReadOnlyList<string> ReporterRoles)
{
    /// <summary>The business process every tax data document declares (TDT-002), whatever the jurisdiction.</summary>
    public const string ProfileId = "urn:peppol:taxreporting";

    /// <summary>Slovakia, whose B2B mandate reports every invoice from 1 January 2027.</summary>
    public static PeppolTaxDataJurisdiction Slovakia { get; } = new(
        "Peppol Tax Data Document (SK)",
        "urn:peppol:schema:sk-taxdata:1.0",
        "urn:peppol:taxdata:sk-1",
        ["S", "R", "D"],
        ["D", "IC", "INTL"],
        ["C2", "C3"]);

    /// <summary>ViDA — the European regime the national ones will be read against.</summary>
    public static PeppolTaxDataJurisdiction ViDA { get; } = new(
        "Peppol Tax Data Document (ViDA)",
        "urn:peppol:schema:vida-taxdata:1.0",
        "urn:peppol:taxdata:vida-1",
        ["S", "R", "D"],
        ["D", "IC", "INTL"],
        ["C2", "C3"]);

    /// <summary>Every jurisdiction this library carries.</summary>
    public static IReadOnlyList<PeppolTaxDataJurisdiction> All { get; } = [Slovakia, ViDA];

    /// <summary>Whether a value is one this jurisdiction's rules accept for its list.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> is <c>null</c>.</exception>
    public static bool IsValid(IReadOnlyList<string> list, string? value)
    {
        ArgumentNullException.ThrowIfNull(list);

        return value is not null && list.Contains(value, StringComparer.Ordinal);
    }
}
