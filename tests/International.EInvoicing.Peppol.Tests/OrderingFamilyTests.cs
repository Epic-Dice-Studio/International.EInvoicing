using System.Xml.Linq;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Xsd;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Peppol.Tests;

/// <summary>
/// The rest of the ordering family: the cancellation, and the advanced response.
/// </summary>
/// <remarks>
/// <para>
/// A cancellation withdraws an order and says why. The advanced response is the <em>same document</em> as
/// the ordinary order response — same root, same shape — under a profile that answers line by line; reading
/// it needed no new reader, only the profile registered and one reference it carries that the simple one
/// does not.
/// </para>
/// <para>
/// These documents are also where this library met the edge of the schemas it embeds: Peppol's advanced
/// ordering uses <c>cac:OrderChangeDocumentReference</c>, which UBL 2.1 does not define. So what is asserted
/// here is that a round trip introduces no error the original did not already have — which is the honest
/// claim, and a stronger one than "no errors" because it holds for documents the shipped schema cannot
/// fully judge.
/// </para>
/// </remarks>
public class OrderingFamilyTests
{
    private static readonly EInvoicing Library =
        EInvoicing.Create(builder => builder.AddDefaults().AddPeppol().AddUblSchema());

    public static TheoryData<string> Cancellations => Documents(PeppolPostAwardProfiles.OrderCancellation);

    public static TheoryData<string> AdvancedResponses =>
        Documents(PeppolPostAwardProfiles.OrderResponseAdvanced);

    [Theory]
    [MemberData(nameof(Cancellations))]
    public void EveryPublishedCancellationIsRead(string fileName)
    {
        DocumentResult result = Library.Read(ReadCorpusFile(fileName));

        result.Kind.ShouldBe(DocumentKind.UblOrderCancellation);
        OrderCancellation cancellation = result.RequireOrderCancellation();

        cancellation.Number.Value.ShouldNotBeNullOrWhiteSpace();
        cancellation.OrderReference.Value.ShouldNotBeNullOrWhiteSpace(
            "a cancellation that names no order cancels nothing");
        cancellation.Reason.Value.ShouldNotBeNullOrWhiteSpace(
            "a cancellation the seller cannot explain is one they will query rather than act on");

        Unmapped(result).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(AdvancedResponses))]
    public void AndEveryAdvancedResponse(string fileName)
    {
        DocumentResult result = Library.Read(ReadCorpusFile(fileName));

        result.Kind.ShouldBe(DocumentKind.UblOrderResponse);
        result.RequireOrderResponse().ResponseCode.Value.ShouldNotBeNullOrWhiteSpace();

        Unmapped(result).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Cancellations))]
    public void ACancellationComesBackWithTheSameElements(string fileName)
    {
        string xml = ReadCorpusFile(fileName);

        Census(Library.Write(Library.Read(xml).RequireOrderCancellation())).ShouldBe(Census(xml));
    }

    [Theory]
    [MemberData(nameof(AdvancedResponses))]
    public void AndSoDoesAnAdvancedResponse(string fileName)
    {
        string xml = ReadCorpusFile(fileName);

        Census(Library.Write(Library.Read(xml).RequireOrderResponse())).ShouldBe(Census(xml));
    }

    /// <summary>
    /// A round trip introduces no schema error the document did not already have.
    /// </summary>
    /// <remarks>
    /// Peppol's advanced ordering uses elements UBL 2.1 does not define, so some of these documents do not
    /// validate as published. Asserting "no errors" would have meant either excluding them or pretending;
    /// asserting that we do not make a document worse is true of all of them and is what a caller actually
    /// needs to know.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Cancellations))]
    [MemberData(nameof(AdvancedResponses))]
    public void AndNeitherIsMadeWorseByBeingWrittenBack(string fileName)
    {
        string xml = ReadCorpusFile(fileName);
        DocumentResult read = Library.Read(xml);

        string written = read.Kind == DocumentKind.UblOrderCancellation
            ? Library.Write(read.RequireOrderCancellation())
            : Library.Write(read.RequireOrderResponse());

        Errors(Library.Validate(written)).ShouldBe(Errors(Library.Validate(xml)));
    }

    /// <summary>
    /// The advanced response is the order response under another profile, and needs no reader of its own.
    /// </summary>
    [Fact]
    public void TheAdvancedResponseIsTheSameDocumentUnderAnotherProfile()
    {
        PeppolPostAwardProfiles.OrderResponseAdvanced.Syntax
            .ShouldBe(PeppolPostAwardProfiles.OrderResponse.Syntax);

        Corpus(PeppolPostAwardProfiles.OrderResponseAdvanced)
            .Select(path => Library.Read(File.ReadAllText(path)).Kind)
            .ShouldAllBe(kind => kind == DocumentKind.UblOrderResponse);
    }

    /// <summary>Which version of the order the seller answered, when the buyer has amended it.</summary>
    [Fact]
    public void AndSaysWhichVersionOfTheOrderItAnswers()
    {
        Assert.SkipWhen(
            !Corpus(PeppolPostAwardProfiles.OrderResponseAdvanced).Any(),
            "The POACC artefacts are not present; run build/fetch-specs.sh poacc.");

        Corpus(PeppolPostAwardProfiles.OrderResponseAdvanced)
            .Select(path => Library.Read(File.ReadAllText(path)).RequireOrderResponse())
            .ShouldContain(response => response.OrderChangeReference.IsSet);
    }

    private static IEnumerable<string> Unmapped(DocumentResult result) =>
        result.Diagnostics
            .Where(diagnostic => diagnostic.Code == UblDiagnostics.UnmappedElement.Code)
            .Select(diagnostic => diagnostic.Found!);

    private static IReadOnlyList<string> Errors(ValidationReport report) =>
        [.. report.Errors.Select(error => error.Message).Order()];

    private static TheoryData<string> Documents(Profile profile)
    {
        var data = new TheoryData<string>();

        foreach (string path in Corpus(profile))
        {
            data.Add(Path.GetFileName(path));
        }

        return data.Count == 0 ? new TheoryData<string> { "(none fetched)" } : data;
    }

    private static string ReadCorpusFile(string fileName)
    {
        string? path = AllDocuments().FirstOrDefault(candidate => Path.GetFileName(candidate) == fileName);

        Assert.SkipWhen(
            path is null,
            "The POACC artefacts are not present; run build/fetch-specs.sh poacc.");

        return File.ReadAllText(path!);
    }

    private static IEnumerable<string> Corpus(Profile profile) =>
        AllDocuments().Where(path =>
            XDocument.Load(path).Root?.Element(UblNames.Cbc + "CustomizationID")?.Value.Trim()
            == profile.Id.Value);

    private static IEnumerable<string> AllDocuments()
    {
        string root = Path.Combine(RepositoryRoot(), "specs", "peppol", "poacc", "examples");

        return Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "*.xml", SearchOption.AllDirectories).Order()
            : [];
    }

    private static IReadOnlyList<string> Census(string xml) =>
        [.. XDocument.Parse(xml).Descendants()
            .GroupBy(element => element.Name.ToString())
            .Select(group => $"{group.Key}={group.Count()}")
            .Order()];

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "International.EInvoicing.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("repository root not found");
    }
}
