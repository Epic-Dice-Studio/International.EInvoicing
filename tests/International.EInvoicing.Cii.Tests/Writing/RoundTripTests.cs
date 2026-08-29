using System.Xml.Linq;
using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.Configuration;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Cii.Tests.Writing;

/// <summary>
/// The promise these tests defend: a document read and written back loses nothing. What the model does not
/// describe travels in extension data, so it must come out the other side too.
/// </summary>
public class RoundTripTests
{
    private static CiiInvoiceReader Reader() =>
        new(new EInvoicingOptions(), new ProfileResolver(new ProfileRegistry(KnownProfiles.All)));

    [Theory]
    [MemberData(nameof(GoldenCorpus.CiiInvoiceCases), MemberType = typeof(GoldenCorpus))]
    public void NoElementOfTheOriginalDisappears(string fileName)
    {
        string original = GoldenCorpus.Read(fileName);
        EInvoice invoice = Reader().Read(original).Value!;

        string written = new CiiInvoiceWriter().WriteToString(invoice);

        Dictionary<string, int> before = CountElements(XElement.Parse(original));
        Dictionary<string, int> after = CountElements(XElement.Parse(written));

        string[] lost = [.. before
            .Where(pair => after.GetValueOrDefault(pair.Key) < pair.Value)
            .Select(pair => $"{Local(pair.Key)} ({pair.Value} -> {after.GetValueOrDefault(pair.Key)})")
            .Order(StringComparer.Ordinal)];

        lost.ShouldBeEmpty($"{fileName} lost elements on the way out: {string.Join(", ", lost)}");
    }

    [Theory]
    [MemberData(nameof(GoldenCorpus.CiiInvoiceCases), MemberType = typeof(GoldenCorpus))]
    public void WritingProducesAReadableDocumentCarryingTheSameKeyTerms(string fileName)
    {
        EInvoice original = Reader().Read(GoldenCorpus.Read(fileName)).Value!;

        string written = new CiiInvoiceWriter().WriteToString(original);
        EInvoice again = Reader().Read(written).Value!;

        again.Number.Value.ShouldBe(original.Number.Value);
        again.IssueDate.Value.ShouldBe(original.IssueDate.Value);
        again.CurrencyCode.Value.ShouldBe(original.CurrencyCode.Value);
        again.SpecificationIdentifier.ShouldBe(original.SpecificationIdentifier);
        again.Lines.Count.ShouldBe(original.Lines.Count);
        again.VatBreakdown.Count.ShouldBe(original.VatBreakdown.Count);
        again.Totals.DuePayableAmount.Value.ShouldBe(original.Totals.DuePayableAmount.Value);
    }

    private static Dictionary<string, int> CountElements(XElement root)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (XElement element in root.DescendantsAndSelf())
        {
            string key = element.Name.ToString();
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        return counts;
    }

    private static string Local(string qualifiedName) =>
        qualifiedName[(qualifiedName.IndexOf('}', StringComparison.Ordinal) + 1)..];
}
