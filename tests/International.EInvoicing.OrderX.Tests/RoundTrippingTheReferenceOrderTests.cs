using System.Xml.Linq;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.OrderX.Writing;
using Shouldly;
using Xunit;

namespace International.EInvoicing.OrderX.Tests;

/// <summary>
/// The reference order, read into the model and written back, judged element for element.
/// </summary>
/// <remarks>
/// Comparing the census — every element name with how many times it appears — rather than the text is what
/// makes this a test of the mapping instead of a test of the formatter. Whitespace, prefixes and attribute
/// order are the writer's business; whether a term survived the crossing is the library's.
/// </remarks>
public class RoundTrippingTheReferenceOrderTests
{
    [Fact]
    public void EveryElementOfTheReferenceOrderComesBack()
    {
        (string original, string written) = ReadAndWrite();

        IReadOnlyList<string> before = Census(original);
        IReadOnlyList<string> after = Census(written);

        string[] lost = [.. before.Except(after)];
        string[] invented = [.. after.Except(before)];

        lost.ShouldBeEmpty($"lost: {string.Join(", ", lost)}");
        invented.ShouldBeEmpty($"invented: {string.Join(", ", invented)}");
    }

    /// <summary>
    /// And the elements come back in the sequence the schema declares, which is the half a census cannot see.
    /// </summary>
    [Fact]
    public void AndInTheOrderTheDocumentHadThem()
    {
        (string original, string written) = ReadAndWrite();

        Sequence(written).ShouldBe(Sequence(original));
    }

    private static (string Original, string Written) ReadAndWrite()
    {
        string? path = OrderXCorpus.Find(OrderXCorpus.ReferenceOrder);
        Assert.SkipWhen(path is null, "run build/fetch-specs.sh order-x");

        string original = File.ReadAllText(path!);
        ParseResult<Order> result = OrderXCorpus.Reader().Read(original);
        Order order = result.Value.ShouldNotBeNull();

        return (original, new OrderXOrderWriter().WriteToString(order));
    }

    private static IReadOnlyList<string> Census(string xml) =>
        [.. XDocument.Parse(xml).Descendants()
            .GroupBy(element => element.Name.ToString())
            .Select(group => $"{group.Key}={group.Count()}")
            .Order()];

    /// <summary>Every element's path, in document order, so a term written in the wrong place shows.</summary>
    private static IReadOnlyList<string> Sequence(string xml) =>
        [.. XDocument.Parse(xml).Descendants().Select(element => element.Name.LocalName)];
}
