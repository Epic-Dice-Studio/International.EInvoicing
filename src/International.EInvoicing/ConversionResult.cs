using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;

namespace International.EInvoicing;

/// <summary>Something a conversion could not carry from one syntax to the other.</summary>
/// <param name="Kind">What sort of loss it is.</param>
/// <param name="Where">Where it was, as far as the source recorded it.</param>
/// <param name="What">What was there.</param>
public sealed record ConversionLoss(ConversionLossKind Kind, string Where, string What)
{
    /// <inheritdoc />
    public override string ToString() => $"{Kind} at {Where}: {What}";
}

/// <summary>The kinds of thing a conversion loses.</summary>
public enum ConversionLossKind
{
    /// <summary>
    /// An element the source syntax carried that the model has no field for, so the target cannot write it.
    /// </summary>
    /// <remarks>
    /// This is the honest cost of converting: extension data is by definition syntax-specific, and there is
    /// nowhere in the other syntax for it to go.
    /// </remarks>
    SyntaxSpecificContent,

    /// <summary>Something the converted document reported when it was read back.</summary>
    ReportedOnReread,
}

/// <summary>
/// A document converted to another syntax, and what the conversion cost.
/// </summary>
/// <remarks>
/// <para>
/// Converting between UBL and CII is a real requirement — a French recipient must accept both, and Factur-X
/// besides — and doing it silently is the dangerous version. The report is the feature.
/// </para>
/// <para>
/// <b>What it measures, and what it does not.</b> The losses are found rather than predicted: the converted
/// document is read back, and what that reports is recorded, along with every extension element the source
/// carried — those are syntax-specific by definition and have nowhere to go. It does not diff the two models
/// field by field. Every mapped business term survives by construction, because both syntaxes are written
/// from the same model; what does not survive is what was never mapped, and that is exactly what extension
/// data holds.
/// </para>
/// </remarks>
/// <param name="Xml">The converted document.</param>
/// <param name="Format">The syntax it is written in.</param>
/// <param name="Invoice">The invoice as the converted document reads back.</param>
/// <param name="Losses">What the conversion could not carry.</param>
/// <param name="Diagnostics">Everything reading the converted document reported.</param>
public sealed record ConversionResult(
    string Xml,
    DocumentFormat Format,
    EInvoice? Invoice,
    IReadOnlyList<ConversionLoss> Losses,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>Whether the conversion carried everything the source had.</summary>
    public bool IsLossless => Losses.Count == 0;

    /// <inheritdoc />
    public override string ToString() => IsLossless
        ? $"Converted to {Format}, losing nothing."
        : $"Converted to {Format}, losing {Losses.Count} thing(s):{Environment.NewLine}"
            + string.Join(Environment.NewLine, Losses.Select(loss => "  " + loss));
}
