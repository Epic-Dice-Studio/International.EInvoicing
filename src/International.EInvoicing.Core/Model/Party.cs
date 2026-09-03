using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// An organisation taking part in the invoice: seller (BG-4), buyer (BG-7), payee (BG-10) or the seller's tax
/// representative (BG-11).
/// </summary>
/// <remarks>
/// One type covers all four because the semantic model gives them the same shape; the roles differ in which
/// terms are mandatory, which is a matter for validation rules rather than for the model. Terms that apply to
/// a single role say so on the property.
/// </remarks>
public sealed class Party : InvoiceNode
{
    /// <summary>BT-27 / BT-44 / BT-59 / BT-62 — the party's legal name.</summary>
    public TextField Name { get; set; }

    /// <summary>BT-28 / BT-45 — the name the party trades under, when different.</summary>
    public TextField TradingName { get; set; }

    /// <summary>BT-29 / BT-46 / BT-60 — an identifier for the party, with its scheme.</summary>
    public List<IdentifierField> Identifiers { get; } = [];

    /// <summary>BT-30 / BT-47 / BT-61 — legal registration identifier, such as a company register number.</summary>
    public IdentifierField LegalRegistrationIdentifier { get; set; }

    /// <summary>BT-31 / BT-48 / BT-63 — VAT identifier.</summary>
    public IdentifierField VatIdentifier { get; set; }

    /// <summary>BT-32 — tax registration identifier other than VAT. Seller only.</summary>
    public IdentifierField TaxRegistrationIdentifier { get; set; }

    /// <summary>BT-33 — additional legal information, such as share capital. Seller only.</summary>
    public TextField AdditionalLegalInformation { get; set; }

    /// <summary>BT-34 / BT-49 — electronic address, with its EAS scheme identifier.</summary>
    public IdentifierField ElectronicAddress { get; set; }

    /// <summary>BG-5 / BG-8 / BG-12 — the party's postal address.</summary>
    public PostalAddress? Address { get; set; }

    /// <summary>
    /// Where the company is registered, when that is not where it trades from.
    /// </summary>
    /// <remarks>
    /// EN 16931 carries only the trading address; UBL's post-award documents carry both, and a party whose
    /// registration is in one country and whose warehouse is in another is ordinary rather than exotic.
    /// </remarks>
    public PostalAddress? RegistrationAddress { get; set; }

    /// <summary>The scheme of <see cref="TaxRegistrationIdentifier"/>, when it is not VAT.</summary>
    public CodeField TaxRegistrationScheme { get; set; }

    /// <summary>BG-6 / BG-9 — the party's contact point.</summary>
    public Contact? Contact { get; set; }
}
