using International.EInvoicing.Cii;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.Xsd.Tests;

/// <summary>
/// The CII half of the same net, and what it caught.
/// </summary>
/// <remarks>
/// <para>
/// Order is normative in CII as it is in UBL, and the same exercise found the same kind of defect: seven of
/// the fifteen official examples came back from a read-then-write in a shape the schema refuses, every one
/// of them because a term was unmapped and kept as extension data. BT-7 read from the wrong place, BT-71
/// written as a GLN, BT-111, BT-128, the basis quantity stated on both prices, the tax scheme on a
/// document-level allowance, the type code of a supporting document.
/// </para>
/// <para>
/// It also found one shape defect of our own making: <c>SellerOrderReferencedDocument</c> was written after
/// <c>BuyerOrderReferencedDocument</c>, and the schema declares them the other way round — BT-14 before
/// BT-13, whatever the numbering suggests.
/// </para>
/// </remarks>
public class CiiSchemaTests
{
    private static readonly CiiSchemaRuleSet Schema = new();

    [Fact]
    public void TheSchemasLoadAndJudgeCiiOnly()
    {
        Schema.AppliesTo(DocumentSyntax.Cii, KnownProfiles.En16931Cii.Id).ShouldBeTrue();
        Schema.AppliesTo(DocumentSyntax.Ubl, KnownProfiles.En16931Ubl.Id).ShouldBeFalse();
        Schema.Version.ShouldBe("D22B");
    }

    /// <summary>
    /// Every official example, read and written back, keeps its shape and leaves nothing unmapped.
    /// </summary>
    /// <remarks>
    /// Two of the examples are schema-invalid **as published** — they carry allowance reason codes outside
    /// the D22B enumeration — so what is asserted is that this library adds nothing: the rewrite is refused
    /// for the same reasons as the source and no others.
    /// </remarks>
    [Theory]
    [MemberData(nameof(OfficialExamples))]
    public void EveryOfficialExampleSurvivesTheRoundTripWithItsShapeIntact(string path)
    {
        EInvoicing library = EInvoicing.Create(builder => builder.AddDefaults());

        string source = File.ReadAllText(path);
        DocumentResult read = library.Read(source);

        Assert.SkipWhen(read.Invoice is null, $"not an invoice this library reads: {Path.GetFileName(path)}");

        string[] written = [.. Failures(Schema.Validate(library.Write(read.Invoice!, DocumentFormat.Cii)))];

        written.ShouldBe(
            Failures(Schema.Validate(source)),
            ignoreOrder: true,
            $"{Path.GetFileName(path)} came back with shape defects the source did not have:"
            + Environment.NewLine + string.Join(Environment.NewLine, written));

        read.Diagnostics
            .Where(diagnostic => diagnostic.Code == CiiDiagnostics.UnmappedElement.Code)
            .Select(diagnostic => diagnostic.Found)
            .ShouldBeEmpty($"{Path.GetFileName(path)} carries terms this library does not map");
    }

    private static IEnumerable<string> Failures(ValidationReport report) =>
        report.Errors.Select(message => message.Message).Distinct().Order(StringComparer.Ordinal);

    public static TheoryData<string> OfficialExamples()
    {
        var data = new TheoryData<string>();
        string directory = Path.Combine(RepositoryRoot(), "specs", "en16931", "cii", "examples");

        if (!Directory.Exists(directory))
        {
            return data;
        }

        foreach (string path in Directory.EnumerateFiles(directory, "*.xml").Order(StringComparer.Ordinal))
        {
            data.Add(path);
        }

        return data;
    }

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
