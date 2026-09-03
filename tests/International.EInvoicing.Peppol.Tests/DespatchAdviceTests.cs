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
/// The despatch advice: what actually left the warehouse.
/// </summary>
/// <remarks>
/// An invoice says what is owed and an order says what was asked for; only this says what was sent. It is
/// the document a buyer reconciles the other two against — receiving ten of an ordered twelve is the whole
/// reason <c>cbc:OutstandingQuantity</c> exists, and why Peppol refuses one without a reason beside it.
/// </remarks>
public class DespatchAdviceTests
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
    public void EveryPublishedDespatchAdviceIsRead(string fileName)
    {
        DocumentResult result = Library.Read(ReadCorpusFile(fileName));

        result.Kind.ShouldBe(DocumentKind.UblDespatchAdvice);
        DespatchAdvice advice = result.RequireDespatchAdvice();

        advice.Number.Value.ShouldNotBeNullOrWhiteSpace();
        advice.Lines.ShouldNotBeEmpty();
        advice.Lines.ShouldAllBe(line => line.DeliveredQuantity.IsSet);
    }

    /// <summary>
    /// Read, written back, and compared element by element — the check that catches a term read into the
    /// model and written somewhere else, and the one that proves the extension data really is written back.
    /// </summary>
    [Theory]
    [MemberData(nameof(PublishedDocuments))]
    public void AndComesBackWithTheSameElementsInTheSamePlaces(string fileName)
    {
        string xml = ReadCorpusFile(fileName);

        Census(Library.Write(Library.Read(xml).RequireDespatchAdvice())).ShouldBe(Census(xml));
    }

    /// <summary>Element order is normative in UBL, and only the schema judges it.</summary>
    [Theory]
    [MemberData(nameof(PublishedDocuments))]
    public void AndInAShapeTheOasisSchemaAccepts(string fileName)
    {
        ValidationReport report = Library.Validate(
            Library.Write(Library.Read(ReadCorpusFile(fileName)).RequireDespatchAdvice()));

        report.Errors.ShouldBeEmpty(
            string.Join(Environment.NewLine, report.Errors.Select(error => error.ToString())));
    }

    [Theory]
    [MemberData(nameof(PublishedDocuments))]
    public void AndIsStillAcceptedByPeppolsOwnRules(string fileName)
    {
        EInvoicing library = WithPeppolRules();

        ValidationReport report = library.Validate(
            library.Write(library.Read(ReadCorpusFile(fileName)).RequireDespatchAdvice()));

        report.Errors.ShouldBeEmpty(
            string.Join(Environment.NewLine, report.Errors.Select(error => error.ToString())));
    }

    /// <summary>What the buyer needs: how much arrived, how much did not, and why.</summary>
    [Fact]
    public void AShortDeliverySaysHowMuchIsMissingAndWhy()
    {
        DespatchAdvice advice = Library
            .Read(ReadCorpusFile("DespatchAdvice_Example.xml"))
            .RequireDespatchAdvice();

        DespatchLine line = advice.Lines.ShouldHaveSingleItem();

        line.DeliveredQuantity.Value.ShouldBe(10);
        line.OutstandingQuantity.Value.ShouldBe(2);
        line.OutstandingReason.Value.ShouldNotBeNullOrWhiteSpace();
        line.OrderLineReference.Value.ShouldBe("1");
        line.Item.ShouldNotBeNull().Name.Value.ShouldBe("beeswax");
    }

    /// <summary>
    /// Which physical items were sent, which is what a recall or a warranty claim is answered from.
    /// </summary>
    [Fact]
    public void AndWhichPhysicalItemsTheyWere()
    {
        DespatchLine line = Library
            .Read(ReadCorpusFile("DespatchAdvice_Example.xml"))
            .RequireDespatchAdvice()
            .Lines[0];

        ItemInstance instance = line.Item.ShouldNotBeNull().Instances.ShouldHaveSingleItem();
        instance.SerialIdentifier.Value.ShouldBe("4558784");
        instance.LotIdentifier.Value.ShouldBe("546378239");
        instance.BestBeforeDate.Value.ShouldBe(new DateOnly(2018, 12, 1));

        TransportHandlingUnit unit = line.Packaging.ShouldNotBeNull().HandlingUnits.ShouldHaveSingleItem();
        unit.TypeCode.Value.ShouldBe("4H");
        unit.Packages.Count.ShouldBe(2);
    }

    /// <summary>Where the goods went, when they left, and when they are expected.</summary>
    [Fact]
    public void AndHowTheGoodsTravel()
    {
        DespatchAdvice advice = Library
            .Read(ReadCorpusFile("DespatchAdvice_Example.xml"))
            .RequireDespatchAdvice();

        Shipment shipment = advice.Shipment.ShouldNotBeNull();
        shipment.TrackingIdentifier.Value.ShouldBe("456789");
        shipment.DespatchedAt.Value.ShouldNotBeNull();
        shipment.DespatchAddress.ShouldNotBeNull().City.Value.ShouldBe("Bridgtow");
        shipment.EstimatedDeliveryFrom.Value.ShouldNotBeNull();
        shipment.EstimatedDeliveryUntil.Value.ShouldNotBeNull();

        advice.DeliveryParty.ShouldNotBeNull().Contact.ShouldNotBeNull()
            .Name.Value.ShouldBe("Mr Fred Churchill");
        advice.DespatchParty.ShouldNotBeNull().Name.Value.ShouldBe("Consortial");
    }

    /// <summary>
    /// Goods reported missing with no reason given is what <c>PEPPOL-T16-R007</c> exists to catch, and this
    /// library's own writer can produce one.
    /// </summary>
    /// <remarks>
    /// Peppol flags it <c>warning</c> rather than <c>fatal</c>: the despatch advice is still a usable
    /// document, and the buyer is still left with nothing to act on. The distinction is the rule set's to
    /// make, so this asserts the severity it actually publishes rather than the one that would be tidier.
    /// </remarks>
    [Fact]
    public void GoodsMissingWithNoReasonAreWarnedAboutByThoseRules()
    {
        EInvoicing library = WithPeppolRules();

        DespatchAdvice advice = library
            .Read(ReadCorpusFile("DespatchAdvice_Example.xml"))
            .RequireDespatchAdvice();

        advice.Lines[0].OutstandingReason = Values.TextField.Unset;

        ValidationReport report = library.Validate(library.Write(advice));

        report.Warnings.Select(warning => warning.RuleIdentifier).ShouldContain("PEPPOL-T16-R007");
        report.Errors.ShouldBeEmpty("the document is still usable; only the buyer is left guessing");
    }

    /// <summary>
    /// What this library deliberately does not model, named so that the list is a decision rather than an
    /// accident.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One element in the whole published corpus: <c>cac:Person</c> on the carrier, which identifies the
    /// driver. It is kept verbatim and written back — the round trip above proves it — and reported as
    /// <c>EIV2020</c> rather than dropped in silence.
    /// </para>
    /// <para>
    /// Everything else the corpus carries is mapped, which is the standard the invoice side is held to. The
    /// list is asserted rather than merely allowed so that the day a reader stops mapping something, this
    /// says so instead of quietly growing.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheOneThingNotModelledIsNamedAndKept()
    {
        Assert.SkipWhen(
            !Corpus().Any(),
            "The POACC artefacts are not present; run build/fetch-specs.sh poacc.");

        string[] unmapped = [.. Corpus()
            .SelectMany(path => Library.Read(File.ReadAllText(path)).Diagnostics)
            .Where(diagnostic => diagnostic.Code == UblDiagnostics.UnmappedElement.Code)
            .Select(diagnostic => diagnostic.Found!)
            .Distinct()
            .Order()];

        unmapped.ShouldBe(["Person"]);
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

    /// <summary>
    /// The despatch advices of the fetched corpus, which holds more than one kind of document.
    /// </summary>
    private static IEnumerable<string> Corpus()
    {
        string root = Path.Combine(CorpusRoot(), "examples");

        return Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "*.xml", SearchOption.AllDirectories)
                .Where(path => File.ReadAllText(path)
                    .Contains(PeppolPostAwardProfiles.DespatchAdvice.Id.Value, StringComparison.Ordinal))
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
