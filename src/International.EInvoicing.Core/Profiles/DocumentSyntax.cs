namespace International.EInvoicing.Profiles;

/// <summary>
/// The XML dialect a document is written in. Not an enumeration, so a caller can introduce a syntax the
/// library does not ship.
/// </summary>
/// <param name="Name">Lowercase identifier, for example <c>ubl</c>.</param>
public readonly record struct DocumentSyntax(string Name)
{
    /// <summary>OASIS UBL 2.1.</summary>
    public static DocumentSyntax Ubl => new("ubl");

    /// <summary>UN/CEFACT Cross Industry Invoice.</summary>
    public static DocumentSyntax Cii => new("cii");

    /// <summary>UN/CEFACT Cross Domain Acknowledgement and Response.</summary>
    public static DocumentSyntax Cdar => new("cdar");

    /// <summary>
    /// Order-X — the UN/CEFACT Cross Industry Order, as profiled by FNFE-MPE and FeRD.
    /// </summary>
    /// <remarks>
    /// Named apart from <see cref="Cii"/> although both are CII: it is a different UN/CEFACT message on a
    /// later version of the same data types, so nothing that reads or validates one reads or validates the
    /// other.
    /// </remarks>
    public static DocumentSyntax OrderX => new("order-x");

    /// <summary>No syntax determined.</summary>
    public static DocumentSyntax Unknown => default;

    /// <summary>Whether a syntax was determined.</summary>
    public bool IsKnown => !string.IsNullOrEmpty(Name);

    /// <inheritdoc />
    public override string ToString() => Name ?? "unknown";
}
