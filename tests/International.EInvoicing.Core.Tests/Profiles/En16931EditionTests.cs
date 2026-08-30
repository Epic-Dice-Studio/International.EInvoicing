using International.EInvoicing.Diagnostics;
using International.EInvoicing.Profiles;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Core.Tests.Profiles;

/// <summary>
/// The standard itself has versions. CEN published EN 16931-1:2026 and withdrew the 2017 edition this
/// library implements, so documents written against both will be in circulation for years — and the one we
/// cannot fully read must say which of the two it is.
/// </summary>
public class En16931EditionTests
{
    [Theory]
    [InlineData("urn:cen.eu:en16931:2017", 2017)]
    [InlineData("urn:cen.eu:en16931:2026", 2026)]
    [InlineData("urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0", 2017)]
    [InlineData("urn:cen.eu:en16931:2026#compliant#urn:something:new", 2026)]
    public void TheEditionIsReadFromTheYearInTheIdentifier(string identifier, int year)
    {
        En16931Edition.Of(new ProfileIdentifier(identifier)).ShouldBe(new En16931Edition(year));
    }

    [Theory]
    [InlineData("urn:factur-x.eu:1p0:minimum")]
    [InlineData("urn:cen.eu:en16931:next")]
    [InlineData("urn.cpro.gouv.fr:1p0:ereporting")]
    [InlineData("")]
    public void AnIdentifierThatNamesNoEditionSaysSo(string identifier)
    {
        En16931Edition.Of(new ProfileIdentifier(identifier)).ShouldBeNull();
    }

    [Fact]
    public void TheEditionThisLibraryImplementsIsTheOneItsArtefactsEncode()
    {
        En16931Edition.Implemented.ShouldBe(En16931Edition.Original);
        En16931Edition.Original.IsImplemented.ShouldBeTrue();
        En16931Edition.Revised.IsImplemented.ShouldBeFalse();
        En16931Edition.Revised.Identifier.Value.ShouldBe("urn:cen.eu:en16931:2026");
    }

    /// <summary>
    /// The distinction that matters: this is not an unknown profile, and telling the caller it is would send
    /// them looking for a registration they cannot make.
    /// </summary>
    [Fact]
    public void ANewerEditionIsReportedAsAnEditionRatherThanAsAnUnknownProfile()
    {
        var resolver = new ProfileResolver(new ProfileRegistry([KnownProfiles.En16931Ubl]));

        ProfileResolution resolution = resolver.Resolve(
            new ProfileIdentifier("urn:cen.eu:en16931:2026"),
            DocumentSyntax.Ubl);

        resolution.Outcome.ShouldBe(ProfileResolutionOutcome.FellBackFromUnsupported);
        resolution.Profile.ShouldBe(KnownProfiles.En16931Ubl);

        Diagnostic diagnostic = resolution.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Code.ShouldBe("EIV1044");
        diagnostic.BusinessTerm.ShouldBe("BT-24");
        diagnostic.Expected.ShouldBe("EN 16931-1:2017");
        diagnostic.Found.ShouldBe("EN 16931-1:2026");
        diagnostic.AppliedFallback!.ShouldContain("extension data");
    }

    /// <summary>A CIUS of a newer edition is the same situation, and must not be read as its own profile.</summary>
    [Fact]
    public void SoIsACiusBuiltOnOne()
    {
        var resolver = new ProfileResolver(new ProfileRegistry([KnownProfiles.En16931Ubl]));

        ProfileResolution resolution = resolver.Resolve(
            new ProfileIdentifier("urn:cen.eu:en16931:2026#compliant#urn:example:cius:1.0"),
            DocumentSyntax.Ubl);

        resolution.Diagnostics.ShouldHaveSingleItem().Code.ShouldBe("EIV1044");
        resolution.AllowsCompleteValidation.ShouldBeFalse();
    }

    /// <summary>And registering the newer edition yourself still wins, as with any other profile.</summary>
    [Fact]
    public void RegisteringTheNewerEditionYourselfStillWins()
    {
        var revised = new Profile(En16931Edition.Revised.Identifier, "EN 16931-1:2026", DocumentSyntax.Ubl);
        var resolver = new ProfileResolver(new ProfileRegistry([KnownProfiles.En16931Ubl, revised]));

        ProfileResolution resolution = resolver.Resolve(revised.Id, DocumentSyntax.Ubl);

        resolution.IsExact.ShouldBeTrue();
        resolution.Diagnostics.ShouldBeEmpty();
    }
}
