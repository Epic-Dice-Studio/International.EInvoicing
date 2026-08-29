using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl.Reading;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Ubl.Tests.Reading;

/// <summary>
/// KnownProfiles is transcribed from published specifications, and transcriptions rot. These tests pin it
/// against the identifiers that actually appear in the official corpus — which is how the XRechnung 3.0
/// identifiers were found to be wrong in the first place.
/// </summary>
public class ProfileConformanceTests
{
    private static UblInvoiceReader Reader() =>
        new(new EInvoicingOptions(), new ProfileResolver(new ProfileRegistry(KnownProfiles.All)));

    [Fact]
    public void TheXRechnungCiusIdentifierMatchesTheOfficialCorpus()
    {
        EInvoice invoice = Reader().Read(GoldenCorpus.Read("01.01a-INVOICE_ubl.xml")).Value!;

        invoice.SpecificationIdentifier.Value
            .ShouldBe("urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0");
        invoice.Profile!.IsExact.ShouldBeTrue();
        invoice.Profile.Profile!.Name.ShouldBe("XRechnung 3.0 (CIUS)");
    }

    [Theory]
    [MemberData(nameof(GoldenCorpus.UblInvoiceCases), MemberType = typeof(GoldenCorpus))]
    public void AProfileIsEitherResolvedExactlyOrTheDowngradeIsReported(string fileName)
    {
        ParseResult<EInvoice> result = Reader().Read(GoldenCorpus.Read(fileName));
        ProfileResolution resolution = result.Value!.Profile!;

        if (resolution.IsExact)
        {
            resolution.AllowsCompleteValidation.ShouldBeTrue();
            return;
        }

        // Not exact: the caller must be able to see it, and to see what was used instead.
        Diagnostic downgrade = result.Diagnostics
            .First(d => d.Category is DiagnosticCategory.UnknownProfile or DiagnosticCategory.UnsupportedProfile);

        downgrade.BusinessTerm.ShouldBe("BT-24");
        downgrade.AppliedFallback.ShouldNotBeNullOrEmpty();
        resolution.AllowsCompleteValidation.ShouldBeFalse();
    }

    [Fact]
    public void TheMajorityOfTheCorpusResolvesExactly()
    {
        int exact = GoldenCorpus.UblInvoicePaths.Count(path =>
            Reader().Read(File.ReadAllText(path)).Value!.Profile!.IsExact);

        exact.ShouldBeGreaterThan(GoldenCorpus.UblInvoicePaths.Count / 2);
    }
}
