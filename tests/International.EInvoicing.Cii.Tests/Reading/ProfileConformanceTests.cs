using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Cii.Tests.Reading;

/// <summary>
/// Pins the profile catalogue against the identifiers that actually appear in the official corpus. CII and
/// UBL share XRechnung's identifiers, which is why the profile is registered for both syntaxes.
/// </summary>
public class ProfileConformanceTests
{
    private static CiiInvoiceReader Reader() =>
        new(new EInvoicingOptions(), new ProfileResolver(new ProfileRegistry(KnownProfiles.All)));

    [Fact]
    public void TheXRechnungCiusResolvesExactlyInCiiToo()
    {
        EInvoice invoice = Reader().Read(GoldenCorpus.Read("01.01a-INVOICE_uncefact.xml")).Value!;

        invoice.Profile!.IsExact.ShouldBeTrue();
        invoice.Profile.Profile!.Syntax.ShouldBe(DocumentSyntax.Cii);
        invoice.Profile.Profile.Name.ShouldBe("XRechnung 3.0 (CIUS)");
    }

    [Theory]
    [MemberData(nameof(GoldenCorpus.CiiInvoiceCases), MemberType = typeof(GoldenCorpus))]
    public void AProfileIsEitherResolvedExactlyOrTheDowngradeIsReported(string fileName)
    {
        ParseResult<EInvoice> result = Reader().Read(GoldenCorpus.Read(fileName));
        ProfileResolution resolution = result.Value!.Profile!;

        if (resolution.IsExact)
        {
            resolution.AllowsCompleteValidation.ShouldBeTrue();
            return;
        }

        Diagnostic downgrade = result.Diagnostics
            .First(d => d.Category is DiagnosticCategory.UnknownProfile or DiagnosticCategory.UnsupportedProfile);

        downgrade.BusinessTerm.ShouldBe("BT-24");
        downgrade.AppliedFallback.ShouldNotBeNullOrEmpty();
        resolution.AllowsCompleteValidation.ShouldBeFalse();
    }
}
