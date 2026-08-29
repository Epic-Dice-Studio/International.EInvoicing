namespace International.EInvoicing.Validation;

/// <summary>How much a failed rule matters.</summary>
public enum RuleSeverity
{
    /// <summary>Worth knowing. The document is fine.</summary>
    Information,

    /// <summary>The rule set flags this, but does not reject the document for it.</summary>
    Warning,

    /// <summary>The document does not conform.</summary>
    Error,
}
