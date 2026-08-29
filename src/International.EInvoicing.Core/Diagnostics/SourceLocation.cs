namespace International.EInvoicing.Diagnostics;

/// <summary>Where something was found in the source document.</summary>
/// <param name="Path">Path to the node, for example <c>/rsm:CrossIndustryInvoice/rsm:ExchangedDocument</c>.</param>
/// <param name="Line">1-based line number, or 0 when unknown.</param>
/// <param name="Position">1-based column, or 0 when unknown.</param>
public readonly record struct SourceLocation(string? Path, int Line, int Position)
{
    /// <summary>No location information.</summary>
    public static SourceLocation None => default;

    /// <summary>Whether this location points at anything.</summary>
    public bool IsKnown => Path is not null || Line > 0;

    /// <inheritdoc />
    public override string ToString() => (Path, Line) switch
    {
        (null, 0) => "unknown location",
        (null, _) => $"line {Line}, position {Position}",
        (_, 0) => Path!,
        _ => $"{Path} (line {Line}, position {Position})",
    };
}
