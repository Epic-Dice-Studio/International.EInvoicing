using International.EInvoicing.Cdar.Model;
using International.EInvoicing.Cdar.Reading;
using International.EInvoicing.Cdar.Writing;
using International.EInvoicing.Configuration;
using International.EInvoicing.Countries.France.Lifecycle;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.France.Tests;

/// <summary>
/// Measures the French lifecycle builder against the DGFiP's own rules and sample messages.
/// </summary>
/// <remarks>
/// The artefacts are fetched, not redistributed — <c>build/fetch-specs.sh france</c> — so these tests measure
/// what is present and stay quiet about what is not, rather than failing a checkout that has not fetched
/// them. What they prove is the thing that matters: a message this library builds is one the published rules
/// accept.
/// </remarks>
public class FrCdarConformanceTests
{
    private static readonly DateTimeOffset Moment = new(2025, 7, 1, 15, 10, 0, TimeSpan.Zero);

    /// <summary>Every status, and who reports it. A collection is reported by the seller, the rest by the buyer.</summary>
    public static TheoryData<string, string> Statuses
    {
        get
        {
            var data = new TheoryData<string, string>();

            foreach (FrLifecycleStatus status in FrLifecycleStatus.All)
            {
                data.Add(status.Code, "partner");
                data.Add(status.Code, "portal");
            }

            return data;
        }
    }

    public static TheoryData<string> Samples
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (string path in SamplePaths())
            {
                data.Add(path);
            }

            // A theory with no cases fails discovery, so an absent corpus is one case that skips itself.
            if (data.Count == 0)
            {
                data.Add(string.Empty);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public void EveryDgfipSampleSatisfiesTheFrenchLifecycleRules(string path)
    {
        Assert.SkipWhen(path.Length == 0, "The DGFiP samples are not present; run build/fetch-specs.sh france.");

        ValidationReport report = Validate(File.ReadAllText(path));

        report.IsValid.ShouldBeTrue(Describe(Path.GetFileName(path), report));
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public void NoRuleIsLeftUnevaluableOnASample(string path)
    {
        Assert.SkipWhen(path.Length == 0, "The DGFiP samples are not present; run build/fetch-specs.sh france.");

        ValidationReport report = Validate(File.ReadAllText(path));

        report.Messages
            .Where(message => message.Message.StartsWith("This rule could not be evaluated", StringComparison.Ordinal))
            .ShouldBeEmpty($"{Path.GetFileName(path)} left rules unevaluated");
    }

    [Theory]
    [MemberData(nameof(Statuses))]
    public void EveryStatusThisLibraryBuildsSatisfiesTheFrenchRules(string statusCode, string route)
    {
        FrLifecycleStatus status = FrLifecycleStatus.FromCode(statusCode)!;

        ValidationReport report = Validate(new CdarWriter().WriteToString(Build(status, route)));

        report.IsValid.ShouldBeTrue(Describe($"{status} to {route}", report));
    }

    /// <summary>
    /// A rule set that accepts everything proves nothing. An acknowledgement type outside the two the DGFiP
    /// allows must be caught.
    /// </summary>
    [Fact]
    public void TheRulesRejectAMessageTheyShould()
    {
        Assert.SkipWhen(!SamplePaths().Any(), "The DGFiP samples are not present.");

        string broken = File.ReadAllText(SamplePaths().First())
            .Replace("<ram:TypeCode>305</ram:TypeCode>", "<ram:TypeCode>999</ram:TypeCode>", StringComparison.Ordinal);

        ValidationReport report = Validate(broken);

        report.IsValid.ShouldBeFalse();
        report.Messages.Select(message => message.RuleIdentifier).ShouldContain("BR-FR-CDV-09_MDT-77");
    }

    /// <summary>
    /// A business status is reported by a trading party. Naming the sending platform instead produces a
    /// message that is rejected, so the builder says so before one is written.
    /// </summary>
    [Fact]
    public void ABusinessStatusWithoutAnIssuingPartyIsRefusedWithAnExplanation()
    {
        InvalidOperationException thrown = Should.Throw<InvalidOperationException>(() => FrCdar
            .ToPublicPortal()
            .From(from => from.Platform("0003", "PA-E Vendeur"))
            .About("F202500003", new DateOnly(2025, 7, 1))
            .Approved(Moment));

        thrown.Message.ShouldContain("IssuedBy");
        thrown.Message.ShouldContain("205");
    }

    /// <summary>
    /// Reading a published message and writing it back must not cost it its conformance — the status detail
    /// above all, which carries the reason, the requested action and the amounts.
    /// </summary>
    [Theory]
    [MemberData(nameof(Samples))]
    public void ASampleSurvivesBeingReadAndWrittenBack(string path)
    {
        Assert.SkipWhen(path.Length == 0, "The DGFiP samples are not present; run build/fetch-specs.sh france.");

        var reader = new CdarReader(
            new EInvoicingOptions(),
            new ProfileResolver(new ProfileRegistry(FrProfiles.All)));

        LifecycleStatusMessage read = reader.Read(File.ReadAllText(path)).Value!;
        string written = new CdarWriter().WriteToString(read);

        ValidationReport report = Validate(written);

        report.IsValid.ShouldBeTrue(Describe($"{Path.GetFileName(path)} after a round trip", report));
    }

    /// <summary>The detail behind a status is read into the model, not left as unmapped extension data.</summary>
    [Fact]
    public void AStatusDetailIsReadIntoTheModel()
    {
        string? path = SamplePaths().FirstOrDefault(sample => sample.Contains("En_litige", StringComparison.Ordinal));

        Assert.SkipWhen(path is null, "The DGFiP samples are not present; run build/fetch-specs.sh france.");

        var reader = new CdarReader(
            new EInvoicingOptions(),
            new ProfileResolver(new ProfileRegistry(FrProfiles.All)));

        DocumentStatusDetail detail = reader.Read(File.ReadAllText(path!)).Value!
            .References.ShouldHaveSingleItem()
            .StatusDetails.ShouldHaveSingleItem();

        detail.ReasonCode.Value.ShouldBe(FrStatusReason.VatRateWrong);
        detail.RequestedActionCode.Value.ShouldBe(FrRequestedAction.CorrectiveInvoice);
        detail.SequenceNumber.Value.ShouldBe(1);
        detail.Characteristics.Count.ShouldBe(2);
        detail.Characteristics[0].Identifier.Value.ShouldBe("BT-152");
        detail.Characteristics[0].TypeCode.Value.ShouldBe(FrStatusValueType.DocumentValue);
        detail.Characteristics[0].ValuePercent.Value.ShouldBe(10.00m);
        detail.Characteristics[1].TypeCode.Value.ShouldBe(FrStatusValueType.ExpectedValue);
        detail.Characteristics[1].ValueChanged.Value.ShouldBe(true);
    }

    [Fact]
    public void ACollectionMustSayHowMuchWasCollected()
    {
        Should.Throw<ArgumentException>(() => FrCdar
            .ToPublicPortal()
            .From(from => from.Platform("0003", "PA-E Vendeur"))
            .IssuedBySeller("100000009")
            .About("F202500003", new DateOnly(2025, 7, 1))
            .Collected([]));
    }

    private static LifecycleStatusMessage Build(FrLifecycleStatus status, string route)
    {
        FrCdar builder = route == "partner"
            ? FrCdar.ToPartner(to => to
                .Company("100000009")
                .Named("VENDEUR")
                .AsSeller()
                .ReachableAt("100000009_STATUTS"))
            : FrCdar.ToPublicPortal();

        builder = builder
            .From(from => from.Platform("0003", "PA-E Vendeur"))
            .About("F202500003", new DateOnly(2025, 7, 1));

        if (status == FrLifecycleStatus.Collected)
        {
            return builder
                .IssuedBySeller("100000009", "VENDEUR")
                .Collected(new FrCollectedAmount(12000m, 20m), Moment);
        }

        if (status.IsBusinessEvent)
        {
            builder = builder.IssuedByBuyer("200000008", "ACHETEUR");
        }

        if (!status.RequiresReason)
        {
            return builder.With(status, Moment);
        }

        string reasonCode = FrStatusReason.AllowedFor(status)[0];

        if (status == FrLifecycleStatus.Disputed)
        {
            return builder.Disputed(
                reasonCode,
                "Motif",
                Moment,
                FrRequestedAction.CorrectiveInvoice,
                "Créer une facture rectificative");
        }

        return status == FrLifecycleStatus.Refused
            ? builder.Refused(reasonCode, "Motif", Moment)
            : builder.Rejected(reasonCode, "Motif", Moment);
    }

    private static ValidationReport Validate(string xml) =>
        new SchematronValidator().Validate(xml, Rules());

    private static SchematronRuleSet Rules()
    {
        string directory = Path.Combine(Specs, "rules");
        string? path = Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*CDAR*.sch", SearchOption.AllDirectories).FirstOrDefault()
            : null;

        Assert.SkipWhen(path is null, "The French artefacts are not present; run build/fetch-specs.sh france.");

        return SchematronRuleSet.Load(File.ReadAllText(path!), "BR-FR-CDV (CDAR)", "1.4.0.03");
    }

    private static IEnumerable<string> SamplePaths()
    {
        string directory = Path.Combine(Specs, "samples");

        return Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*.xml", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal)
            : [];
    }

    private static string Specs => Path.Combine(RepositoryRoot(), "specs", "fr-dse");

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private static string Describe(string what, ValidationReport report) =>
        $"{what} was rejected:{Environment.NewLine}"
        + string.Join(
            Environment.NewLine,
            report.Messages.Select(message => $"  {message.RuleIdentifier}: {message.Message}"));
}
