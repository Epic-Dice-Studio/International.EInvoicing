using System.Xml.Linq;
using International.EInvoicing.Configuration;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl.Reading;
using International.EInvoicing.Ubl.Writing;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Ubl.Tests.Writing;

/// <summary>
/// The official corpus contains no element outside EN 16931, so it cannot prove that unmapped content is
/// kept where it belongs. These documents are hand-made for exactly that: they test this library's behaviour,
/// not conformance to a norm.
/// </summary>
public class ExtensionDataTests
{
    private const string Acme = "urn:acme:invoice:1p0";

    private static UblInvoiceReader Reader() =>
        new(new EInvoicingOptions(), new ProfileResolver(new ProfileRegistry(KnownProfiles.All)));

    private static string InvoiceWithNestedExtensions() =>
        $"""
        <ubl:Invoice xmlns:ubl="{UblNames.Invoice}" xmlns:cac="{UblNames.Cac}" xmlns:cbc="{UblNames.Cbc}"
                     xmlns:acme="{Acme}">
          <cbc:CustomizationID>urn:cen.eu:en16931:2017</cbc:CustomizationID>
          <cbc:ID>FA-1</cbc:ID>
          <cbc:IssueDate>2026-08-29</cbc:IssueDate>
          <cbc:DocumentCurrencyCode>EUR</cbc:DocumentCurrencyCode>
          <acme:DocumentLevelNote>at the document</acme:DocumentLevelNote>
          <cac:InvoiceLine>
            <cbc:ID>1</cbc:ID>
            <cbc:LineExtensionAmount currencyID="EUR">100.00</cbc:LineExtensionAmount>
            <acme:LineLevelNote>on the line</acme:LineLevelNote>
            <cac:Item>
              <cbc:Name>Consulting</cbc:Name>
              <acme:ItemLevelNote>on the item</acme:ItemLevelNote>
            </cac:Item>
          </cac:InvoiceLine>
        </ubl:Invoice>
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

        XElement written = XElement.Parse(new UblInvoiceWriter().WriteToString(invoice));

        Parent(written, "DocumentLevelNote").ShouldBe("Invoice");
        Parent(written, "LineLevelNote").ShouldBe("InvoiceLine");
        Parent(written, "ItemLevelNote").ShouldBe("Item");
    }

    [Fact]
    public void KeepingAnElementIsReportedRatherThanSilent()
    {
        IReadOnlyList<Diagnostics.Diagnostic> diagnostics =
            Reader().Read(InvoiceWithNestedExtensions()).Diagnostics;

        diagnostics.Count(d => d.Code == "EIV2020").ShouldBe(3);
    }

    private static string Parent(XElement root, string localName) =>
        root.Descendants(XName.Get(localName, Acme)).Single().Parent!.Name.LocalName;
}
