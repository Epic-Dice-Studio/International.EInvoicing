using International.EInvoicing.Profiles;

namespace International.EInvoicing.OrderX;

/// <summary>The three Order-X profiles, and the codes that say which document you are holding.</summary>
/// <remarks>
/// The profiles nest: COMFORT is BASIC plus more, EXTENDED is COMFORT plus more. Declaring the parents is
/// what lets a document claiming EXTENDED still be read when only COMFORT is implemented.
/// </remarks>
public static class OrderXProfiles
{
    /// <summary>The smallest profile: what an order needs to be an order.</summary>
    public static Profile Basic { get; } = new(
        new ProfileIdentifier("urn:order-x.eu:1p0:basic"),
        "Order-X BASIC",
        DocumentSyntax.OrderX);

    /// <summary>The profile the reference document is written in, and the one meant for general use.</summary>
    public static Profile Comfort { get; } = new(
        new ProfileIdentifier("urn:order-x.eu:1p0:comfort"),
        "Order-X COMFORT",
        DocumentSyntax.OrderX,
        Basic.Id);

    /// <summary>Everything the Cross Industry Order can carry.</summary>
    public static Profile Extended { get; } = new(
        new ProfileIdentifier("urn:order-x.eu:1p0:extended"),
        "Order-X EXTENDED",
        DocumentSyntax.OrderX,
        Comfort.Id);

    /// <summary>Every Order-X profile this library knows about.</summary>
    public static IReadOnlyList<Profile> All { get; } = [Basic, Comfort, Extended];
}
