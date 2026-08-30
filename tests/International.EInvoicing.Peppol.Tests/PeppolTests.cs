using International.EInvoicing.Configuration;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Peppol.Tests;

/// <summary>
/// Peppol is what a dozen countries exchange over, so what it gets wrong, they all get wrong.
/// </summary>
public class PeppolTests
{
    /// <summary>
    /// The code list is taken from the EN 16931 artefacts this library ships, not transcribed — so this
    /// checks it against the artefact rather than against a second transcription.
    /// </summary>
    [Fact]
    public void TheSchemeListMatchesTheArtefactItWasTakenFrom()
    {
        string codes = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "specs", "en16931", "ubl", "schematron", "codelist", "EN16931-UBL-codes.sch"));

        int rule = codes.IndexOf("id=\"BR-CL-25\"", StringComparison.Ordinal);
        rule.ShouldBeGreaterThan(0);

        string test = codes[..rule];
        int opened = test.LastIndexOf("contains('", StringComparison.Ordinal) + "contains('".Length;
        string[] published = test[opened..test.IndexOf('\'', opened)].Split(' ', StringSplitOptions.RemoveEmptyEntries);

        PeppolEndpointScheme.All.ShouldBe(published, ignoreOrder: false);
    }

    [Theory]
    [InlineData("0208", true)]     // a Belgian enterprise number
    [InlineData("0088", true)]     // a GLN
    [InlineData("9925", true)]     // Belgian VAT, one of the 99xx schemes
    [InlineData("0238", false)]    // French platforms — used in lifecycle messages, not in this list
    [InlineData("1234", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ASchemeIsKnownOrItIsNot(string? code, bool expected) =>
        PeppolEndpointScheme.IsKnown(code).ShouldBe(expected);

    /// <summary>An unusable scheme should say what to do about it, not just that it is unusable.</summary>
    [Fact]
    public void AnUnknownSchemeSaysWhichRuleWouldRejectIt()
    {
        ArgumentException thrown = Should.Throw<ArgumentException>(() => PeppolEndpointScheme.Require("1234"));

        thrown.Message.ShouldContain("BR-CL-25");
        thrown.Message.ShouldContain(PeppolEndpointScheme.ArtefactVersion);
    }

    [Theory]
    [InlineData("0208:0203201340", "0208", "0203201340")]
    [InlineData("iso6523-actorid-upis::0208:0203201340", "0208", "0203201340")]
    [InlineData("  0192:991825827  ", "0192", "991825827")]
    public void AParticipantIsReadWhicheverWayItIsWritten(string text, string scheme, string value)
    {
        PeppolParticipant participant = PeppolParticipant.Parse(text);

        participant.Scheme.ShouldBe(scheme);
        participant.Value.ShouldBe(value);
        participant.HasKnownScheme.ShouldBeTrue();
        participant.ToString().ShouldBe($"{scheme}:{value}");
        participant.ToQualifiedString().ShouldBe($"iso6523-actorid-upis::{scheme}:{value}");
    }

    [Theory]
    [InlineData("0208")]
    [InlineData(":0203201340")]
    [InlineData("0208:")]
    [InlineData("")]
    public void SomethingThatIsNotAParticipantSaysSo(string text)
    {
        PeppolParticipant.TryParse(text, out _).ShouldBeFalse();
        Should.Throw<FormatException>(() => PeppolParticipant.Parse(text));
    }

    /// <summary>A shape the network would accept, carrying a scheme this version does not know.</summary>
    [Fact]
    public void AParticipantCanBeWellShapedAndStillUnusable()
    {
        PeppolParticipant.TryParse("1234:anything", out PeppolParticipant participant).ShouldBeTrue();

        participant.HasKnownScheme.ShouldBeFalse();
        PeppolParticipant.IsValid("1234:anything").ShouldBeFalse();
        Should.Throw<ArgumentException>(() => PeppolParticipant.Create("1234", "anything"));
    }

    [Fact]
    public void AParticipantBecomesTheElectronicAddressAnInvoiceCarries()
    {
        Values.IdentifierField address = PeppolParticipant
            .Create(PeppolEndpointScheme.BelgianEnterprise, "0203201340")
            .ToElectronicAddress();

        address.Value.ShouldBe("0203201340");
        address.SchemeId.ShouldBe("0208");
    }

    [Fact]
    public void AddingPeppolBringsItsProfilesAndBothSyntaxes()
    {
        var builder = new EInvoicingBuilder();
        builder.AddPeppol();

        ProfileRegistry registry = builder.BuildRegistry();

        registry.IsSupported(PeppolProfiles.BillingUbl.Id, DocumentSyntax.Ubl).ShouldBeTrue();
        registry.IsSupported(PeppolProfiles.BillingCii.Id, DocumentSyntax.Cii).ShouldBeTrue();
        registry.IsSupported(KnownProfiles.En16931Ubl.Id, DocumentSyntax.Ubl).ShouldBeTrue();
    }

    /// <summary>
    /// Both of Peppol's rule sets apply to a Peppol document: its own, and its copy of the EN 16931 ones.
    /// Loading only the first is the false pass this method exists to prevent.
    /// </summary>
    [Fact]
    public void EveryRuleSetInTheFolderIsLoaded()
    {
        string directory = Path.Combine(RepositoryRoot(), "specs", "peppol", "rules");

        Assert.SkipWhen(
            !Directory.Exists(directory),
            "The Peppol artefacts are not present; run build/fetch-specs.sh peppol.");

        var builder = new EInvoicingBuilder();
        builder.AddPeppol().AddPeppolRulesFrom(directory, "3.0.20");

        IReadOnlyList<IDocumentRuleSet> ruleSets = builder.BuildRuleSets();

        ruleSets.Count.ShouldBe(4);
        ruleSets.ShouldContain(rules => rules.Name == "PEPPOL-EN16931-UBL");
        ruleSets.ShouldContain(rules => rules.Name == "CEN-EN16931-UBL");
        ruleSets.ShouldAllBe(rules => rules.Version == "3.0.20");
    }

    [Fact]
    public void AMissingFolderSaysWhereTheArtefactsComeFrom()
    {
        DirectoryNotFoundException thrown = Should.Throw<DirectoryNotFoundException>(
            () => new EInvoicingBuilder().AddPeppolRulesFrom("no/such/place"));

        thrown.Message.ShouldContain("fetch-specs.sh peppol");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
