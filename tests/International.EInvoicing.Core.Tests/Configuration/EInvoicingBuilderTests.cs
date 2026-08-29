using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Profiles;
using International.EInvoicing.Xml;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Core.Tests.Configuration;

public class EInvoicingBuilderTests
{
    [Fact]
    public void TheDefaultSetupReadsWithBalancedDiagnosticsAndDefaultLimits()
    {
        ServiceProvider provider = new ServiceCollection().AddEInvoicing().BuildServiceProvider();

        var options = provider.GetRequiredService<EInvoicingOptions>();

        options.Limits.ShouldBe(DocumentLimits.Default);
        options.DiagnosticPolicy.Resolve(Diagnostic.Create(DiagnosticCodes.InvalidValue, "x", "a date"))
            .ShouldBe(DiagnosticAction.Keep);
    }

    [Fact]
    public void ACallerCanRegisterItsOwnProfileAndHaveItResolved()
    {
        var acme = new Profile(new ProfileIdentifier("urn:acme:profile:1p0"), "Acme", DocumentSyntax.Cii);

        ServiceProvider provider = new ServiceCollection()
            .AddEInvoicing(o => o.AddProfile(acme))
            .BuildServiceProvider();

        ProfileResolution resolution = provider.GetRequiredService<IProfileResolver>()
            .Resolve(acme.Id, DocumentSyntax.Cii);

        resolution.IsExact.ShouldBeTrue();
        resolution.Profile.ShouldBe(acme);
    }

    [Fact]
    public void DiagnosticOverridesAreAppliedInTheOrderTheyWereDeclared()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddEInvoicing(o => o
                .UseDiagnosticPreset(DiagnosticPreset.Balanced)
                .OnCategory(DiagnosticCategory.InvalidValue, DiagnosticAction.Escalate)
                .OnCode("EIV2001", DiagnosticAction.Suppress))
            .BuildServiceProvider();

        DiagnosticPolicy policy = provider.GetRequiredService<EInvoicingOptions>().DiagnosticPolicy;

        policy.Apply(Diagnostic.Create(DiagnosticCodes.InvalidValue, "x", "a date")).ShouldBeNull();
    }

    [Fact]
    public void LimitsCanBeTightenedForUntrustedTraffic()
    {
        var limits = new DocumentLimits { MaxDocumentCharacters = 1_000 };

        ServiceProvider provider = new ServiceCollection()
            .AddEInvoicing(o => o.UseLimits(limits))
            .BuildServiceProvider();

        provider.GetRequiredService<EInvoicingOptions>().Limits.MaxDocumentCharacters.ShouldBe(1_000);
    }

    [Fact]
    public void RegisteringTwiceDoesNotDuplicateOrReplaceTheFirstConfiguration()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddEInvoicing(o => o.UseLimits(new DocumentLimits { MaxElementDepth = 7 }))
            .AddEInvoicing()
            .BuildServiceProvider();

        provider.GetRequiredService<EInvoicingOptions>().Limits.MaxElementDepth.ShouldBe(7);
    }

    [Fact]
    public void AddEInvoicing_RejectsANullServiceCollection()
        => Should.Throw<ArgumentNullException>(() => ((IServiceCollection)null!).AddEInvoicing());
}
