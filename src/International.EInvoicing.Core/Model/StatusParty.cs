using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>A party taking part in a lifecycle message: who sent it, who issued it, who it is for.</summary>
public sealed class StatusParty : InvoiceNode
{
    /// <summary>The party's identifier, with the scheme that gives it meaning.</summary>
    public IdentifierField GlobalIdentifier { get; set; }

    /// <summary>The party's registered name.</summary>
    public TextField Name { get; set; }

    /// <summary>
    /// The name the party trades under, when it differs from the registered one.
    /// </summary>
    /// <remarks>
    /// The same distinction EN 16931 draws with BT-27 and BT-28, and UBL keeps it in two different elements:
    /// carrying both is what lets a message be written back into the element it was read from.
    /// </remarks>
    public TextField TradingName { get; set; }

    /// <summary>
    /// What the party is in this exchange — supplier, buyer, platform. The codes are national: a French
    /// message uses the DGFiP list, and reading one without its profile leaves the code uninterpreted.
    /// </summary>
    public CodeField RoleCode { get; set; }

    /// <summary>The party's electronic address, with its scheme.</summary>
    public IdentifierField ElectronicAddress { get; set; }

    /// <summary>Who to talk to about this message, when the sender named somebody.</summary>
    public Contact? Contact { get; set; }
}
