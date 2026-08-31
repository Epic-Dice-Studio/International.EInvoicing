namespace International.EInvoicing.Diagnostics;

/// <summary>
/// Every diagnostic this library can emit. Each code has a page in <c>docs/diagnostics/</c>, and CI fails
/// when one does not.
/// </summary>
public static class DiagnosticCodes
{
    /// <summary>
    /// The document is not well-formed XML, so nothing could be read from it.
    /// </summary>
    /// <remarks>
    /// Declared here as well as in each syntax package because it is the one failure that belongs to no
    /// syntax: a truncated file is not an unrecognised document, and saying so sends the reader looking in
    /// the wrong place.
    /// </remarks>
    public static DiagnosticDescriptor MalformedDocument { get; } = new(
        "EIV5001",
        DiagnosticCategory.Safety,
        DiagnosticSeverity.Fatal,
        "The document is not well-formed XML: {0}");

    /// <summary>An attachment is larger than the limit, so it was not decoded.</summary>
    public static DiagnosticDescriptor AttachmentTooLarge { get; } = new(
        "EIV4003",
        DiagnosticCategory.Safety,
        DiagnosticSeverity.Warning,
        "An attachment (BT-125) of about {0} bytes is over the {1} byte limit and was not decoded.");

    /// <summary>The document carries more of something than the limits allow, and the rest was not read.</summary>
    public static DiagnosticDescriptor TooMany { get; } = new(
        "EIV4004",
        DiagnosticCategory.Safety,
        DiagnosticSeverity.Error,
        "The document carries more than {0} {1}; the rest was not read.");

    /// <summary>The document's bytes are not valid in the encoding it declares.</summary>
    public static DiagnosticDescriptor DeclaredEncodingMismatch { get; } = new(
        "EIV5002",
        DiagnosticCategory.Safety,
        DiagnosticSeverity.Warning,
        "The document declares encoding '{0}' but byte {1} is not valid in it.");

    /// <summary>The document declares an encoding this library does not decode.</summary>
    public static DiagnosticDescriptor UnsupportedEncoding { get; } = new(
        "EIV5003",
        DiagnosticCategory.Safety,
        DiagnosticSeverity.Warning,
        "The document declares encoding '{0}', which this library does not decode.");

    /// <summary>The declared profile identifier matches nothing the library knows.</summary>
    public static DiagnosticDescriptor UnknownProfile { get; } = new(
        "EIV1042",
        DiagnosticCategory.UnknownProfile,
        DiagnosticSeverity.Warning,
        "Profile '{0}' is not registered and is not a profile this library knows.");

    /// <summary>The declared profile is a published standard, but no implementation is registered.</summary>
    public static DiagnosticDescriptor UnsupportedProfile { get; } = new(
        "EIV1043",
        DiagnosticCategory.UnsupportedProfile,
        DiagnosticSeverity.Error,
        "Profile '{0}' is a known standard but no implementation is registered.");

    /// <summary>The document declares an edition of EN 16931 this library does not implement.</summary>
    public static DiagnosticDescriptor UnsupportedEdition { get; } = new(
        "EIV1044",
        DiagnosticCategory.UnsupportedProfile,
        DiagnosticSeverity.Error,
        "Profile '{0}' declares an edition of EN 16931 this library does not implement.");

    /// <summary>A value could not be interpreted as its declared type; the raw text is preserved.</summary>
    public static DiagnosticDescriptor InvalidValue { get; } = new(
        "EIV2001",
        DiagnosticCategory.InvalidValue,
        DiagnosticSeverity.Warning,
        "The value '{0}' could not be read as {1}.");

    /// <summary>A date uses a legal format code this library does not turn into a typed value.</summary>
    public static DiagnosticDescriptor UnsupportedDateFormat { get; } = new(
        "EIV2002",
        DiagnosticCategory.InvalidValue,
        DiagnosticSeverity.Info,
        "Date format code '{0}' is valid but not converted to a typed value.");
}
