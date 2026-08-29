using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Building;

/// <summary>Builds a seller, buyer, payee or tax representative.</summary>
public sealed class PartyBuilder
{
    private readonly Party _party = new();

    /// <summary>BT-27 / BT-44 — the party's legal name.</summary>
    public PartyBuilder Named(string name)
    {
        _party.Name = name;
        return this;
    }

    /// <summary>BT-28 / BT-45 — the name the party trades under.</summary>
    public PartyBuilder TradingAs(string tradingName)
    {
        _party.TradingName = tradingName;
        return this;
    }

    /// <summary>BT-29 / BT-46 — an identifier for the party, optionally with its scheme.</summary>
    public PartyBuilder WithIdentifier(string identifier, string? schemeId = null)
    {
        _party.Identifiers.Add(new IdentifierField(identifier, schemeId));
        return this;
    }

    /// <summary>BT-30 / BT-47 — legal registration identifier, such as a company register number.</summary>
    public PartyBuilder WithLegalRegistration(string identifier, string? schemeId = null)
    {
        _party.LegalRegistrationIdentifier = new IdentifierField(identifier, schemeId);
        return this;
    }

    /// <summary>BT-31 / BT-48 — VAT identifier.</summary>
    public PartyBuilder WithVatIdentifier(string vatIdentifier)
    {
        _party.VatIdentifier = vatIdentifier;
        return this;
    }

    /// <summary>BT-34 / BT-49 — electronic address and its EAS scheme identifier.</summary>
    public PartyBuilder WithElectronicAddress(string address, string schemeId)
    {
        _party.ElectronicAddress = new IdentifierField(address, schemeId);
        return this;
    }

    /// <summary>BG-5 / BG-8 — the party's postal address.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public PartyBuilder WithAddress(Action<PostalAddress> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        _party.Address ??= new PostalAddress();
        configure(_party.Address);
        return this;
    }

    /// <summary>BG-6 / BG-9 — the party's contact point.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public PartyBuilder WithContact(Action<Contact> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        _party.Contact ??= new Contact();
        configure(_party.Contact);
        return this;
    }

    /// <summary>Reaches the party directly, for anything this builder does not cover.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public PartyBuilder Extend(Action<Party> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(_party);
        return this;
    }

    internal Party Build() => _party;
}
