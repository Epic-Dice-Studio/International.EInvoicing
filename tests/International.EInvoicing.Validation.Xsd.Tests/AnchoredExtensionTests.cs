using System.Xml.Linq;
using International.EInvoicing.Model;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.Xsd.Tests;

/// <summary>
/// An element nobody mapped is written back where it was read from.
/// </summary>
/// <remarks>
/// Keeping unmapped content verbatim is only half of "nothing is lost": UBL's element order is normative, so
/// content kept in the wrong place is content a receiver's parser rejects. Every case in the official
/// corpora turned out to be a term the reader did not map, and mapping them left nothing to misplace — but a
/// national extension nobody here models has nowhere to be moved to, and this is what happens to it.
/// </remarks>
public class AnchoredExtensionTests
{
    private static readonly EInvoicing Library =
        EInvoicing.Create(builder => builder.AddDefaults().AddUblSchema());

    /// <summary>
    /// A foreign element in the middle of a document comes back in the middle of it.
    /// </summary>
    /// <remarks>
    /// <c>cbc:BuyerReference</c> is deliberately not the point: the element used here is one UBL declares in
    /// the invoice sequence and this library does map, so the fixture instead carries an element from
    /// another namespace, which is what a national extension actually looks like.
    /// </remarks>
    [Fact]
    public void AForeignElementComesBackWhereItWas()
    {
        string written = Library.Write(Library.Read(WithForeignElement()).RequireInvoice(), DocumentFormat.Ubl);

        written.ShouldContain("acme:LocalReference");

        int foreign = written.IndexOf("acme:LocalReference", StringComparison.Ordinal);
        int seller = written.IndexOf("AccountingSupplierParty", StringComparison.Ordinal);

        foreign.ShouldBeLessThan(seller, "it was read before the seller and belongs before the seller");
    }

    /// <summary>And the document the schema accepted is still one the schema accepts.</summary>
    [Fact]
    public void AndTheDocumentIsStillOneTheSchemaAccepts()
    {
        string xml = WithForeignElement();

        ValidationReport before = Library.Validate(xml);
        ValidationReport after = Library.Validate(
            Library.Write(Library.Read(xml).RequireInvoice(), DocumentFormat.Ubl));

        after.Errors.Count().ShouldBe(
            before.Errors.Count(),
            string.Join(Environment.NewLine, after.Errors.Select(error => error.ToString())));
    }

    /// <summary>
    /// A foreign element inside a party comes back inside that party, not at the end of the invoice.
    /// </summary>
    /// <remarks>
    /// This is the half that needs the reader: an element is only anchored to a sibling if the node holding
    /// it is the node that sibling belongs to. Read naively, everything unmapped bubbles up to the invoice,
    /// where the nearest thing it can follow is a party — so it lands outside the party it was written in.
    /// </remarks>
    [Fact]
    public void AndOneInsideAPartyComesBackInsideThatParty()
    {
        XDocument written = Written(WithNestedForeignElements());

        ParentOf(written, "Department").ShouldBe("Party");
        ParentOf(written, "Sector").ShouldBe("PostalAddress");
    }

    /// <summary>And inside its party it keeps the place it had, which is what the schema judges.</summary>
    [Fact]
    public void AndKeepsItsPlaceAmongTheElementsItWasWrittenBetween()
    {
        XDocument written = Written(WithNestedForeignElements());

        Follows(written, "Department").ShouldBe("PartyName");
        Follows(written, "Sector").ShouldBe("CityName");
    }

    private static XDocument Written(string xml) =>
        XDocument.Parse(Library.Write(Library.Read(xml).RequireInvoice(), DocumentFormat.Ubl));

    private static XElement Foreign(XDocument document, string localName) =>
        document.Descendants(XNamespace.Get("urn:acme:national:1.0") + localName).ShouldHaveSingleItem();

    private static string? ParentOf(XDocument document, string localName) =>
        Foreign(document, localName).Parent?.Name.LocalName;

    private static string? Follows(XDocument document, string localName) =>
        Foreign(document, localName).ElementsBeforeSelf().LastOrDefault()?.Name.LocalName;

    /// <summary>
    /// An invoice with a national extension where such things really sit: inside the document, before the
    /// parties, in a namespace this library knows nothing about.
    /// </summary>
    private static string WithForeignElement() =>
        """
        <Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
                 xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
                 xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"
                 xmlns:acme="urn:acme:national:1.0">
          <cbc:CustomizationID>urn:cen.eu:en16931:2017</cbc:CustomizationID>
          <cbc:ID>2026-0001</cbc:ID>
          <cbc:IssueDate>2026-09-03</cbc:IssueDate>
          <cbc:InvoiceTypeCode>380</cbc:InvoiceTypeCode>
          <cbc:DocumentCurrencyCode>EUR</cbc:DocumentCurrencyCode>
          <acme:LocalReference>A-1</acme:LocalReference>
          <cac:AccountingSupplierParty>
            <cac:Party>
              <cac:PartyLegalEntity><cbc:RegistrationName>Vendeur SAS</cbc:RegistrationName></cac:PartyLegalEntity>
            </cac:Party>
          </cac:AccountingSupplierParty>
          <cac:AccountingCustomerParty>
            <cac:Party>
              <cac:PartyLegalEntity><cbc:RegistrationName>Acheteur GmbH</cbc:RegistrationName></cac:PartyLegalEntity>
            </cac:Party>
          </cac:AccountingCustomerParty>
          <cac:LegalMonetaryTotal>
            <cbc:PayableAmount currencyID="EUR">100.00</cbc:PayableAmount>
          </cac:LegalMonetaryTotal>
        </Invoice>
        """;

    /// <summary>
    /// The same, one level down: a national extension inside the seller and another inside its address,
    /// each between two elements this library does map.
    /// </summary>
    private static string WithNestedForeignElements() =>
        """
        <Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
                 xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
                 xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"
                 xmlns:acme="urn:acme:national:1.0">
          <cbc:CustomizationID>urn:cen.eu:en16931:2017</cbc:CustomizationID>
          <cbc:ID>2026-0002</cbc:ID>
          <cbc:IssueDate>2026-09-03</cbc:IssueDate>
          <cbc:InvoiceTypeCode>380</cbc:InvoiceTypeCode>
          <cbc:DocumentCurrencyCode>EUR</cbc:DocumentCurrencyCode>
          <cac:AccountingSupplierParty>
            <cac:Party>
              <cac:PartyName><cbc:Name>Vendeur</cbc:Name></cac:PartyName>
              <acme:Department>Ventes</acme:Department>
              <cac:PostalAddress>
                <cbc:CityName>Lyon</cbc:CityName>
                <acme:Sector>7A</acme:Sector>
                <cac:Country><cbc:IdentificationCode>FR</cbc:IdentificationCode></cac:Country>
              </cac:PostalAddress>
              <cac:PartyLegalEntity><cbc:RegistrationName>Vendeur SAS</cbc:RegistrationName></cac:PartyLegalEntity>
            </cac:Party>
          </cac:AccountingSupplierParty>
          <cac:AccountingCustomerParty>
            <cac:Party>
              <cac:PartyLegalEntity><cbc:RegistrationName>Acheteur GmbH</cbc:RegistrationName></cac:PartyLegalEntity>
            </cac:Party>
          </cac:AccountingCustomerParty>
          <cac:LegalMonetaryTotal>
            <cbc:PayableAmount currencyID="EUR">100.00</cbc:PayableAmount>
          </cac:LegalMonetaryTotal>
        </Invoice>
        """;
}
