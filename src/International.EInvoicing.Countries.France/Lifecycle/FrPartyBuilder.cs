using International.EInvoicing.Cdar.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.Lifecycle;

/// <summary>Describes a party in a lifecycle message without spelling out its schemes and role codes.</summary>
public sealed class FrPartyBuilder
{
    private readonly StatusParty _party = new();

    internal FrPartyBuilder()
    {
    }

    /// <summary>A company, identified by its SIREN.</summary>
    /// <exception cref="ArgumentException"><paramref name="siren"/> is empty.</exception>
    public FrPartyBuilder Company(string siren)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(siren);

        _party.GlobalIdentifier = new IdentifierField(siren, FrPartyScheme.Company);
        return this;
    }

    /// <summary>A platform, identified by its platform number.</summary>
    /// <exception cref="ArgumentException"><paramref name="identifier"/> is empty.</exception>
    public FrPartyBuilder Platform(string identifier, string? name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        _party.GlobalIdentifier = new IdentifierField(identifier, FrPartyScheme.Platform);
        _party.RoleCode = FrPartyRole.Platform;

        if (name is not null)
        {
            _party.Name = name;
        }

        return this;
    }

    /// <summary>The party's name.</summary>
    public FrPartyBuilder Named(string name)
    {
        _party.Name = name;
        return this;
    }

    /// <summary>The party is the seller.</summary>
    public FrPartyBuilder AsSeller() => InRole(FrPartyRole.Seller);

    /// <summary>The party is the buyer.</summary>
    public FrPartyBuilder AsBuyer() => InRole(FrPartyRole.Buyer);

    /// <summary>The party plays some other role, named by its code.</summary>
    public FrPartyBuilder InRole(string roleCode)
    {
        _party.RoleCode = roleCode;
        return this;
    }

    /// <summary>Where statuses are delivered for this party.</summary>
    /// <exception cref="ArgumentException"><paramref name="address"/> is empty.</exception>
    public FrPartyBuilder ReachableAt(string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        _party.ElectronicAddress = new IdentifierField(address, FrPartyScheme.RoutingAddress);
        return this;
    }

    internal static StatusParty Build(Action<FrPartyBuilder> configure)
    {
        var builder = new FrPartyBuilder();
        configure(builder);
        return builder._party;
    }
}
