using System.Xml.Linq;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Xsd;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Peppol.Tests;

/// <summary>
/// The Peppol documents that are not invoices, measured against the corpus their publisher ships.
/// </summary>
/// <remarks>
/// An Invoice Response is what a receiver owes a sender — the invoice is in process, accepted, rejected,
/// under query, or paid — and a Message Level Response answers the question underneath it: whether the
/// message arrived and parsed at all. Both are a UBL <c>ApplicationResponse</c>, so both are read here, and
/// what they are read against is OpenPEPPOL's own thirteen use cases rather than documents written for the
/// occasion.
/// </remarks>
public class InvoiceResponseTests
{
    private static readonly EInvoicing Library = EInvoicing.Create(builder => builder.AddDefaults().AddPeppol());

    private static readonly EInvoicing Schemas =
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
        string xml = ReadCorpusFile(fileName);

        DocumentResult result = Library.Read(xml);

        result.Kind.ShouldBe(DocumentKind.UblApplicationResponse);
        LifecycleStatusMessage message = result.RequireLifecycleStatus();

        message.Identifier.Value.ShouldNotBeNullOrWhiteSpace();
        message.References.ShouldNotBeEmpty();

        result.Diagnostics
            .Where(diagnostic => diagnostic.Code == UblDiagnostics.UnmappedElement.Code)
            .Select(diagnostic => diagnostic.Found)
            .ShouldBeEmpty();
    }

    /// <summary>
    /// Read, written back, and compared element by element — which is the only check that catches a term
    /// read into the model and written somewhere else.
    /// </summary>
    [Theory]
    [MemberData(nameof(PublishedDocuments))]
    public void AndComesBackWithTheSameElementsInTheSamePlaces(string fileName)
    {
        string xml = ReadCorpusFile(fileName);

        LifecycleStatusMessage message = Library.Read(xml).RequireLifecycleStatus();
        string written = Library.Write(message, DocumentSyntax.Ubl);

        Census(written).ShouldBe(Census(xml));
    }

    /// <summary>
    /// And is still a document the OASIS schema accepts, which the element census cannot tell you.
    /// </summary>
    /// <remarks>
    /// Element order is normative in UBL and no business rule looks at it, so a response can carry every
    /// element the original did and still be something a receiver's parser rejects outright. The schema is
    /// the only thing that judges the shape.
    /// </remarks>
    [Theory]
    [MemberData(nameof(PublishedDocuments))]
    public void AndInAShapeTheOasisSchemaAccepts(string fileName)
    {
        ValidationReport report = Schemas.Validate(
            Schemas.Write(Schemas.Read(ReadCorpusFile(fileName)).RequireLifecycleStatus(), DocumentSyntax.Ubl));

        report.Errors.ShouldBeEmpty(
            string.Join(Environment.NewLine, report.Errors.Select(error => error.ToString())));
    }

    /// <summary>
    /// And is still accepted by Peppol's own rules, which is the check no schema can make.
    /// </summary>
    /// <remarks>
    /// OpenPEPPOL generates the structural half of these rule sets at build time and publishes only the
    /// compiled form, so what runs here is compiled XSLT with the assertions recovered from it. That is the
    /// same path Croatia and the tax data documents take, and it is why these rules run at all.
    /// </remarks>
    [Theory]
    [MemberData(nameof(PublishedDocuments))]
    public void AndIsStillAcceptedByPeppolsOwnRules(string fileName)
    {
        string xml = ReadCorpusFile(fileName);
        EInvoicing library = WithPeppolRules();

        ValidationReport report = library.Validate(
            library.Write(library.Read(xml).RequireLifecycleStatus(), DocumentSyntax.Ubl));

        report.Errors.ShouldBeEmpty(
            string.Join(Environment.NewLine, report.Errors.Select(error => error.ToString())));
    }

    /// <summary>
    /// A rejection with nothing said about why is what <c>PEPPOL-T111-R001</c> exists to stop, and this
    /// library's own writer can produce one.
    /// </summary>
    [Fact]
    public void ARejectionWithNoClarificationIsRefusedByThoseRules()
    {
        EInvoicing library = WithPeppolRules();

        LifecycleStatusMessage message = library
            .Read(ReadCorpusFile("T111-uc004b-Rejected requesting reissue.xml"))
            .RequireLifecycleStatus();

        message.References.Single().StatusDetails.Clear();

        library.Validate(library.Write(message, DocumentSyntax.Ubl))
            .Errors.Select(error => error.RuleIdentifier)
            .ShouldContain("PEPPOL-T111-R001");
    }

    /// <summary>The status a receiver acts on, and the clarification that says what to do about it.</summary>
    [Fact]
    public void ARejectionSaysWhyAndWhatIsExpectedNext()
    {
        LifecycleStatusMessage message = Library
            .Read(ReadCorpusFile("T111-uc004b-Rejected requesting reissue.xml"))
            .RequireLifecycleStatus();

        ReferencedDocumentStatus status = message.References.ShouldHaveSingleItem();
        status.ProcessConditionCode.Value.ShouldBe(PeppolResponseCodes.Rejected);
        status.DocumentIdentifier.Value.ShouldBe("inv021");

        status.StatusDetails.Select(detail => detail.ReasonCode.Value).ShouldContain("REF");
        status.StatusDetails.Select(detail => detail.RequestedActionCode.Value).ShouldContain("NIN");
    }

    /// <summary>
    /// A message level response points at the place in the document that failed, which is how a receiver
    /// tells "this document is wrong" from "line 3 of this document is wrong".
    /// </summary>
    [Fact]
    public void AMessageLevelResponseSaysWhichLineFailed()
    {
        LifecycleStatusMessage message = Library
            .Read(ReadCorpusFile("MessageLevelResponse_Example.xml"))
            .RequireLifecycleStatus();

        message.SpecificationIdentifier.ShouldBe(PeppolResponseProfiles.MessageLevelResponse.Id);

        ReferencedLineStatus line = message.References.ShouldHaveSingleItem().LineStatuses.ShouldHaveSingleItem();
        line.LineIdentifier.Value.ShouldStartWith("/Catalogue/cac:CatalogueLine[3]");
        line.ProcessConditionCode.Value.ShouldBe(PeppolResponseCodes.Rejected);
    }

    /// <summary>The profile is what tells the two apart, and both are registered.</summary>
    [Fact]
    public void BothProfilesAreResolvedExactly()
    {
        Library.Read(ReadCorpusFile("InvoiceResponse_Example.xml")).Profile.ShouldNotBeNull()
            .IsExact.ShouldBeTrue();
        Library.Read(ReadCorpusFile("MessageLevelResponse_Example.xml")).Profile.ShouldNotBeNull()
            .IsExact.ShouldBeTrue();
    }

    /// <summary>
    /// The seven codes an Invoice Response may carry, checked against the list they were taken from.
    /// </summary>
    /// <remarks>
    /// A transcribed code list is wrong the day the publisher changes it and nobody notices, so the
    /// constants are compared against OpenPEPPOL's own <c>UNCL4343-T111</c> subset on every build the
    /// artefacts are present for.
    /// </remarks>
    [Fact]
    public void TheResponseCodesAreTheOnesThePublisherLists()
    {
        string path = Path.Combine(CorpusRoot(), "codelist", "UNCL4343-T111.xml");
        Assert.SkipWhen(!File.Exists(path), "The POACC artefacts are not present; run build/fetch-specs.sh poacc.");

        XNamespace list = "urn:fdc:difi.no:2017:vefa:structure:CodeList-1";
        string[] published = [.. XDocument.Load(path).Descendants(list + "Code").Select(code => code.Element(list + "Id")!.Value)];

        published.Order().ShouldBe(PeppolResponseCodes.All.Order());
    }

    /// <summary>
    /// The library with Peppol's own rules, which are fetched rather than shipped: OpenPEPPOL declares no
    /// licence permitting redistribution.
    /// </summary>
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
            .AddPeppolResponseRulesFrom(rules));
    }

    /// <summary>Reading never throws on the document, whatever arrives.</summary>
    [Fact]
    public void AResponseThatIsNotWellFormedIsReportedRatherThanRaised()
    {
        DocumentResult result = Library.Read(
            """<ApplicationResponse xmlns="urn:oasis:names:specification:ubl:schema:xsd:ApplicationResponse-2">""");

        result.LifecycleStatus.ShouldBeNull();
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(UblDiagnostics.MalformedDocument.Code);
    }

    /// <summary>
    /// An element this reader does not map is kept verbatim and written back, rather than dropped.
    /// </summary>
    /// <remarks>
    /// Peppol's own corpus leaves nothing unmapped, which is the point of the theory above — so what is
    /// checked here is the promise for the document nobody anticipated.
    /// </remarks>
    [Fact]
    public void AnElementNobodyMappedIsKeptAndSaidOutLoud()
    {
        string xml = ReadCorpusFile("InvoiceResponse_Example.xml")
            .Replace(
                "<cbc:Note>text</cbc:Note>",
                "<cbc:Note>text</cbc:Note><cbc:UUID>0d1b6ffe</cbc:UUID>",
                StringComparison.Ordinal);

        DocumentResult result = Library.Read(xml);

        result.Diagnostics
            .Where(diagnostic => diagnostic.Code == UblDiagnostics.UnmappedElement.Code)
            .Select(diagnostic => diagnostic.Found)
            .ShouldContain("UUID");

        Library.Write(result.RequireLifecycleStatus(), DocumentSyntax.Ubl).ShouldContain("0d1b6ffe");
    }

    private static string ReadCorpusFile(string fileName)
    {
        Assert.SkipWhen(
            fileName == "(none fetched)",
            "The POACC artefacts are not present; run build/fetch-specs.sh poacc.");

        string path = Corpus().Single(candidate => Path.GetFileName(candidate) == fileName);
        return File.ReadAllText(path);
    }

    private static IEnumerable<string> Corpus()
    {
        string root = Path.Combine(CorpusRoot(), "examples");

        return Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "*.xml", SearchOption.AllDirectories).Order()
            : [];
    }

    private static string CorpusRoot() => Path.Combine(RepositoryRoot(), "specs", "peppol", "poacc");

    /// <summary>How many of each element the document holds, which is what a round trip must preserve.</summary>
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
