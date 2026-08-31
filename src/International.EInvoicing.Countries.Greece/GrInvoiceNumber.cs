using System.Collections.Frozen;
using System.Globalization;
using International.EInvoicing.Countries.Greece.Identifiers;

namespace International.EInvoicing.Countries.Greece;

/// <summary>
/// The invoice number a Greek supplier must write: six segments separated by a vertical bar.
/// </summary>
/// <remarks>
/// <para>
/// <c>GR-R-001</c> is fatal and unlike anything else in this library. When the supplier is Greek, BT-1 is not
/// a number but a compound key, and every part of it is checked against the rest of the document:
/// </para>
/// <list type="number">
/// <item>the supplier's AFM, which must satisfy its checksum <em>and</em> match the seller's VAT identifier;</item>
/// <item>the issue date as <c>DD/MM/YYYY</c>, which must be the same date as BT-2;</item>
/// <item>the branch, a non-negative integer;</item>
/// <item>the document type, one of six myDATA codes;</item>
/// <item>the series, which may not be empty;</item>
/// <item>the number, which may not be empty.</item>
/// </list>
/// <para>
/// An ordinary invoice number is therefore rejected outright, and a hand-built string is rejected for
/// reasons that are hard to read off a validation report. <see cref="For"/> builds it from the parts.
/// </para>
/// </remarks>
public static class GrInvoiceNumber
{
    /// <summary>What separates the segments.</summary>
    public const char Separator = '|';

    /// <summary>How many segments a Greek invoice number has.</summary>
    public const int Segments = 6;

    private static readonly string[] DocumentTypes = ["1.1", "1.6", "2.1", "2.4", "5.1", "5.2"];

    private static readonly FrozenSet<string> KnownTypes = DocumentTypes.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>The myDATA document types the Greek rules accept in the fourth segment.</summary>
    public static IReadOnlyList<string> KnownDocumentTypes => DocumentTypes;

    /// <summary>Builds the invoice number BT-1 must carry.</summary>
    /// <param name="supplierTaxIdentifier">The supplier's AFM. It must match the seller's VAT identifier.</param>
    /// <param name="issueDate">The invoice's issue date. It must be the same as BT-2.</param>
    /// <param name="branch">The branch number, zero for the head office.</param>
    /// <param name="documentType">A myDATA document type — see <see cref="KnownDocumentTypes"/>.</param>
    /// <param name="series">The series, which may not be empty.</param>
    /// <param name="number">The number within the series, which may not be empty.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="FormatException">The tax identifier is not an AFM.</exception>
    /// <exception cref="ArgumentException">A part is empty, negative, or not a known document type.</exception>
    public static string For(
        string supplierTaxIdentifier,
        DateOnly issueDate,
        int branch,
        string documentType,
        string series,
        string number)
    {
        GrTaxIdentifier identifier = GrTaxIdentifier.Parse(supplierTaxIdentifier);

        ArgumentOutOfRangeException.ThrowIfNegative(branch);

        if (!KnownTypes.Contains(documentType))
        {
            throw new ArgumentException(
                $"'{documentType}' is not a Greek document type. GR-R-001-5 accepts "
                + $"{string.Join(", ", DocumentTypes)}.",
                nameof(documentType));
        }

        ArgumentException.ThrowIfNullOrEmpty(series);
        ArgumentException.ThrowIfNullOrEmpty(number);

        return string.Join(
            Separator,
            identifier.Value,
            issueDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            branch.ToString(CultureInfo.InvariantCulture),
            documentType,
            series,
            number);
    }

    /// <summary>The segments of an invoice number, or an empty list when it does not have six.</summary>
    public static IReadOnlyList<string> Split(string? invoiceNumber) =>
        invoiceNumber?.Split(Separator) is { Length: Segments } parts ? parts : [];

    /// <summary>Whether an invoice number is shaped the way the Greek rules require.</summary>
    /// <remarks>
    /// The parts that can be checked without the rest of the document are checked: the count, the AFM, the
    /// document type, and that the last two are not empty. Whether the AFM and the date match the seller and
    /// BT-2 is a question about the invoice, and the rules ask it there.
    /// </remarks>
    public static bool IsValid(string? invoiceNumber) =>
        Split(invoiceNumber) is { Count: Segments } parts
        && GrTaxIdentifier.IsValid(parts[0])
        && KnownTypes.Contains(parts[3])
        && parts[4].Length > 0
        && parts[5].Length > 0;
}
