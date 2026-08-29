using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>A postal address (BG-5, BG-8, BG-12, BG-15).</summary>
public sealed class PostalAddress : InvoiceNode
{
    /// <summary>BT-35 / BT-50 / BT-64 / BT-75 — first line of the address.</summary>
    public TextField Line1 { get; set; }

    /// <summary>BT-36 / BT-51 / BT-65 / BT-76 — second line.</summary>
    public TextField Line2 { get; set; }

    /// <summary>BT-162 / BT-163 / BT-164 / BT-165 — third line.</summary>
    public TextField Line3 { get; set; }

    /// <summary>BT-37 / BT-52 / BT-66 / BT-77 — city.</summary>
    public TextField City { get; set; }

    /// <summary>BT-38 / BT-53 / BT-67 / BT-78 — post code.</summary>
    public TextField PostCode { get; set; }

    /// <summary>BT-39 / BT-54 / BT-68 / BT-79 — country subdivision, such as a region or state.</summary>
    public TextField CountrySubdivision { get; set; }

    /// <summary>BT-40 / BT-55 / BT-69 / BT-80 — ISO 3166-1 alpha-2 country code.</summary>
    public CodeField CountryCode { get; set; }
}
