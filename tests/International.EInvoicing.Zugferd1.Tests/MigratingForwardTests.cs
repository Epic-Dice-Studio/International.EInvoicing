using System.Globalization;
using System.Xml.Linq;
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Zugferd1.Tests;

/// <summary>
/// Reading a 2013 document and writing it forward as ZUGFeRD 2, judged against the publisher's own
/// conversion of the same file.
/// </summary>
/// <remarks>
/// This is the point of reading ZUGFeRD 1.0 at all, and mustangproject ships both halves of one migration —
/// the 2013 input and their ZUGFeRD 2 output — which makes it the only external judge available for a format
/// whose own publisher no longer distributes anything. The two need not agree element for element: their
/// converter and this library make different modelling choices, and an exact match would be a test of
/// sameness rather than of correctness. What must agree is what the invoice <em>says</em>.
/// </remarks>
public class MigratingForwardTests
{
    private static readonly XNamespace Ram =
        "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100";

    [Fact]
    public void TheMigratedInvoiceSaysWhatTheirsSays()
    {
        (XElement Ours, XElement Theirs) both = Migrate();

        Header(both.Ours, "ID").ShouldBe(Header(both.Theirs, "ID"));
        Header(both.Ours, "TypeCode").ShouldBe(Header(both.Theirs, "TypeCode"));
        Value(both.Ours, "InvoiceCurrencyCode").ShouldBe(Value(both.Theirs, "InvoiceCurrencyCode"));
    }

    /// <summary>And the money is the same money, which is the part a mistake would be expensive in.</summary>
    [Fact]
    public void AndTheTotalsAreTheSameTotals()
    {
        (XElement Ours, XElement Theirs) both = Migrate();

        foreach (string total in (string[])
        [
            "LineTotalAmount",
            "TaxBasisTotalAmount",
            "TaxTotalAmount",
            "GrandTotalAmount",
            "DuePayableAmount",
        ])
        {
            Amount(both.Ours, total).ShouldBe(Amount(both.Theirs, total), total);
        }
    }

    /// <summary>And the parties are the same parties.</summary>
    [Fact]
    public void AndTheSameTwoPartiesAreNamed()
    {
        (XElement Ours, XElement Theirs) both = Migrate();

        foreach (string party in (string[])["SellerTradeParty", "BuyerTradeParty"])
        {
            Party(both.Ours, party).ShouldBe(Party(both.Theirs, party), party);
        }
    }

    /// <summary>And every line comes across, with its quantity and its net amount.</summary>
    [Fact]
    public void AndEveryLineComesAcrossWithItsQuantityAndAmount()
    {
        (XElement Ours, XElement Theirs) both = Migrate();

        Lines(both.Ours).ShouldBe(Lines(both.Theirs));
    }

    /// <summary>
    /// The one thing a migration must decide for itself: what the document now claims to conform to.
    /// </summary>
    /// <remarks>
    /// The reader keeps what the 2013 document declared, because that is what it said. Written forward
    /// unchanged, the result is a CII document claiming a ZUGFeRD 1.0 profile — which no CII validator knows
    /// and which the document is not in. mustangproject's converter rewrites it to EN 16931 silently; this
    /// library will not invent a conformance claim, so the caller states the target. This test is here to
    /// say so, because it is the one step of a migration that cannot be done for you.
    /// </remarks>
    [Fact]
    public void TheProfileItNowClaimsIsTheCallersToState()
    {
        string? source = Zugferd1Corpus.Find("ZUGFeRD1_COMFORT_Einfach.xml");
        Assert.SkipWhen(source is null, "run build/fetch-specs.sh zugferd1");

        EInvoice invoice = Zugferd1Corpus.Reader().Read(File.ReadAllText(source!)).Value.ShouldNotBeNull();

        invoice.SpecificationIdentifier.Value.ShouldBe(
            "urn:ferd:CrossIndustryDocument:invoice:1p0:comfort",
            "the reader reports what the document said, not what it ought to say");

        invoice.SpecificationIdentifier = KnownProfiles.En16931Cii.Id;
        string written = new CiiInvoiceWriter().WriteToString(invoice);

        Value(XDocument.Parse(written).Root!, "ID").ShouldBe(KnownProfiles.En16931Cii.Id.Value);
    }

    private static (XElement Ours, XElement Theirs) Migrate()
    {
        string? source = Zugferd1Corpus.Find("ZUGFeRD1_COMFORT_Einfach.xml");
        string reference = Path.Combine(Zugferd1Corpus.Root, "reference", "ZUGFeRD2_COMFORT_Einfach.xml");

        Assert.SkipWhen(source is null || !File.Exists(reference), "run build/fetch-specs.sh zugferd1");

        EInvoice invoice = Zugferd1Corpus.Reader().Read(File.ReadAllText(source!)).Value.ShouldNotBeNull();
        string written = new CiiInvoiceWriter().WriteToString(invoice);

        return (XDocument.Parse(written).Root!, XDocument.Load(reference).Root!);
    }

    /// <summary>A term of the exchanged document, which is where the invoice's own number and type sit.</summary>
    private static string? Header(XElement root, string localName) =>
        root.Elements()
            .First(e => e.Name.LocalName == "ExchangedDocument")
            .Elements(Ram + localName)
            .FirstOrDefault()
            ?.Value.Trim();

    private static string? Value(XElement root, string localName) =>
        root.Descendants().FirstOrDefault(e => e.Name.LocalName == localName)?.Value.Trim();

    private static decimal? Amount(XElement root, string localName) =>
        root.Descendants(Ram + "SpecifiedTradeSettlementHeaderMonetarySummation")
            .Elements(Ram + localName)
            .Select(e => decimal.Parse(e.Value.Trim(), CultureInfo.InvariantCulture))
            .Cast<decimal?>()
            .FirstOrDefault();

    private static string? Party(XElement root, string localName) =>
        root.Descendants(Ram + localName).Elements(Ram + "Name").FirstOrDefault()?.Value.Trim();

    private static IReadOnlyList<string> Lines(XElement root) =>
    [
        .. root.Descendants(Ram + "IncludedSupplyChainTradeLineItem")
            .Select(line => string.Join(
                '|',
                line.Descendants(Ram + "BilledQuantity").FirstOrDefault()?.Value.Trim(),
                line.Descendants(Ram + "LineTotalAmount").FirstOrDefault()?.Value.Trim())),
    ];
}
