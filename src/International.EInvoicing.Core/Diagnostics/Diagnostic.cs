using System.Text;

namespace International.EInvoicing.Diagnostics;

/// <summary>
/// Something worth telling the caller about a document that was read. Readers report diagnostics instead of
/// throwing: the document still parses, and the caller decides what is acceptable.
/// </summary>
public sealed record Diagnostic
{
    private Diagnostic(DiagnosticDescriptor descriptor, DiagnosticSeverity severity, string message)
    {
        Descriptor = descriptor;
        Severity = severity;
        Message = message;
    }

    /// <summary>The definition this diagnostic was created from.</summary>
    public DiagnosticDescriptor Descriptor { get; }

    /// <summary>Effective severity, after policy has been applied.</summary>
    public DiagnosticSeverity Severity { get; init; }

    /// <summary>Human-readable explanation, already formatted.</summary>
    public string Message { get; }

    /// <summary>Stable code, for example <c>EIV2001</c>.</summary>
    public string Code => Descriptor.Code;

    /// <summary>Category, used to configure policy.</summary>
    public DiagnosticCategory Category => Descriptor.Category;

    /// <summary>Link to the catalogue page for this code.</summary>
    public string HelpLink => Descriptor.HelpLink;

    /// <summary>Where in the source document this was found.</summary>
    public SourceLocation Location { get; init; }

    /// <summary>The EN 16931 business term concerned, when there is one.</summary>
    public string? BusinessTerm { get; init; }

    /// <summary>What the reader expected to find.</summary>
    public string? Expected { get; init; }

    /// <summary>What the reader actually found.</summary>
    public string? Found { get; init; }

    /// <summary>What the reader did instead of failing.</summary>
    public string? AppliedFallback { get; init; }

    /// <summary>Creates a diagnostic from its descriptor, using the descriptor's default severity.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="descriptor"/> is <c>null</c>.</exception>
    public static Diagnostic Create(DiagnosticDescriptor descriptor, params object?[] messageArguments)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return new Diagnostic(descriptor, descriptor.DefaultSeverity, descriptor.FormatMessage(messageArguments));
    }

    /// <summary>Returns a copy of this diagnostic with a different severity, as a policy would produce.</summary>
    public Diagnostic WithSeverity(DiagnosticSeverity severity) => this with { Severity = severity };

    /// <inheritdoc />
    public override string ToString()
    {
        var text = new StringBuilder()
            .Append(Code).Append("  ").Append(Severity).Append("  ").Append(Category)
            .Append("  ").Append(Message);

        if (Location.IsKnown)
        {
            text.Append(" at ").Append(Location);
        }

        if (BusinessTerm is not null)
        {
            text.Append(" (").Append(BusinessTerm).Append(')');
        }

        if (AppliedFallback is not null)
        {
            text.Append(" [fallback: ").Append(AppliedFallback).Append(']');
        }

        return text.ToString();
    }
}
