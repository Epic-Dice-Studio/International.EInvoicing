using International.EInvoicing.Diagnostics;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Configuration;

/// <summary>How this instance of the library reads documents.</summary>
public sealed class EInvoicingOptions
{
    /// <summary>Resource limits applied to documents received from third parties.</summary>
    public DocumentLimits Limits { get; set; } = DocumentLimits.Default;

    /// <summary>What happens to each diagnostic a reader produces.</summary>
    public DiagnosticPolicy DiagnosticPolicy { get; set; } = DiagnosticPolicy.Balanced;
}
