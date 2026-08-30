using System.Diagnostics.CodeAnalysis;
using International.EInvoicing.Identifiers;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.Germany.Identifiers;

/// <summary>
/// A Leitweg-ID: the routing identifier a German public-sector recipient requires in BT-10.
/// </summary>
/// <remarks>
/// <para>
/// Three parts separated by hyphens: a coarse address of 2 to 12 characters, an optional fine address of up
/// to 30, and two check digits. The check follows ISO/IEC 7064 MOD 97-10, the scheme behind IBANs.
/// </para>
/// <para>
/// It is worth checking rather than pattern-matching. An invoice sent to the wrong authority is not rejected
/// — it is delivered somewhere else, and nobody notices until payment does not arrive.
/// </para>
/// </remarks>
public readonly record struct DeLeitwegId
{
    private DeLeitwegId(string coarse, string fine, string check)
    {
        CoarseAddress = coarse;
        FineAddress = fine;
        CheckDigits = check;
    }

    /// <summary>The coarse address: which authority, 2 to 12 characters.</summary>
    public string CoarseAddress { get; }

    /// <summary>The fine address: which part of it, up to 30 characters. Empty when there is none.</summary>
    public string FineAddress { get; }

    /// <summary>The two check digits.</summary>
    public string CheckDigits { get; }

    /// <summary>Whether this holds a Leitweg-ID at all.</summary>
    public bool IsSet => !string.IsNullOrEmpty(CoarseAddress);

    /// <summary>Reads a Leitweg-ID and verifies its check digits.</summary>
    public static bool TryParse(string? value, out DeLeitwegId leitwegId)
    {
        leitwegId = default;

        if (value is null)
        {
            return false;
        }

        string[] parts = value.Trim().Split('-');
        if (parts.Length is < 2 or > 3)
        {
            return false;
        }

        string coarse = parts[0];
        string fine = parts.Length == 3 ? parts[1] : string.Empty;
        string check = parts[^1];

        if (!IsWellShaped(coarse, fine, check))
        {
            return false;
        }

        if (!string.Equals(CheckDigit.Iso7064Mod97(coarse + fine), check, StringComparison.Ordinal))
        {
            return false;
        }

        leitwegId = new DeLeitwegId(coarse, fine, check);
        return true;
    }

    /// <summary>Whether the text is a valid Leitweg-ID.</summary>
    public static bool IsValid([NotNullWhen(true)] string? value) => TryParse(value, out _);

    /// <summary>Reads a Leitweg-ID, or throws when it is not one.</summary>
    /// <exception cref="FormatException">The value is not a valid Leitweg-ID.</exception>
    public static DeLeitwegId Parse(string value) =>
        TryParse(value, out DeLeitwegId leitwegId)
            ? leitwegId
            : throw new FormatException($"'{value}' is not a Leitweg-ID, or its check digits do not match.");

    /// <summary>Builds a Leitweg-ID from its parts, computing the check digits.</summary>
    /// <exception cref="ArgumentException">The parts are not shaped as the specification requires.</exception>
    public static DeLeitwegId Create(string coarseAddress, string fineAddress = "")
    {
        ArgumentNullException.ThrowIfNull(coarseAddress);
        ArgumentNullException.ThrowIfNull(fineAddress);

        string? check = CheckDigit.Iso7064Mod97(coarseAddress + fineAddress);

        if (check is null || !IsWellShaped(coarseAddress, fineAddress, check))
        {
            throw new ArgumentException(
                "A Leitweg-ID needs a coarse address of 2 to 12 alphanumeric characters and a fine address of "
                + "at most 30.",
                nameof(coarseAddress));
        }

        return new DeLeitwegId(coarseAddress, fineAddress, check);
    }

    /// <summary>The identifier as an invoice carries it, in BT-10.</summary>
    public TextField ToBuyerReference() => new(ToString());

    /// <inheritdoc />
    public override string ToString() =>
        !IsSet
            ? string.Empty
            : FineAddress.Length == 0
                ? $"{CoarseAddress}-{CheckDigits}"
                : $"{CoarseAddress}-{FineAddress}-{CheckDigits}";

    private static bool IsWellShaped(string coarse, string fine, string check) =>
        coarse.Length is >= 2 and <= 12
        && coarse.All(char.IsAsciiLetterOrDigit)
        && fine.Length <= 30
        && fine.All(char.IsAsciiLetterOrDigit)
        && check.Length == 2
        && check.All(char.IsAsciiDigit);
}
