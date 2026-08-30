namespace International.EInvoicing.Playground.Services;

/// <summary>What a document turned out to be, once the site looked at it rather than asking the visitor.</summary>
public enum DocumentKind
{
    /// <summary>Nothing recognisable.</summary>
    Unknown,

    /// <summary>A UBL 2.1 invoice or credit note.</summary>
    Ubl,

    /// <summary>A UN/CEFACT Cross Industry Invoice.</summary>
    Cii,

    /// <summary>A lifecycle status message.</summary>
    Cdar,

    /// <summary>A PDF, which may or may not carry an invoice inside it.</summary>
    Pdf,
}
