using System.Xml.Linq;
using International.EInvoicing.Model;
using International.EInvoicing.Ubl;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Xsd;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Peppol.Tests;

/// <summary>
/// The order: what the buyer asked for, and the document the other two are answered against.
/// </summary>
/// <remarks>
/// A despatch advice says what was sent of it and an invoice says what is owed for it, so an integrator who
/// can read all three can check the second two against the first — which is the whole reason the post-award
/// documents are worth carrying.
/// </remarks>
public class OrderTests
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
    public void EveryPublishedOrderIsReadWithNothingLeftUnmapped(string fileName)
    {
        DocumentResult result = Library.Read(ReadCorpusFile(fileName));

        result.Kind.ShouldBe(DocumentKind.UblOrder);
        Order order = result.RequireOrder();

        order.Number.Value.ShouldNotBeNullOrWhiteSpace();
        order.Lines.ShouldNotBeEmpty();
        order.Buyer.ShouldNotBeNull();
        order.Seller.ShouldNotBeNull();

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

        Census(Library.Write(Library.Read(xml).RequireOrder())).ShouldBe(Census(xml));
    }

    /// <summary>Element order is normative in UBL, and only the schema judges it.</summary>
    [Theory]
    [MemberData(nameof(PublishedDocuments))]
    public void AndInAShapeTheOasisSchemaAccepts(string fileName)
    {
        ValidationReport report = Library.Validate(
            Library.Write(Library.Read(ReadCorpusFile(fileName)).RequireOrder()));

        report.Errors.ShouldBeEmpty(
            string.Join(Environment.NewLine, report.Errors.Select(error => error.ToString())));
    }

    [Theory]
    [MemberData(nameof(PublishedDocuments))]
    public void AndIsStillAcceptedByPeppolsOwnRules(string fileName)
    {
        EInvoicing library = WithPeppolRules();

        ValidationReport report = library.Validate(
            library.Write(library.Read(ReadCorpusFile(fileName)).RequireOrder()));

        report.Errors.ShouldBeEmpty(
            string.Join(Environment.NewLine, report.Errors.Select(error => error.ToString())));
    }

    /// <summary>What the buyer asked for, and on what terms.</summary>
    [Fact]
    public void AnOrderSaysWhatIsWantedAndWhen()
    {
        Order order = Library.Read(ReadCorpusFile("Order_Example.xml")).RequireOrder();

        order.Number.Value.ShouldNotBeNullOrWhiteSpace();
        order.CurrencyCode.Value.ShouldNotBeNullOrWhiteSpace();
        order.Totals.DuePayableAmount.Value.ShouldNotBeNull();

        OrderLine line = order.Lines[0];
        line.Quantity.Value.ShouldNotBeNull();
        line.Item.ShouldNotBeNull().Name.Value.ShouldNotBeNullOrWhiteSpace();
        line.Price.ShouldNotBeNull().NetPrice.Value.ShouldNotBeNull();
    }

    /// <summary>
    /// Whether a short delivery is acceptable is the buyer's to say, and the order is where they say it.
    /// </summary>
    /// <remarks>
    /// It is the term that connects the three documents: a line the buyer will not take in part makes an
    /// outstanding quantity on the despatch advice a failure rather than a note.
    /// </remarks>
    [Fact]
    public void AndWhetherPartOfALineWillDo()
    {
        Assert.SkipWhen(
            !Corpus().Any(),
            "The POACC artefacts are not present; run build/fetch-specs.sh poacc.");

        IEnumerable<OrderLine> lines = Corpus()
            .Select(path => Library.Read(File.ReadAllText(path)).RequireOrder())
            .SelectMany(order => order.Lines);

        lines.ShouldContain(line => line.PartialDeliveryAccepted.IsSet);
    }

    /// <summary>A party's tax registration and its registered address, which an order carries and an invoice does not.</summary>
    [Fact]
    public void AndWhoIsRegisteredWhere()
    {
        Order order = Library.Read(ReadCorpusFile("Order_Example.xml")).RequireOrder();

        Party buyer = order.Buyer.ShouldNotBeNull();
        buyer.VatIdentifier.Value.ShouldNotBeNullOrWhiteSpace();
        buyer.Address.ShouldNotBeNull().CountryCode.Value.ShouldNotBeNullOrWhiteSpace();
        buyer.RegistrationAddress.ShouldNotBeNull().City.Value.ShouldNotBeNullOrWhiteSpace();
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

    /// <summary>The orders of the fetched corpus, which holds more than one kind of document.</summary>
    private static IEnumerable<string> Corpus()
    {
        string root = Path.Combine(CorpusRoot(), "examples");

        return Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "*.xml", SearchOption.AllDirectories)
                .Where(path => File.ReadAllText(path)
                    .Contains(PeppolPostAwardProfiles.Order.Id.Value, StringComparison.Ordinal))
                .Order()
            : [];
    }

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
