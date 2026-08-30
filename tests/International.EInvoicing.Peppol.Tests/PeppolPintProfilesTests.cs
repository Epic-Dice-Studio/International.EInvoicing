using International.EInvoicing.Building;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Peppol.Tests;

/// <summary>
/// The PINT jurisdiction identifiers, checked against the artefacts they were taken from.
/// </summary>
/// <remarks>
/// A specification identifier is the one string that must be exactly right: a wrong one makes every document
/// written with it wrong, and makes documents that should be read look unknown. So none of these is
/// transcribed from prose — each is compared against the published rule artefact for its jurisdiction, which
/// <c>build/fetch-specs.sh pint</c> puts on disk.
/// </remarks>
public class PeppolPintProfilesTests
{
    public static TheoryData<string, string> Jurisdictions
    {
        get
        {
            var data = new TheoryData<string, string>();

            foreach ((string folder, string identifier) in PeppolPintProfiles.ArtefactFolders)
            {
                data.Add(folder, identifier);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Jurisdictions))]
    public void EveryIdentifierAppearsInTheArtefactItCameFrom(string folder, string identifier)
    {
        string root = Path.Combine(RepositoryRoot(), "specs", "peppol", "pint", "schematron", folder);

        Assert.SkipWhen(
            !Directory.Exists(root),
            "The PINT artefacts are not present; run build/fetch-specs.sh pint.");

        string[] rules = Directory
            .EnumerateFiles(root, "PINT-jurisdiction-aligned-rules.xslt", SearchOption.AllDirectories)
            .ToArray();

        rules.ShouldNotBeEmpty($"no jurisdiction rules were found under {folder}");

        rules.ShouldContain(
            path => File.ReadAllText(path).Contains(identifier, StringComparison.Ordinal),
            $"none of the {rules.Length} artefact(s) under {folder} mentions '{identifier}'");
    }

    /// <summary>
    /// PINT and BIS Billing are two families, and the difference that catches people is the process
    /// identifier: they are not the same string, and each family's rules reject the other's.
    /// </summary>
    [Fact]
    public void ThePintProcessIsNotTheBisProcess()
    {
        PeppolBusinessProcess.PintBilling.ShouldNotBe(PeppolBusinessProcess.Billing);
        PeppolBusinessProcess.PintBilling.ShouldBe("urn:peppol:bis:billing");

        EInvoice pint = EInvoiceBuilder.Create(PeppolPintProfiles.BillingJp).ForPeppolPint().Build();
        EInvoice bis = EInvoiceBuilder.Create(PeppolProfiles.BillingUbl).ForPeppol().Build();

        pint.BusinessProcessType.Value.ShouldBe(PeppolBusinessProcess.PintBilling);
        bis.BusinessProcessType.Value.ShouldBe(PeppolBusinessProcess.Billing);
    }

    [Fact]
    public void AJurisdictionIsFoundByItsCountryCode()
    {
        PeppolPintProfiles.ForJurisdiction("jp").ShouldBe(PeppolPintProfiles.BillingJp);
        PeppolPintProfiles.ForJurisdiction("AU").ShouldBe(PeppolPintProfiles.BillingAuNz);
        PeppolPintProfiles.ForJurisdiction("NZ").ShouldBe(PeppolPintProfiles.BillingAuNz);
        PeppolPintProfiles.ForJurisdiction("FR").ShouldBeNull();
        PeppolPintProfiles.ForJurisdiction(null).ShouldBeNull();
    }

    /// <summary>PINT is carried in UBL; OpenPEPPOL publishes no CII binding for it.</summary>
    [Fact]
    public void EveryPintProfileIsUbl() =>
        PeppolPintProfiles.All.ShouldAllBe(profile => profile.Syntax == DocumentSyntax.Ubl);

    [Fact]
    public void ThePintIdentifiersAreNotTheBisOnes() =>
        PeppolPintProfiles.All.ShouldAllBe(profile => profile.Id.Value.StartsWith("urn:peppol:pint:", StringComparison.Ordinal));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "International.EInvoicing.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
    }
}
