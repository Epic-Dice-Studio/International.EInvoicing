namespace International.EInvoicing.OrderX;

/// <summary>
/// The three documents Order-X carries, told apart by <c>ExchangedDocument/TypeCode</c>.
/// </summary>
/// <remarks>
/// Unlike UBL, which gives each document its own root element, all three Order-X documents share one. The
/// type code is the only thing that says which you have, so a reader that ignores it reads an order response
/// as an order.
/// </remarks>
public static class OrderXTypeCodes
{
    /// <summary>An order.</summary>
    public const string Order = "220";

    /// <summary>An order change: an order that amends one already sent.</summary>
    public const string OrderChange = "230";

    /// <summary>An order response: what the seller says about an order.</summary>
    public const string OrderResponse = "231";
}
