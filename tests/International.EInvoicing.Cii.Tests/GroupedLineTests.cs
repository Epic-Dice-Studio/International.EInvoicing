using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Cii.Tests;

/// <summary>
/// Invoices whose lines are grouped, which EN 16931 has no term for and Factur-X EXTENDED does.
/// </summary>
/// <remarks>
/// <para>
/// The hierarchy is expressed by <em>reference</em>, not by nesting: the lines stay a flat list and each
/// child names its parent's line number, with <c>ram:LineStatusReasonCode</c> saying whether a line is a
/// <c>GROUP</c> heading, a <c>DETAIL</c> to charge for, or <c>INFORMATION</c> to display.
/// </para>
/// <para>
/// A reader that ignores it gets every line and no structure — which is worse than it sounds, because a
/// group heading's amount is the sum of its children, so adding every line up counts those amounts twice.
/// </para>
/// </remarks>
public class GroupedLineTests
{
    private static readonly EInvoicingOptions Options = new();

    [Fact]
    public void AGroupedInvoiceIsReadWithItsStructureIntact()
    {
        EInvoice invoice = Read(AGroupedInvoice());

        invoice.Lines.Count.ShouldBe(3);

        InvoiceLine heading = invoice.Lines[0];
        heading.LineStatusReasonCode.Value.ShouldBe(LineStatusReasonCodes.Group);
        heading.ParentLineIdentifier.IsSet.ShouldBeFalse();

        invoice.Lines[1].ParentLineIdentifier.Value.ShouldBe("1");
        invoice.Lines[2].ParentLineIdentifier.Value.ShouldBe("1");
        invoice.Lines[1].LineStatusReasonCode.Value.ShouldBe(LineStatusReasonCodes.Detail);
    }

    /// <summary>
    /// The arithmetic the structure exists for: a heading's amount is already the sum of its children.
    /// </summary>
    [Fact]
    public void AndTheGroupHeadingIsNotCountedTwice()
    {
        EInvoice invoice = Read(AGroupedInvoice());

        decimal charged = invoice.Lines
            .Where(line => LineStatusReasonCodes.IsCharged(line.LineStatusReasonCode.Value))
            .Sum(line => line.NetAmount.Value ?? 0m);

        charged.ShouldBe(300m);
        invoice.Lines.Sum(line => line.NetAmount.Value ?? 0m).ShouldBe(600m, "every line added up double-counts");
    }

    /// <summary>An invoice that says nothing about grouping still has lines that are charged for.</summary>
    [Fact]
    public void ALineThatSaysNothingIsALineToCharge() =>
        LineStatusReasonCodes.IsCharged(null).ShouldBeTrue();

    [Fact]
    public void AndTheStructureSurvivesBeingWrittenBack()
    {
        EInvoice read = Read(new CiiInvoiceWriter().WriteToString(Read(AGroupedInvoice())));

        read.Lines[0].LineStatusReasonCode.Value.ShouldBe(LineStatusReasonCodes.Group);
        read.Lines[1].ParentLineIdentifier.Value.ShouldBe("1");
        read.Lines[2].ParentLineIdentifier.Value.ShouldBe("1");
    }

    private static EInvoice Read(string xml) =>
        new CiiInvoiceReader(Options, new ProfileResolver(new ProfileRegistry(KnownProfiles.All)))
            .Read(xml).Value.ShouldNotBeNull();

    /// <summary>
    /// A heading with two details under it, in the shape Factur-X EXTENDED prescribes.
    /// </summary>
    private static string AGroupedInvoice() =>
        $"""
        <rsm:CrossIndustryInvoice xmlns:rsm="{CiiNames.Rsm}" xmlns:ram="{CiiNames.Ram}" xmlns:udt="{CiiNames.Udt}">
          <rsm:ExchangedDocumentContext>
            <ram:GuidelineSpecifiedDocumentContextParameter>
              <ram:ID>{KnownProfiles.FacturXExtended.Id.Value}</ram:ID>
            </ram:GuidelineSpecifiedDocumentContextParameter>
          </rsm:ExchangedDocumentContext>
          <rsm:ExchangedDocument>
            <ram:ID>2026-0007</ram:ID>
            <ram:TypeCode>380</ram:TypeCode>
            <ram:IssueDateTime><udt:DateTimeString format="102">20260903</udt:DateTimeString></ram:IssueDateTime>
          </rsm:ExchangedDocument>
          <rsm:SupplyChainTradeTransaction>
            {Line("1", parent: null, LineStatusReasonCodes.Group, "Groundworks", 300)}
            {Line("2", parent: "1", LineStatusReasonCodes.Detail, "Excavation", 200)}
            {Line("3", parent: "1", LineStatusReasonCodes.Detail, "Backfill", 100)}
            <ram:ApplicableHeaderTradeAgreement/>
            <ram:ApplicableHeaderTradeDelivery/>
            <ram:ApplicableHeaderTradeSettlement>
              <ram:InvoiceCurrencyCode>EUR</ram:InvoiceCurrencyCode>
            </ram:ApplicableHeaderTradeSettlement>
          </rsm:SupplyChainTradeTransaction>
        </rsm:CrossIndustryInvoice>
        """;

    private static string Line(string id, string? parent, string reason, string name, decimal amount) =>
        $"""
        <ram:IncludedSupplyChainTradeLineItem>
              <ram:AssociatedDocumentLineDocument>
                <ram:LineID>{id}</ram:LineID>
                {(parent is null ? string.Empty : $"<ram:ParentLineID>{parent}</ram:ParentLineID>")}
                <ram:LineStatusReasonCode>{reason}</ram:LineStatusReasonCode>
              </ram:AssociatedDocumentLineDocument>
              <ram:SpecifiedTradeProduct><ram:Name>{name}</ram:Name></ram:SpecifiedTradeProduct>
              <ram:SpecifiedLineTradeAgreement/>
              <ram:SpecifiedLineTradeDelivery/>
              <ram:SpecifiedLineTradeSettlement>
                <ram:SpecifiedTradeSettlementLineMonetarySummation>
                  <ram:LineTotalAmount>{amount}</ram:LineTotalAmount>
                </ram:SpecifiedTradeSettlementLineMonetarySummation>
              </ram:SpecifiedLineTradeSettlement>
            </ram:IncludedSupplyChainTradeLineItem>
        """;
}
