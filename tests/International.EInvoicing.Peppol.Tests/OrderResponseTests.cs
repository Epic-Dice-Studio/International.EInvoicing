using System.Xml.Linq;
using International.EInvoicing.Model;
using International.EInvoicing.Ubl;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Xsd;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Peppol.Tests;

/// <summary>
/// The seller's answer to an order: accepted, rejected, or accepted on different terms.
/// </summary>
/// <remarks>
/// Without it a buyer who has sent an order knows nothing until goods arrive or do not — the pre-award twin
/// of the gap the Invoice Response closes. What makes it more than a yes or no is that a seller may accept a
/// line on other terms: a different quantity, a later date, or a substitute product, each of which the buyer
/// needs to see before the goods turn up rather than after.
/// </remarks>
public class OrderResponseTests
{
    private static readonly EInvoicing Library =
        EInvoicing.Create(builder => builder.AddDefaults().AddPeppol().AddUblSchema());

    public static TheoryData<string> PublishedDocuments
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (string path in Corpus())
            {
                data.Add(Path.GetFileName(path));
            }

            return data.Count == 0 ? new TheoryData<string> { "(none fetched)" } : data;
        }
    }

    [Theory]
    [MemberData(nameof(PublishedDocuments))]
    public void EveryPublishedResponseIsReadWithNothingLeftUnmapped(string fileName)
    {
        DocumentResult result = Library.Read(ReadCorpusFile(fileName));

        result.Kind.ShouldBe(DocumentKind.UblOrderResponse);
        OrderResponse response = result.RequireOrderResponse();

        response.Number.Value.ShouldNotBeNullOrWhiteSpace();
        response.OrderReference.Value.ShouldNotBeNullOrWhiteSpace();
        response.ResponseCode.Value.ShouldNotBeNullOrWhiteSpace();

        result.Diagnostics
            .Where(diagnostic => diagnostic.Code == UblDiagnostics.UnmappedElement.Code)
            .Select(diagnostic => diagnostic.Found)
            .ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(PublishedDocuments))]
    public void AndComesBackWithTheSameElementsInTheSamePlaces(string fileName)
    {
        string xml = ReadCorpusFile(fileName);

        Census(Library.Write(Library.Read(xml).RequireOrderResponse())).ShouldBe(Census(xml));
    }

    [Theory]
    [MemberData(nameof(PublishedDocuments))]
    public void AndInAShapeTheOasisSchemaAccepts(string fileName)
    {
        ValidationReport report = Library.Validate(
            Library.Write(Library.Read(ReadCorpusFile(fileName)).RequireOrderResponse()));

        report.Errors.ShouldBeEmpty(
            string.Join(Environment.NewLine, report.Errors.Select(error => error.ToString())));
    }

    [Theory]
    [MemberData(nameof(PublishedDocuments))]
    public void AndIsStillAcceptedByPeppolsOwnRules(string fileName)
    {
        EInvoicing library = WithPeppolRules();

        ValidationReport report = library.Validate(
            library.Write(library.Read(ReadCorpusFile(fileName)).RequireOrderResponse()));

        report.Errors.ShouldBeEmpty(
            string.Join(Environment.NewLine, report.Errors.Select(error => error.ToString())));
    }

    /// <summary>
    /// The answer a buyer most needs before the goods arrive: not this, but that instead.
    /// </summary>
    /// <remarks>
    /// A response reduced to a status code cannot carry it, which is why the substituted line item is
    /// modelled rather than kept as extension data.
    /// </remarks>
    [Fact]
    public void ASellerOfferingSomethingElseSaysWhat()
    {
        Assert.SkipWhen(
            !Corpus().Any(),
            "The POACC artefacts are not present; run build/fetch-specs.sh poacc.");

        IEnumerable<OrderResponseLine> lines = Corpus()
            .Select(path => Library.Read(File.ReadAllText(path)).RequireOrderResponse())
            .SelectMany(response => response.Lines);

        OrderResponseLine substituted = lines.First(line => line.SubstitutedItem is not null);

        substituted.SubstitutedItem.ShouldNotBeNull().Name.Value.ShouldNotBeNullOrWhiteSpace();
        substituted.OrderLineReference.Value.ShouldNotBeNullOrWhiteSpace(
            "a substitute is only meaningful against the line it replaces");
    }

    /// <summary>
    /// Requested and promised are different claims by different parties, and are kept apart.
    /// </summary>
    /// <remarks>
    /// A buyer asking for Friday and a seller promising Monday is the ordinary case; collapsing the two into
    /// one delivery window would lose which of them said what.
    /// </remarks>
    [Fact]
    public void AndWhenTheSellerUndertakesToDeliver()
    {
        Assert.SkipWhen(
            !Corpus().Any(),
            "The POACC artefacts are not present; run build/fetch-specs.sh poacc.");

        IEnumerable<OrderResponse> responses = Corpus()
            .Select(path => Library.Read(File.ReadAllText(path)).RequireOrderResponse());

        OrderDelivery promised = responses
            .SelectMany(response => response.Lines.Select(line => line.Delivery).Prepend(response.Delivery))
            .First(delivery => delivery?.PromisedFrom.IsSet == true)!;

        promised.PromisedFrom.Value.ShouldNotBeNull();
        promised.RequestedFrom.IsSet.ShouldBeFalse("a promise is not a request");
    }

    private static EInvoicing WithPeppolRules()
    {
        string rules = Path.Combine(CorpusRoot(), "rules");

        Assert.SkipWhen(
            !Directory.Exists(rules),
            "The POACC rule sets are not present; run build/fetch-specs.sh poacc.");

        return EInvoicing.Create(builder => builder
            .AddDefaults()
            .AddPeppol()
            .AddUblSchema()
            .AddPeppolPostAwardRulesFrom(rules));
    }

    private static string ReadCorpusFile(string fileName)
    {
        string? path = Corpus().FirstOrDefault(candidate => Path.GetFileName(candidate) == fileName);

        Assert.SkipWhen(
            path is null,
            "The POACC artefacts are not present; run build/fetch-specs.sh poacc.");

        return File.ReadAllText(path!);
    }

    private static IEnumerable<string> Corpus()
    {
        string root = Path.Combine(CorpusRoot(), "examples");

        return Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "*.xml", SearchOption.AllDirectories)
                .Where(path => Declares(path, PeppolPostAwardProfiles.OrderResponse))
                .Order()
            : [];
    }

    /// <summary>Whether a document declares exactly this profile.</summary>
    private static bool Declares(string path, Profiles.Profile profile) =>
        XDocument.Load(path).Root?.Element(UblNames.Cbc + "CustomizationID")?.Value.Trim() == profile.Id.Value;

    private static string CorpusRoot() => Path.Combine(RepositoryRoot(), "specs", "peppol", "poacc");

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
