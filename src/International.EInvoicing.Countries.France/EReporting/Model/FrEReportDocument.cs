using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>The header of a transmission: what it is, who sent it, and on whose behalf.</summary>
public sealed class FrEReportDocument : InvoiceNode
{
    /// <summary>The transmission's identifier, at most 35 characters and without double spaces.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>The name the sender gave the transmission.</summary>
    public TextField Name { get; set; }

    /// <summary>When the transmission was created, to the second.</summary>
    public DateTimeField IssuedAt { get; set; }

    /// <summary>Whether this is a first transmission or one that replaces an earlier one.</summary>
    /// <remarks>See <see cref="FrEReportCodes.InitialTransmission"/> and <see cref="FrEReportCodes.Replacement"/>.</remarks>
    public CodeField TypeCode { get; set; }

    /// <summary>The platform transmitting the report.</summary>
    public FrEReportParty? Sender { get; set; }

    /// <summary>The company the report is about — the <em>déclarant</em>.</summary>
    public FrEReportParty? Issuer { get; set; }
}
