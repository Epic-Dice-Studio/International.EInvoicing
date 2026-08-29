namespace International.EInvoicing.Diagnostics;

/// <summary>What kind of problem a diagnostic reports. Policies are configured per category.</summary>
public enum DiagnosticCategory
{
    /// <summary>The declared profile matches nothing registered.</summary>
    UnknownProfile,

    /// <summary>The profile is recognised but no implementation is registered for it.</summary>
    UnsupportedProfile,

    /// <summary>A value could not be interpreted; its raw text is preserved.</summary>
    InvalidValue,

    /// <summary>A value is well formed but its code is not in the expected code list.</summary>
    UnknownCode,

    /// <summary>An element was not mapped to the model and was kept as extension data.</summary>
    UnmappedElement,

    /// <summary>Cardinality, ordering or a missing mandatory group.</summary>
    StructuralAnomaly,

    /// <summary>A container problem: PDF, embedded attachment, XMP metadata.</summary>
    Container,

    /// <summary>A resource limit was exceeded, or the input is not well-formed XML.</summary>
    Safety,

    /// <summary>The caller's own registration or policy configuration is inconsistent.</summary>
    Configuration,
}
