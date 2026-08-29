namespace International.EInvoicing.Profiles;

/// <summary>
/// The specification identifier a document declares (BT-24). It is the key everything resolves on: which
/// mapping applies, which rules run, what the document claims to conform to.
/// </summary>
/// <param name="Value">The URN, for example <c>urn:cen.eu:en16931:2017</c>.</param>
public readonly record struct ProfileIdentifier(string Value)
{
    /// <summary>No identifier declared.</summary>
    public static ProfileIdentifier None => default;

    /// <summary>Whether an identifier was declared.</summary>
    public bool IsDeclared => !string.IsNullOrWhiteSpace(Value);

    /// <summary>Reads an identifier from a document, trimming surrounding whitespace.</summary>
    public static ProfileIdentifier FromDocument(string? value) =>
        string.IsNullOrWhiteSpace(value) ? None : new ProfileIdentifier(value.Trim());

    /// <inheritdoc />
    public override string ToString() => IsDeclared ? Value : "(none)";
}
