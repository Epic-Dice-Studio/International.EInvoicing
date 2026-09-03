namespace International.EInvoicing.Model;

/// <summary>
/// What a line is for, when an invoice groups its lines.
/// </summary>
/// <remarks>
/// EN 16931 has no term for this; Factur-X EXTENDED does, and a document using it is read as a flat list of
/// lines by anything that ignores it. The distinction decides arithmetic: a <c>GROUP</c> line's amount is
/// already the sum of the lines beneath it, so totalling every line adds those amounts twice.
/// </remarks>
public static class LineStatusReasonCodes
{
    /// <summary>A heading whose amount is the sum of the lines that name it as their parent.</summary>
    public const string Group = "GROUP";

    /// <summary>An ordinary line, charged for in its own right.</summary>
    public const string Detail = "DETAIL";

    /// <summary>Text carried on the invoice, charged for by nothing.</summary>
    public const string Information = "INFORMATION";

    /// <summary>Every code Factur-X EXTENDED admits here.</summary>
    public static IReadOnlyList<string> All { get; } = [Detail, Group, Information];

    /// <summary>
    /// Whether a line is one to add into a total.
    /// </summary>
    /// <remarks>
    /// A line saying nothing is a detail line: that is what a document without the hierarchy means, and
    /// treating an unmarked line as uncountable would make every EN 16931 invoice total zero.
    /// </remarks>
    public static bool IsCharged(string? code) =>
        !string.Equals(code, Group, StringComparison.Ordinal)
        && !string.Equals(code, Information, StringComparison.Ordinal);
}
