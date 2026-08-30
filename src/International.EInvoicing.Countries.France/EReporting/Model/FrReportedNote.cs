using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>A note carried on a reported invoice or one of its lines.</summary>
public sealed class FrReportedNote : InvoiceNode
{
    /// <summary>What the note is about, as a subject code.</summary>
    public CodeField SubjectCode { get; set; }

    /// <summary>The note itself.</summary>
    public TextField Content { get; set; }
}
