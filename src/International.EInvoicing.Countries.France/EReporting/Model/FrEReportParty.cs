using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>A party in the header of a transmission: the sending platform, or the company reporting.</summary>
public sealed class FrEReportParty : InvoiceNode
{
    /// <summary>The identifier, with the scheme that says what it is — <c>0238</c> a platform, <c>0002</c> a SIREN.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>The party's name.</summary>
    public TextField Name { get; set; }

    /// <summary>What the party is here: <c>WK</c> a platform, <c>SE</c> a seller, <c>BY</c> a buyer.</summary>
    public CodeField RoleCode { get; set; }

    /// <summary>Where the party is reachable on the network.</summary>
    public IdentifierField ElectronicAddress { get; set; }
}
