using International.EInvoicing.Building;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Playground.Services;

/// <summary>One profile a country exchanges, with a label a person can choose between.</summary>
/// <param name="Label">What to call it in a menu.</param>
/// <param name="Profile">The profile itself, from the country package.</param>
public sealed record PlaygroundProfile(string Label, Profile Profile);

/// <summary>
/// A country, as the playground offers it: the profiles it exchanges, the currency it invoices in, and the
/// two or three things it asks for that EN 16931 does not.
/// </summary>
/// <remarks>
/// This is the shape of the library's own country packages, reflected into a menu. The point of choosing a
/// country first is that almost every question after it has a different answer per country — which profile,
/// which business process, which identifier scheme, which rules — and a person building their first invoice
/// should not have to know that before they start.
/// </remarks>
public sealed class PlaygroundCountry
{
    /// <summary>The ISO 3166 alpha-2 code, or <c>"--"</c> for the country-neutral entry.</summary>
    public required string Code { get; init; }

    /// <summary>What to call it in the menu.</summary>
    public required string Name { get; init; }

    /// <summary>The currency invoices are usually denominated in.</summary>
    public required string Currency { get; init; }

    /// <summary>The profiles this country exchanges, most usual first.</summary>
    public required IReadOnlyList<PlaygroundProfile> Profiles { get; init; }

    /// <summary>The shortcut type in the country package, or <c>null</c> when there is none.</summary>
    public string? Facade { get; init; }

    /// <summary>A legal identifier for the seller that this country's rules accept.</summary>
    public string SellerIdentifier { get; init; } = string.Empty;

    /// <summary>A legal identifier for the buyer.</summary>
    public string BuyerIdentifier { get; init; } = string.Empty;

    /// <summary>
    /// A VAT identifier for the seller, when this country's <see cref="Describe"/> does not derive one.
    /// </summary>
    /// <remarks>
    /// BR-S-02 and BR-CO-26 refuse a standard-rated invoice whose seller cannot be identified for VAT, and
    /// the Dutch and Icelandic rules care about the legal entity identifier rather than the VAT one — so the
    /// two are set separately.
    /// </remarks>
    public string SellerVat { get; init; } = string.Empty;

    /// <summary>A VAT identifier for the buyer, when one is wanted.</summary>
    public string BuyerVat { get; init; } = string.Empty;

    /// <summary>Anything the builder must add beyond the profile — a business process, a mention.</summary>
    public Func<EInvoiceBuilder, EInvoiceBuilder> Prepare { get; init; } = builder => builder;

    /// <summary>How this country identifies a party, which is rarely just a name.</summary>
    public Func<PartyBuilder, string, string, PartyBuilder> Describe { get; init; } =
        (party, _, name) => party.Named(name);

    /// <summary>The line of C# that creates the library for this country.</summary>
    public string CreationSnippet { get; init; } = "EInvoicing library = EInvoicing.CreateDefault();";

    /// <summary>How a party is written in the snippet, so the code shown is the code that would run.</summary>
    public Func<string, string, string> DescribeSnippet { get; init; } =
        (_, name) => $"party => party.Named(\"{name}\")";

    /// <summary>The rule sets that apply here, and whether this build carries them.</summary>
    public IReadOnlyList<PlaygroundRuleSet> RuleSets { get; init; } = [];

    /// <summary>One thing about this country that surprises people, or <c>null</c>.</summary>
    public string? Trap { get; init; }

    /// <summary>Documents beyond the invoice that this country exchanges.</summary>
    public IReadOnlyList<string> ExtraDocuments { get; init; } = [];
}

/// <summary>A rule set a country's invoices are judged by, and whether it is in this build.</summary>
/// <param name="Name">What the publisher calls it.</param>
/// <param name="Embedded">Whether this build carries it, or it has to be fetched.</param>
/// <param name="Note">Why it is not here, when it is not.</param>
public sealed record PlaygroundRuleSet(string Name, bool Embedded, string? Note = null);
