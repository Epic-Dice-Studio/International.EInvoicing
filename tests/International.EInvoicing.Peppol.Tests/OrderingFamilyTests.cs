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

    public static TheoryData<string> Agreements => Documents(PeppolPostAwardProfiles.OrderAgreement);

    public static TheoryData<string> Changes => Documents(PeppolPostAwardProfiles.OrderChange);

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

    /// <summary>
    /// An order change is an order that amends an earlier one, and is read as one.
    /// </summary>
    /// <remarks>
    /// UBL gives it its own root and one element the order does not have — the sequence number saying which
    /// amendment this is — and is otherwise the same document. So it fills the same model, and
    /// <see cref="DocumentKind"/> tells the two apart: exactly the arrangement an invoice and a credit note
    /// already have here.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Changes))]
    public void AnOrderChangeIsAnOrderThatAmendsAnEarlierOne(string fileName)
    {
        DocumentResult result = Library.Read(ReadCorpusFile(fileName));

        result.Kind.ShouldBe(DocumentKind.UblOrderChange);
        Order change = result.RequireOrder();

        change.SequenceNumber.Value.ShouldNotBeNullOrWhiteSpace(
            "two amendments to one order may not arrive in the order they were sent");
        change.OrderReference.Value.ShouldNotBeNullOrWhiteSpace("a change that names no order changes nothing");
        change.Lines.ShouldContain(line => line.StatusCode.IsSet,
            "a change restates every line and marks the ones that moved");

        Unmapped(result).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Changes))]
    public void AndIsWrittenBackUnderItsOwnRoot(string fileName)
    {
        string xml = ReadCorpusFile(fileName);
        string written = Library.Write(Library.Read(xml).RequireOrder());

        written.ShouldContain("OrderChange", Case.Sensitive);
        Census(written).ShouldBe(Census(xml));
    }

    /// <summary>
    /// The order agreement is the order response restating the whole order as the parties settled it.
    /// </summary>
    /// <remarks>
    /// Same root and same reader as the response, and a much fuller payload: the totals, the VAT breakdown,
    /// the allowances, the extra parties, and on each item the certificates and the specification the
    /// parties agreed against. It is the document that says what was actually agreed, so an element of it
    /// left unmapped is a term of a contract nobody can see.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Agreements))]
    public void AnOrderAgreementRestatesTheWholeOrder(string fileName)
    {
        DocumentResult result = Library.Read(ReadCorpusFile(fileName));

        result.Kind.ShouldBe(DocumentKind.UblOrderResponse);
        OrderResponse agreement = result.RequireOrderResponse();

        agreement.Totals.DuePayableAmount.Value.ShouldNotBeNull();
        agreement.VatBreakdown.ShouldNotBeEmpty();
        agreement.AllowancesAndCharges.ShouldNotBeEmpty();
        agreement.AdditionalDocuments.ShouldNotBeEmpty();

        OrderItem item = agreement.Lines[0].Item.ShouldNotBeNull();
        item.Certificates.ShouldNotBeEmpty("what an item is certified as is a term of the agreement");
        item.Certificates[0].Issuer.ShouldNotBeNull("a certificate nobody issued is worth nothing");

        Unmapped(result).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Agreements))]
    public void AndComesBackWithEveryOneOfThoseTerms(string fileName)
    {
        string xml = ReadCorpusFile(fileName);

        Census(Library.Write(Library.Read(xml).RequireOrderResponse())).ShouldBe(Census(xml));
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
    [MemberData(nameof(Agreements))]
    [MemberData(nameof(Changes))]
    public void AndNoneIsMadeWorseByBeingWrittenBack(string fileName)
    {
        string xml = ReadCorpusFile(fileName);
        DocumentResult read = Library.Read(xml);

        string written = read.Kind switch
        {
            DocumentKind.UblOrderCancellation => Library.Write(read.RequireOrderCancellation()),
            DocumentKind.UblOrderChange => Library.Write(read.RequireOrder()),
            _ => Library.Write(read.RequireOrderResponse()),
        };

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
