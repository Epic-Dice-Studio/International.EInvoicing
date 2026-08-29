using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Values;

/// <summary>
/// What every field exposes regardless of the type it carries. Lets tooling walk a document generically —
/// a validator, an explorer, a difference report — without knowing each field type.
/// </summary>
public interface IField
{
    /// <summary>The exact text from the document, or <c>null</c> for a field built in code.</summary>
    string? Raw { get; }

    /// <summary>Where the value was found in the document.</summary>
    SourceLocation Location { get; }

    /// <summary>Why the raw text could not be converted, when it could not.</summary>
    Diagnostic? Diagnostic { get; }

    /// <summary>Whether the field carries anything at all: a value, or a source that produced none.</summary>
    bool IsSet { get; }

    /// <summary>Whether a typed value is available.</summary>
    bool HasValue { get; }

    /// <summary>Whether the document carried text that could not be converted to a typed value.</summary>
    bool IsRawOnly { get; }

    /// <summary>
    /// Whether the field was read from a document. A field that was not is written from its value; a field
    /// that was, and has not been replaced, is written back from <see cref="Raw"/>.
    /// </summary>
    bool IsFromSource { get; }

    /// <summary>The typed value, boxed. For generic tooling only.</summary>
    object? UntypedValue { get; }
}
