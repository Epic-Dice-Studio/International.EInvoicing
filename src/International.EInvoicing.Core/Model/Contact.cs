using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>A contact point at a party (BG-6, BG-9).</summary>
public sealed class Contact : InvoiceNode
{
    /// <summary>BT-41 / BT-56 — contact point, a person or a department.</summary>
    public TextField Name { get; set; }

    /// <summary>The department they work in, when the document names one beside the person.</summary>
    public TextField Department { get; set; }

    /// <summary>What kind of contact this is — an ordering contact, a delivery contact.</summary>
    public CodeField TypeCode { get; set; }

    /// <summary>BT-42 / BT-57 — telephone number.</summary>
    public TextField Telephone { get; set; }

    /// <summary>BT-43 / BT-58 — email address.</summary>
    public TextField Email { get; set; }
}
