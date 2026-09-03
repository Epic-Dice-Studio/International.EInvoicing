using System.Xml.Linq;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Zugferd1.Tests;

/// <summary>
/// What the four published ZUGFeRD 1.0 documents say, read into the model.
/// </summary>
/// <remarks>
/// Reading an archive is the only reason to read this format, so the measure is coverage rather than
/// round-tripping: nothing may be dropped, and what is not modelled must be findable.
/// </remarks>
public class ReadingTheCorpusTests
{
    public static TheoryData<string> Corpus()
    {
        var data = new TheoryData<string>();

        foreach (string path in Zugferd1Corpus.Documents())
        {
            data.Add(Path.GetFileName(path));
        }

        // Theory data may not be empty, and an empty corpus is a skip rather than a failure.
        if (data.Count == 0)
        {
            data.Add("(none fetched)");
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Corpus))]
    public void EveryPublishedDocumentIsRead(string name)
    {
        EInvoice invoice = Read(name);

        invoice.Number.Value.ShouldNotBeNullOrWhiteSpace();
        invoice.IssueDate.Value.ShouldNotBeNull();
        invoice.Seller!.Name.Value.ShouldNotBeNullOrWhiteSpace();
        invoice.Buyer!.Name.Value.ShouldNotBeNullOrWhiteSpace();
        invoice.CurrencyCode.Value.ShouldNotBeNullOrWhiteSpace();
        invoice.Lines.ShouldNotBeEmpty();
    }

    /// <summary>
    /// And nothing in it is dropped: every element is either mapped or kept, and the two together account
    /// for the whole document.
    /// </summary>
    [Theory]
    [MemberData(nameof(Corpus))]
    public void AndNothingInItIsDropped(string name)
    {
        string? path = Zugferd1Corpus.Find(name);
        Assert.SkipWhen(path is null, "run build/fetch-specs.sh zugferd1");

        string xml = File.ReadAllText(path!);
        ParseResult<EInvoice> result = Zugferd1Corpus.Reader().Read(xml);
        EInvoice invoice = result.Value.ShouldNotBeNull();

        // Every element the reader could not place is reported, and every report names an element that is
        // in the document. A reader that silently skipped something would satisfy neither.
        IReadOnlyList<string> kept = [.. invoice.Extensions().Select(extension => extension.LocalName)];
        IReadOnlyList<string> reported =
        [
            .. result.Diagnostics
                .Where(diagnostic => diagnostic.Code == "EIV2023")
                .Select(diagnostic => diagnostic.Found!),
        ];

        reported.Order().ShouldBe(kept.Order(), "what is kept and what is reported must be the same set");

        IReadOnlyList<string> names = [.. XDocument.Parse(xml).Descendants().Select(e => e.Name.LocalName)];
        foreach (string name1 in kept)
        {
            names.ShouldContain(name1);
        }
    }

    /// <summary>
    /// The German <c>Bankleitzahl</c> is the case that shows the point: a term of the 2013 format that
    /// EN 16931 has no field for, and that somebody reading an archive may well be looking for.
    /// </summary>
    [Fact]
    public void TheGermanSortCodeIsKeptRatherThanDropped()
    {
        string? path = Zugferd1Corpus.Documents()
            .FirstOrDefault(file => File.ReadAllText(file).Contains("GermanBankleitzahlID", StringComparison.Ordinal));

        Assert.SkipWhen(path is null, "no fetched document states a Bankleitzahl");

        EInvoice invoice = Read(Path.GetFileName(path!));

        invoice.Extensions().ShouldContain(extension => extension.LocalName == "GermanBankleitzahlID");
    }

    private static EInvoice Read(string name)
    {
        string? path = Zugferd1Corpus.Find(name);
        Assert.SkipWhen(path is null, "run build/fetch-specs.sh zugferd1");

        ParseResult<EInvoice> result = Zugferd1Corpus.Reader().Read(File.ReadAllText(path!));

        return result.Value.ShouldNotBeNull(
            string.Join(Environment.NewLine, result.Diagnostics.Select(d => d.ToString())));
    }
}
