using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Profiles;

/// <summary>
/// What resolving a document's declared profile produced: the profile actually used, whether it is the one
/// the document asked for, and what was given up along the way.
/// </summary>
/// <param name="Profile">The profile used to read the document, or <c>null</c> when reading generically.</param>
/// <param name="Declared">The identifier the document declared.</param>
/// <param name="Outcome">How the profile was arrived at.</param>
/// <param name="Diagnostics">What the caller must be told about the downgrade, if any.</param>
public sealed record ProfileResolution(
    Profile? Profile,
    ProfileIdentifier Declared,
    ProfileResolutionOutcome Outcome,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>Whether the document is being read with exactly the profile it declared.</summary>
    public bool IsExact => Outcome == ProfileResolutionOutcome.Exact;

    /// <summary>
    /// Whether validation against this document can be complete. It cannot when a fallback was applied: the
    /// declared profile's own rules never ran.
    /// </summary>
    public bool AllowsCompleteValidation => IsExact;
}

/// <summary>How a profile was arrived at.</summary>
public enum ProfileResolutionOutcome
{
    /// <summary>The declared profile is registered and was used.</summary>
    Exact,

    /// <summary>The declared profile is a published standard, but no implementation is registered.</summary>
    FellBackFromUnsupported,

    /// <summary>The declared identifier matches nothing the library knows.</summary>
    FellBackFromUnknown,

    /// <summary>The document declared no profile at all.</summary>
    Undeclared,
}
