namespace International.EInvoicing.Countries.France.Lifecycle;

/// <summary>What a party is in a lifecycle exchange, as it appears in <c>ram:RoleCode</c>.</summary>
public static class FrPartyRole
{
    /// <summary>The seller — the party that issued the invoice.</summary>
    public const string Seller = "SE";

    /// <summary>The buyer — the party the invoice is addressed to.</summary>
    public const string Buyer = "BY";

    /// <summary>A platform acting on behalf of a party.</summary>
    public const string Platform = "WK";

    /// <summary>The public portal.</summary>
    public const string PublicPortal = "DFH";
}
