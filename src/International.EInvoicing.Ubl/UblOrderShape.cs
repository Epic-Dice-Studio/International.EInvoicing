using System.Xml.Linq;
using International.EInvoicing.Model;

namespace International.EInvoicing.Ubl;

/// <summary>
/// Which of the two order documents is in hand: the order, or the change that amends it.
/// </summary>
/// <remarks>
/// UBL gives an order change its own root element and one element the order does not have — the sequence
/// number saying which amendment this is. Everything else is the same document, so reading and writing
/// differ by this record rather than by two implementations that drift apart. It is the arrangement the
/// invoice and the credit note already use, for the same reason.
/// </remarks>
internal readonly record struct UblOrderShape(XName Root, bool IsChange)
{
    /// <summary>An order: <c>ubl:Order</c>.</summary>
    public static UblOrderShape Order { get; } = new(UblOrderNames.Order + UblOrderNames.RootElement, false);

    /// <summary>An order change: its own root, carrying the sequence number.</summary>
    public static UblOrderShape Change { get; } = new(
        UblOrderChangeNames.OrderChange + UblOrderChangeNames.RootElement,
        true);

    /// <summary>The shape a document already has, judged by its root element.</summary>
    public static UblOrderShape Of(XElement root) => root.Name == Change.Root ? Change : Order;

    /// <summary>
    /// The shape an order should be written in, judged by what it declares itself to be.
    /// </summary>
    /// <remarks>
    /// The profile decides, not the sequence number: an amendment is an amendment because it says so, and a
    /// first order carrying a sequence number of 1 is still an order.
    /// </remarks>
    public static UblOrderShape For(Model.Order order) =>
        order.SpecificationIdentifier.Value?.Contains("order_change", StringComparison.Ordinal) == true
            ? Change
            : Order;
}
