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
/// The official corpus contains no element outside EN 16931, so it cannot prove that unmapped content is
/// kept where it belongs. These documents are hand-made for exactly that.
/// </summary>
public class ExtensionDataTests
{
    private const string Acme = "urn:acme:invoice:1p0";

    private static CiiInvoiceReader Reader() =>
        new(new EInvoicingOptions(), new ProfileResolver(new ProfileRegistry(KnownProfiles.All)));

    private static string InvoiceWithNestedExtensions() =>
        $"""
        <rsm:CrossIndustryInvoice xmlns:rsm="{CiiNames.Rsm}" xmlns:ram="{CiiNames.Ram}"
                                  xmlns:udt="{CiiNames.Udt}" xmlns:acme="{Acme}">
          <rsm:ExchangedDocumentContext>
            <ram:GuidelineSpecifiedDocumentContextParameter>
              <ram:ID>urn:cen.eu:en16931:2017</ram:ID>
            </ram:GuidelineSpecifiedDocumentContextParameter>
          </rsm:ExchangedDocumentContext>
          <rsm:ExchangedDocument>
            <ram:ID>FA-1</ram:ID>
            <ram:TypeCode>380</ram:TypeCode>
            <ram:IssueDateTime><udt:DateTimeString format="102">20260829</udt:DateTimeString></ram:IssueDateTime>
          </rsm:ExchangedDocument>
          <rsm:SupplyChainTradeTransaction>
            <ram:IncludedSupplyChainTradeLineItem>
              <ram:AssociatedDocumentLineDocument><ram:LineID>1</ram:LineID></ram:AssociatedDocumentLineDocument>
              <ram:SpecifiedTradeProduct>
                <ram:Name>Consulting</ram:Name>
                <acme:ItemLevelNote>on the item</acme:ItemLevelNote>
              </ram:SpecifiedTradeProduct>
              <acme:LineLevelNote>on the line</acme:LineLevelNote>
            </ram:IncludedSupplyChainTradeLineItem>
            <ram:ApplicableHeaderTradeSettlement>
              <ram:InvoiceCurrencyCode>EUR</ram:InvoiceCurrencyCode>
            </ram:ApplicableHeaderTradeSettlement>
          </rsm:SupplyChainTradeTransaction>
          <acme:DocumentLevelNote>at the document</acme:DocumentLevelNote>
        </rsm:CrossIndustryInvoice>
        """;

    [Fact]
    public void UnmappedElementsAreKeptOnTheNodeThatContainedThem()
    {
        EInvoice invoice = Reader().Read(InvoiceWithNestedExtensions()).Value!;

        invoice.Extensions.Named(Acme, "DocumentLevelNote").ShouldHaveSingleItem();
        invoice.Lines[0].Extensions.Named(Acme, "LineLevelNote").ShouldHaveSingleItem();
        invoice.Lines[0].Item!.Extensions.Named(Acme, "ItemLevelNote").ShouldHaveSingleItem();
    }

    [Fact]
    public void EachOneIsWrittenBackInsideTheElementItCameFrom()
    {
        EInvoice invoice = Reader().Read(InvoiceWithNestedExtensions()).Value!;

        XElement written = XElement.Parse(new CiiInvoiceWriter().WriteToString(invoice));

        Parent(written, "DocumentLevelNote").ShouldBe("CrossIndustryInvoice");
        Parent(written, "LineLevelNote").ShouldBe("IncludedSupplyChainTradeLineItem");
        Parent(written, "ItemLevelNote").ShouldBe("SpecifiedTradeProduct");
    }

    private static string Parent(XElement root, string localName) =>
        root.Descendants(XName.Get(localName, Acme)).Single().Parent!.Name.LocalName;
}
