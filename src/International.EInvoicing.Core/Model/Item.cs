using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>What is being invoiced on a line (BG-31).</summary>
public sealed class Item : InvoiceNode
{
    /// <summary>BT-153 — the item's name.</summary>
    public TextField Name { get; set; }

    /// <summary>BT-154 — a description of the item.</summary>
    public TextField Description { get; set; }

    /// <summary>BT-155 — the seller's identifier for the item.</summary>
    public IdentifierField SellerIdentifier { get; set; }

    /// <summary>BT-156 — the buyer's identifier for the item.</summary>
    public IdentifierField BuyerIdentifier { get; set; }

    /// <summary>BT-157 — a standard identifier such as a GTIN, with its scheme.</summary>
    public IdentifierField StandardIdentifier { get; set; }

    /// <summary>BT-158 — classifications, such as CPV or UNSPSC, each with its list and its name.</summary>
    public List<ItemClassification> Classifications { get; } = [];

    /// <summary>BT-159 — the item's country of origin.</summary>
    public CodeField OriginCountryCode { get; set; }

    /// <summary>BG-32 — named characteristics of the item.</summary>
    public List<ItemCharacteristic> Characteristics { get; } = [];
}
