using International.EInvoicing.Profiles;

namespace International.EInvoicing.Zugferd1;

/// <summary>The ZUGFeRD 1.0 profiles.</summary>
/// <remarks>
/// BASIC, COMFORT and EXTENDED, the three FeRD published in 2013 and the ancestors of the Factur-X ones. A
/// document declaring the bare <c>urn:ferd:CrossIndustryDocument:invoice:1p0</c> with no profile after it is
/// read as BASIC, which is what the schema's own default amounts to.
/// </remarks>
public static class Zugferd1Profiles
{
    /// <summary>The base identifier, which some documents declare without naming a profile.</summary>
    public const string BaseIdentifier = "urn:ferd:CrossIndustryDocument:invoice:1p0";

    /// <summary>What an invoice needs to be an invoice.</summary>
    public static Profile Basic { get; } = new(
        new ProfileIdentifier(BaseIdentifier + ":basic"),
        "ZUGFeRD 1.0 BASIC",
        DocumentSyntax.Zugferd1);

    /// <summary>The profile most 2013 documents were written in.</summary>
    public static Profile Comfort { get; } = new(
        new ProfileIdentifier(BaseIdentifier + ":comfort"),
        "ZUGFeRD 1.0 COMFORT",
        DocumentSyntax.Zugferd1,
        Basic.Id);

    /// <summary>Everything ZUGFeRD 1.0 could carry.</summary>
    public static Profile Extended { get; } = new(
        new ProfileIdentifier(BaseIdentifier + ":extended"),
        "ZUGFeRD 1.0 EXTENDED",
        DocumentSyntax.Zugferd1,
        Comfort.Id);

    /// <summary>A document that names the standard and no profile within it.</summary>
    public static Profile Unprofiled { get; } = new(
        new ProfileIdentifier(BaseIdentifier),
        "ZUGFeRD 1.0",
        DocumentSyntax.Zugferd1,
        Basic.Id);

    /// <summary>Every ZUGFeRD 1.0 profile this library knows about.</summary>
    public static IReadOnlyList<Profile> All { get; } = [Basic, Comfort, Extended, Unprofiled];
}
