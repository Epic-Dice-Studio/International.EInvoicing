namespace International.EInvoicing.Countries.France.Lifecycle;

/// <summary>
/// The identifier schemes French lifecycle messages use, as they appear in <c>schemeID</c>.
/// </summary>
/// <remarks>
/// These are entries of the Electronic Address Scheme code list. The names here describe the role each plays
/// in a lifecycle message, which is what a caller needs to choose between them.
/// </remarks>
public static class FrPartyScheme
{
    /// <summary>A company, identified by its SIREN.</summary>
    public const string Company = "0002";

    /// <summary>A routing address, which is how a platform is reached for statuses.</summary>
    public const string RoutingAddress = "0225";

    /// <summary>A platform or the public portal itself.</summary>
    public const string Platform = "0238";
}
