using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Values;

/// <summary>
/// Where a field's value came from: the exact text in the document, its position, and any diagnostic
/// explaining why it could not be typed. A field built in code has no source.
/// </summary>
/// <param name="Raw">The exact text as it appears in the document.</param>
/// <param name="Location">Where it was found.</param>
/// <param name="Diagnostic">Why the raw text could not be converted, when it could not.</param>
public sealed record FieldSource(string? Raw, SourceLocation Location = default, Diagnostic? Diagnostic = null);
