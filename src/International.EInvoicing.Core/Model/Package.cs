using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>One package within a handling unit.</summary>
public sealed class Package : InvoiceNode
{
    /// <summary>The package's identifier.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>How it is packaged (UN/ECE Recommendation 21).</summary>
    public CodeField PackagingTypeCode { get; set; }
}
