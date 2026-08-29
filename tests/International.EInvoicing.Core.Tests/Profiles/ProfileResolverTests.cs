using International.EInvoicing.Diagnostics;
using International.EInvoicing.Profiles;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Core.Tests.Profiles;

/// <summary>
/// The fallback chain is the promise that an unknown profile costs the caller information, never the whole
/// document — and that the cost is always reported.
/// </summary>
public class ProfileResolverTests
{
    private static ProfileResolver ResolverWith(params Profile[] supported) =>
        new(new ProfileRegistry(supported));

    [Fact]
    public void ARegisteredProfileIsUsedAsIsAndReportsNothing()
    {
        ProfileResolution resolution = ResolverWith(KnownProfiles.FacturXBasic)
            .Resolve(KnownProfiles.FacturXBasic.Id, DocumentSyntax.Cii);

        resolution.IsExact.ShouldBeTrue();
        resolution.Outcome.ShouldBe(ProfileResolutionOutcome.Exact);
        resolution.Diagnostics.ShouldBeEmpty();
        resolution.AllowsCompleteValidation.ShouldBeTrue();
    }

    [Fact]
    public void AKnownButUnimplementedProfileFallsBackToItsParentAndSaysSo()
    {
        ProfileResolution resolution = ResolverWith(KnownProfiles.En16931Cii)
            .Resolve(KnownProfiles.FacturXExtended.Id, DocumentSyntax.Cii);

        resolution.Outcome.ShouldBe(ProfileResolutionOutcome.FellBackFromUnsupported);
        resolution.Profile.ShouldBe(KnownProfiles.En16931Cii);

        Diagnostic diagnostic = resolution.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Code.ShouldBe("EIV1043");
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
        diagnostic.BusinessTerm.ShouldBe("BT-24");
        diagnostic.AppliedFallback.ShouldBe("read as EN 16931");
    }

    [Fact]
    public void AnUnrecognisedProfileIsReportedDifferentlyFromAnUnsupportedOne()
    {
        ProfileResolution resolution = ResolverWith(KnownProfiles.En16931Cii)
            .Resolve(new ProfileIdentifier("urn:acme:profile:2p0"), DocumentSyntax.Cii);

        resolution.Outcome.ShouldBe(ProfileResolutionOutcome.FellBackFromUnknown);

        Diagnostic diagnostic = resolution.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Code.ShouldBe("EIV1042");
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
        diagnostic.Found.ShouldBe("urn:acme:profile:2p0");
    }

    [Fact]
    public void WithNothingRegisteredTheDocumentIsStillReadGenerically()
    {
        ProfileResolution resolution = ResolverWith()
            .Resolve(new ProfileIdentifier("urn:acme:profile:2p0"), DocumentSyntax.Cii);

        resolution.Profile.ShouldBeNull();
        resolution.AllowsCompleteValidation.ShouldBeFalse();
        resolution.Diagnostics.ShouldHaveSingleItem().AppliedFallback
            .ShouldBe("generic cii reading; no profile rules applied");
    }

    [Fact]
    public void TheChainSkipsAncestorsThatAreNotRegisteredEither()
    {
        // BASIC WL restricts MINIMUM; only MINIMUM is implemented here.
        ProfileResolution resolution = ResolverWith(KnownProfiles.FacturXMinimum)
            .Resolve(KnownProfiles.FacturXBasicWl.Id, DocumentSyntax.Cii);

        resolution.Profile.ShouldBe(KnownProfiles.FacturXMinimum);
        resolution.Outcome.ShouldBe(ProfileResolutionOutcome.FellBackFromUnsupported);
    }

    [Fact]
    public void ADocumentDeclaringNoProfileIsReadWithTheBaselineAndNotBlamed()
    {
        ProfileResolution resolution = ResolverWith(KnownProfiles.En16931Cii)
            .Resolve(ProfileIdentifier.None, DocumentSyntax.Cii);

        resolution.Outcome.ShouldBe(ProfileResolutionOutcome.Undeclared);
        resolution.Profile.ShouldBe(KnownProfiles.En16931Cii);
        resolution.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void ACallerProfileTakesPrecedenceOverTheSameIdentifierShippedByTheLibrary()
    {
        var mine = new Profile(KnownProfiles.FacturXBasic.Id, "Acme BASIC", DocumentSyntax.Cii);
        var registry = new ProfileRegistry([KnownProfiles.FacturXBasic]);
        registry.Register(mine);

        new ProfileResolver(registry)
            .Resolve(KnownProfiles.FacturXBasic.Id, DocumentSyntax.Cii)
            .Profile!.Name.ShouldBe("Acme BASIC");
    }

    [Fact]
    public void ProfilesAreScopedToTheirSyntax()
    {
        var registry = new ProfileRegistry([KnownProfiles.PeppolBisBilling3Ubl]);

        registry.IsSupported(KnownProfiles.PeppolBisBilling3Ubl.Id, DocumentSyntax.Ubl).ShouldBeTrue();
        registry.IsSupported(KnownProfiles.PeppolBisBilling3Ubl.Id, DocumentSyntax.Cii).ShouldBeFalse();
    }
}
