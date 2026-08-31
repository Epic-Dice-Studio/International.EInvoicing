namespace International.EInvoicing.Countries.Slovakia.TaxData;

/// <summary>
/// The three code lists a tax data document is judged against, as the published rules enumerate them.
/// </summary>
/// <remarks>
/// <c>ibr-tdd-06</c>, <c>ibr-tdd-08</c> and <c>ibr-tdd-09</c> each say "MUST be coded according to the
/// applicable code list" and then carry that list inside the rule. These are those lists, read from the
/// artefact rather than from prose; what each code means is in the Slovak specification.
/// </remarks>
public static class SkTaxDataCodes
{
    /// <summary>What the document reports (TDT-007): <c>S</c>, <c>R</c> or <c>D</c>.</summary>
    public static IReadOnlyList<string> TaxDataTypes { get; } = ["S", "R", "D"];

    /// <summary>How far the transaction reaches (TDT-006): domestic, intra-Community, or international.</summary>
    public static IReadOnlyList<string> DocumentScopes { get; } = ["D", "IC", "INTL"];

    /// <summary>Which corner is reporting (TDT-012): <c>C2</c> the sender's agent, <c>C3</c> the receiver's.</summary>
    public static IReadOnlyList<string> ReporterRoles { get; } = ["C2", "C3"];

    /// <summary>Whether a value is one the rules accept for its list.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> is <c>null</c>.</exception>
    public static bool IsValid(IReadOnlyList<string> list, string? value)
    {
        ArgumentNullException.ThrowIfNull(list);

        return value is not null && list.Contains(value, StringComparer.Ordinal);
    }
}
