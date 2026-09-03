using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>A party taking part in a lifecycle message: who sent it, who issued it, who it is for.</summary>
public sealed class StatusParty : InvoiceNode
{
    /// <summary>The party's identifier, with the scheme that gives it meaning.</summary>
    public IdentifierField GlobalIdentifier { get; set; }

    /// <summary>The party's name.</summary>
    public TextField Name { get; set; }

    /// <summary>
    /// What the party is in this exchange — supplier, buyer, platform. The codes are national: a French
    /// message uses the DGFiP list, and reading one without its profile leaves the code uninterpreted.
    /// </summary>
    public CodeField RoleCode { get; set; }

    /// <summary>The party's electronic address, with its scheme.</summary>
    public IdentifierField ElectronicAddress { get; set; }
}
