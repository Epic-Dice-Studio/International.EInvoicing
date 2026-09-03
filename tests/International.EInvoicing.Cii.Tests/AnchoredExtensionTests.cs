using System.Xml.Linq;
using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.Configuration;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Cii.Tests;

/// <summary>
/// An element nobody mapped is written back where it was read from.
/// </summary>
/// <remarks>
/// CII's element order is normative, exactly as UBL's is, so content kept in the wrong place is content a
/// receiver's parser rejects. Every unmapped element in the official corpora turned out to be a term the
/// reader could be taught, and teaching it left nothing to misplace — but a national extension nobody here
/// models has nowhere to be moved to, and this is what happens to it.
/// </remarks>
public class AnchoredExtensionTests
{
    private static readonly XNamespace Acme = "urn:acme:national:1.0";

    private static EInvoicingOptions Options { get; } = new();

    /// <summary>A foreign element in the middle of the document comes back in the middle of it.</summary>
    [Fact]
    public void AForeignElementComesBackWhereItWas()
    {
        XElement written = WriteBack(WithForeignElements());

        XElement foreign = written.Descendants(Acme + "Reference").ShouldHaveSingleItem();

        foreign.Parent!.Name.LocalName.ShouldBe("ExchangedDocument");
        foreign.ElementsBeforeSelf().Last().Name.LocalName.ShouldBe("TypeCode");
    }

    /// <summary>
    /// And one inside a trade product comes back inside that product, which is the half that needs the
    /// reader: everything unmapped used to bubble up to the invoice, where the item's siblings are not.
    /// </summary>
    [Fact]
    public void AndOneInsideAnItemComesBackInsideThatItem()
    {
        XElement written = WriteBack(WithForeignElements());

        XElement foreign = written.Descendants(Acme + "Grade").ShouldHaveSingleItem();

        foreign.Parent!.Name.LocalName.ShouldBe("SpecifiedTradeProduct");
        foreign.ElementsBeforeSelf().Last().Name.LocalName.ShouldBe("Name");
    }

    private static XElement WriteBack(string xml)
    {
        EInvoice invoice = new CiiInvoiceReader(Options, new ProfileResolver(new ProfileRegistry(KnownProfiles.All)))
            .Read(xml).Value.ShouldNotBeNull();

        return XDocument.Parse(new CiiInvoiceWriter().WriteToString(invoice)).Root!;
    }

    /// <summary>
    /// An invoice carrying two national extensions where such things really sit: between elements the reader
    /// does map, in a namespace this library knows nothing about.
    /// </summary>
    private static string WithForeignElements() =>
        $"""
        <rsm:CrossIndustryInvoice xmlns:rsm="{CiiNames.Rsm}" xmlns:ram="{CiiNames.Ram}" xmlns:udt="{CiiNames.Udt}"
                                  xmlns:acme="{Acme}">
          <rsm:ExchangedDocumentContext>
            <ram:GuidelineSpecifiedDocumentContextParameter>
              <ram:ID>{KnownProfiles.En16931Cii.Id.Value}</ram:ID>
            </ram:GuidelineSpecifiedDocumentContextParameter>
          </rsm:ExchangedDocumentContext>
          <rsm:ExchangedDocument>
            <ram:ID>2026-0001</ram:ID>
            <ram:TypeCode>380</ram:TypeCode>
            <acme:Reference>A-1</acme:Reference>
            <ram:IssueDateTime><udt:DateTimeString format="102">20260903</udt:DateTimeString></ram:IssueDateTime>
          </rsm:ExchangedDocument>
          <rsm:SupplyChainTradeTransaction>
            <ram:IncludedSupplyChainTradeLineItem>
              <ram:AssociatedDocumentLineDocument><ram:LineID>1</ram:LineID></ram:AssociatedDocumentLineDocument>
              <ram:SpecifiedTradeProduct>
                <ram:Name>Un article</ram:Name>
                <acme:Grade>premier</acme:Grade>
                <ram:OriginTradeCountry><ram:ID>FR</ram:ID></ram:OriginTradeCountry>
              </ram:SpecifiedTradeProduct>
              <ram:SpecifiedLineTradeSettlement>
                <ram:SpecifiedTradeSettlementLineMonetarySummation>
                  <ram:LineTotalAmount>100.00</ram:LineTotalAmount>
                </ram:SpecifiedTradeSettlementLineMonetarySummation>
              </ram:SpecifiedLineTradeSettlement>
            </ram:IncludedSupplyChainTradeLineItem>
            <ram:ApplicableHeaderTradeAgreement/>
            <ram:ApplicableHeaderTradeDelivery/>
            <ram:ApplicableHeaderTradeSettlement>
              <ram:InvoiceCurrencyCode>EUR</ram:InvoiceCurrencyCode>
            </ram:ApplicableHeaderTradeSettlement>
          </rsm:SupplyChainTradeTransaction>
        </rsm:CrossIndustryInvoice>
        """;
}
