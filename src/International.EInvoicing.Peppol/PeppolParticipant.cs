using System.Diagnostics.CodeAnalysis;
using International.EInvoicing.Values;

namespace International.EInvoicing.Peppol;

/// <summary>
/// A Peppol participant identifier: a scheme, and a value inside that scheme.
/// </summary>
/// <remarks>
/// <para>
/// Written <c>0208:0203201340</c> — a Belgian enterprise number — and, in full, prefixed by the
/// <c>iso6523-actorid-upis</c> qualifier the network uses. Both forms are read here; the qualifier is
/// addressing rather than invoicing, so it is dropped once understood.
/// </para>
/// <para>
/// What this checks is the scheme and the shape. Whether the value itself is a valid Belgian enterprise
/// number or a valid Norwegian organisation number is the country package's job, and those packages check it
/// properly rather than by pattern.
/// </para>
/// </remarks>
public readonly record struct PeppolParticipant
{
    /// <summary>The qualifier the Peppol network prefixes an identifier with.</summary>
    public const string Qualifier = "iso6523-actorid-upis";

    private PeppolParticipant(string scheme, string value)
    {
        Scheme = scheme;
        Value = value;
    }

    /// <summary>The scheme, a code from <see cref="PeppolEndpointScheme"/>.</summary>
    public string Scheme { get; }

    /// <summary>The identifier itself, as the scheme defines it.</summary>
    public string Value { get; }

    /// <summary>Whether this holds a participant at all.</summary>
    public bool IsSet => !string.IsNullOrEmpty(Scheme);

    /// <summary>Whether the scheme is one the shipped code list accepts.</summary>
    public bool HasKnownScheme => PeppolEndpointScheme.IsKnown(Scheme);

    /// <summary>Builds a participant from its two parts.</summary>
    /// <param name="scheme">The scheme code, from <see cref="PeppolEndpointScheme"/>.</param>
    /// <param name="value">The identifier.</param>
    /// <exception cref="ArgumentException">Either part is empty, or the scheme is not in the code list.</exception>
    public static PeppolParticipant Create(string scheme, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new PeppolParticipant(PeppolEndpointScheme.Require(scheme), value.Trim());
    }

    /// <summary>
    /// Reads <c>0208:0203201340</c>, or the same with the network qualifier in front.
    /// </summary>
    /// <returns><c>true</c> when it is shaped like a participant identifier, whether or not the scheme is known.</returns>
    public static bool TryParse(string? value, out PeppolParticipant participant)
    {
        participant = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string text = value.Trim();

        if (text.StartsWith(Qualifier + "::", StringComparison.Ordinal))
        {
            text = text[(Qualifier.Length + 2)..];
        }

        int separator = text.IndexOf(':', StringComparison.Ordinal);
        if (separator <= 0 || separator == text.Length - 1)
        {
            return false;
        }

        participant = new PeppolParticipant(text[..separator], text[(separator + 1)..]);
        return true;
    }

    /// <summary>Whether the text is shaped like a participant identifier with a scheme this version knows.</summary>
    public static bool IsValid([NotNullWhen(true)] string? value) =>
        TryParse(value, out PeppolParticipant participant) && participant.HasKnownScheme;

    /// <summary>Reads a participant identifier, or throws when it is not one.</summary>
    /// <exception cref="FormatException">The value is not shaped like a participant identifier.</exception>
    public static PeppolParticipant Parse(string value) =>
        TryParse(value, out PeppolParticipant participant)
            ? participant
            : throw new FormatException(
                $"'{value}' is not a Peppol participant identifier: a scheme, a colon, then the identifier — "
                + "0208:0203201340, optionally prefixed with iso6523-actorid-upis::.");

    /// <summary>The participant as an invoice carries it, in BT-34 or BT-49.</summary>
    public IdentifierField ToElectronicAddress() => new(Value, Scheme);

    /// <summary>The identifier as the network writes it, qualifier included.</summary>
    public string ToQualifiedString() => IsSet ? $"{Qualifier}::{Scheme}:{Value}" : string.Empty;

    /// <inheritdoc />
    public override string ToString() => IsSet ? $"{Scheme}:{Value}" : string.Empty;
}
