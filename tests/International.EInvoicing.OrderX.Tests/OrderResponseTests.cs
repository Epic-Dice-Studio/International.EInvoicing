using System.Xml.Linq;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.OrderX.Reading;
using International.EInvoicing.OrderX.Writing;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Xsd;
using Shouldly;
using Xunit;

namespace International.EInvoicing.OrderX.Tests;

/// <summary>
/// The third Order-X document: what the seller says about an order.
/// </summary>
/// <remarks>
/// <para>
/// FNFE-MPE publishes no reference order response, so there is nothing to round-trip against the way the
/// order has. What there is instead is their order — and a response shares its whole transaction shape. So
/// the fixture is <em>their</em> document with the three things a response changes: the type code, a status
/// on the document, and a status on each line. The content is theirs; only the answer is ours.
/// </para>
/// <para>
/// That is weaker than a published example and is worth saying plainly. What it does establish is that the
/// reader and writer are inverse over a realistic document, and that what comes out satisfies the
/// publisher's own schema and their 124 assertions — which is the same bar the order clears.
/// </para>
/// </remarks>
public class OrderResponseTests
{
    private static readonly XNamespace Rsm = OrderXNames.Rsm;
    private static readonly XNamespace Ram = OrderXNames.Ram;

    [Fact]
    public void AResponseIsToldFromAnOrderByItsTypeCode()
    {
        OrderResponse response = TheResponse();

        response.TypeCode.Value.ShouldBe(OrderXTypeCodes.OrderResponse);
        response.ResponseCode.Value.ShouldBe("29", "UNTDID 1373 — accepted");
        response.Number.Value.ShouldBe("PO123456789");
    }

    [Fact]
    public void AndEveryLineCarriesTheSellerSAnswer()
    {
        OrderResponse response = TheResponse();

        response.Lines.Count.ShouldBe(3);
        response.Lines.Select(line => line.StatusCode.Value).ShouldAllBe(code => code == "5");
    }

    /// <summary>
    /// The agreed quantity is kept apart from the requested one, which is the whole point of a response
    /// that is not a plain acceptance.
    /// </summary>
    [Fact]
    public void AndTheQuantityAgreedIsKeptApartFromTheQuantityAsked()
    {
        OrderResponseLine line = TheResponse().Lines[0];

        line.RequestedQuantity.Value.ShouldNotBeNull();
        line.Quantity.Value.ShouldNotBeNull();
        line.Quantity.Value.ShouldNotBe(line.RequestedQuantity.Value, "the fixture agrees to less than was asked");
    }

    [Fact]
    public void EveryElementOfTheResponseComesBack()
    {
        string source = AResponseDerivedFromThePublishedOrder();
        string written = new OrderXOrderResponseWriter().WriteToString(Read(source));

        IReadOnlyList<string> before = Census(source);
        IReadOnlyList<string> after = Census(written);

        string[] lost = [.. before.Except(after)];
        string[] invented = [.. after.Except(before)];

        lost.ShouldBeEmpty($"lost: {string.Join(", ", lost)}");
        invented.ShouldBeEmpty($"invented: {string.Join(", ", invented)}");
    }

    /// <summary>And in the sequence the schema declares, which a census cannot see.</summary>
    [Fact]
    public void AndInTheOrderTheDocumentHadThem()
    {
        string source = AResponseDerivedFromThePublishedOrder();
        string written = new OrderXOrderResponseWriter().WriteToString(Read(source));

        Sequence(written).ShouldBe(Sequence(source));
    }

    /// <summary>And what we write satisfies the publisher's own schema and rules.</summary>
    [Fact]
    public void AndWhatWeWriteSatisfiesTheSchemaAndTheRules()
    {
        string schemas = Path.Combine(OrderXCorpus.Root, "schema");
        string rules = Path.Combine(OrderXCorpus.Root, "schematron");

        Assert.SkipWhen(
            !Directory.Exists(schemas) || !Directory.Exists(rules),
            "run build/fetch-specs.sh order-x");

        EInvoicing library = EInvoicing.Create(builder => builder
            .AddDefaults()
            .AddOrderX()
            .AddOrderXSchemaFrom(schemas)
            .AddOrderXRulesFrom(rules));

        string written = new OrderXOrderResponseWriter().WriteToString(Read(AResponseDerivedFromThePublishedOrder()));
        ValidationReport report = library.Validate(written);

        report.Errors.ShouldBeEmpty(
            string.Join(Environment.NewLine, report.Errors.Select(error => error.ToString())));
        report.RuleSets.ShouldContain(ruleSet => ruleSet.Ran);
    }

    private static OrderResponse Read(string xml)
    {
        ParseResult<OrderResponse> result =
            new OrderXOrderResponseReader(new EInvoicingOptions(), new ProfileResolver(new ProfileRegistry(OrderXProfiles.All)))
                .Read(xml);

        return result.Value.ShouldNotBeNull(
            string.Join(Environment.NewLine, result.Diagnostics.Select(d => d.ToString())));
    }

    private static OrderResponse TheResponse() => Read(AResponseDerivedFromThePublishedOrder());

    /// <summary>
    /// FNFE-MPE's reference order, answered: type code 231, a status on the document, a status on every
    /// line, and one line agreed at a lower quantity than was asked for.
    /// </summary>
    private static string AResponseDerivedFromThePublishedOrder()
    {
        string? path = OrderXCorpus.Find(OrderXCorpus.ReferenceOrder);
        Assert.SkipWhen(path is null, "run build/fetch-specs.sh order-x");

        XDocument document = XDocument.Load(path!);
        XElement root = document.Root!;

        XElement exchanged = root.Element(Rsm + "ExchangedDocument")!;
        exchanged.Element(Ram + "TypeCode")!.Value = OrderXTypeCodes.OrderResponse;
        // UNTDID 1373, which is what the schema enumerates: 29 is "accepted".
        exchanged.Element(Ram + "TypeCode")!.AddAfterSelf(new XElement(Ram + "StatusCode", "29"));

        foreach (XElement line in root.Descendants(Ram + "IncludedSupplyChainTradeLineItem"))
        {
            XElement document1 = line.Element(Ram + "AssociatedDocumentLineDocument")!;
            document1.Element(Ram + "LineID")!.AddAfterSelf(new XElement(Ram + "LineStatusCode", "5"));

            // An order asks with RequestedQuantity; a response answers with AgreedQuantity beside it.
            XElement delivery = line.Element(Ram + "SpecifiedLineTradeDelivery")!;
            if (delivery.Element(Ram + "RequestedQuantity") is { } requested)
            {
                requested.AddAfterSelf(new XElement(
                    Ram + "AgreedQuantity",
                    new XAttribute("unitCode", requested.Attribute("unitCode")?.Value ?? "C62"),
                    "1"));
            }
        }

        return document.ToString();
    }

    private static IReadOnlyList<string> Census(string xml) =>
        [.. XDocument.Parse(xml).Descendants()
            .GroupBy(element => element.Name.ToString())
            .Select(group => $"{group.Key}={group.Count()}")
            .Order()];

    private static IReadOnlyList<string> Sequence(string xml) =>
        [.. XDocument.Parse(xml).Descendants().Select(element => element.Name.LocalName)];
}
