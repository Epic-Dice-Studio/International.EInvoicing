namespace International.EInvoicing.Playground.Services;

/// <summary>Documents the site offers so a visitor has something to try immediately.</summary>
public static class Samples
{
    /// <summary>A small UBL invoice that satisfies EN 16931.</summary>
    public const string MinimalUblInvoice = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
                 xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
                 xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2">
          <cbc:CustomizationID>urn:cen.eu:en16931:2017</cbc:CustomizationID>
          <cbc:ID>FA-2026-001</cbc:ID>
          <cbc:IssueDate>2026-08-30</cbc:IssueDate>
          <cbc:DueDate>2026-09-29</cbc:DueDate>
          <cbc:InvoiceTypeCode>380</cbc:InvoiceTypeCode>
          <cbc:DocumentCurrencyCode>EUR</cbc:DocumentCurrencyCode>
          <cac:AccountingSupplierParty>
            <cac:Party>
              <cac:PartyName><cbc:Name>Epic Dice Studio</cbc:Name></cac:PartyName>
              <cac:PostalAddress>
                <cbc:StreetName>1 rue de la Facture</cbc:StreetName>
                <cbc:CityName>Angers</cbc:CityName>
                <cbc:PostalZone>49000</cbc:PostalZone>
                <cac:Country><cbc:IdentificationCode>FR</cbc:IdentificationCode></cac:Country>
              </cac:PostalAddress>
              <cac:PartyTaxScheme>
                <cbc:CompanyID>FR12345678901</cbc:CompanyID>
                <cac:TaxScheme><cbc:ID>VAT</cbc:ID></cac:TaxScheme>
              </cac:PartyTaxScheme>
              <cac:PartyLegalEntity><cbc:RegistrationName>Epic Dice Studio</cbc:RegistrationName></cac:PartyLegalEntity>
            </cac:Party>
          </cac:AccountingSupplierParty>
          <cac:AccountingCustomerParty>
            <cac:Party>
              <cac:PartyName><cbc:Name>Acme</cbc:Name></cac:PartyName>
              <cac:PostalAddress>
                <cbc:CityName>Nantes</cbc:CityName>
                <cac:Country><cbc:IdentificationCode>FR</cbc:IdentificationCode></cac:Country>
              </cac:PostalAddress>
              <cac:PartyLegalEntity><cbc:RegistrationName>Acme</cbc:RegistrationName></cac:PartyLegalEntity>
            </cac:Party>
          </cac:AccountingCustomerParty>
          <cac:PaymentMeans>
            <cbc:PaymentMeansCode>30</cbc:PaymentMeansCode>
            <cac:PayeeFinancialAccount><cbc:ID>FR7630001007941234567890185</cbc:ID></cac:PayeeFinancialAccount>
          </cac:PaymentMeans>
          <cac:TaxTotal>
            <cbc:TaxAmount currencyID="EUR">90.00</cbc:TaxAmount>
            <cac:TaxSubtotal>
              <cbc:TaxableAmount currencyID="EUR">450.00</cbc:TaxableAmount>
              <cbc:TaxAmount currencyID="EUR">90.00</cbc:TaxAmount>
              <cac:TaxCategory>
                <cbc:ID>S</cbc:ID>
                <cbc:Percent>20</cbc:Percent>
                <cac:TaxScheme><cbc:ID>VAT</cbc:ID></cac:TaxScheme>
              </cac:TaxCategory>
            </cac:TaxSubtotal>
          </cac:TaxTotal>
          <cac:LegalMonetaryTotal>
            <cbc:LineExtensionAmount currencyID="EUR">450.00</cbc:LineExtensionAmount>
            <cbc:TaxExclusiveAmount currencyID="EUR">450.00</cbc:TaxExclusiveAmount>
            <cbc:TaxInclusiveAmount currencyID="EUR">540.00</cbc:TaxInclusiveAmount>
            <cbc:PayableAmount currencyID="EUR">540.00</cbc:PayableAmount>
          </cac:LegalMonetaryTotal>
          <cac:InvoiceLine>
            <cbc:ID>1</cbc:ID>
            <cbc:InvoicedQuantity unitCode="HUR">3</cbc:InvoicedQuantity>
            <cbc:LineExtensionAmount currencyID="EUR">450.00</cbc:LineExtensionAmount>
            <cac:Item>
              <cbc:Name>Consulting</cbc:Name>
              <cac:ClassifiedTaxCategory>
                <cbc:ID>S</cbc:ID>
                <cbc:Percent>20</cbc:Percent>
                <cac:TaxScheme><cbc:ID>VAT</cbc:ID></cac:TaxScheme>
              </cac:ClassifiedTaxCategory>
            </cac:Item>
            <cac:Price><cbc:PriceAmount currencyID="EUR">150.00</cbc:PriceAmount></cac:Price>
          </cac:InvoiceLine>
        </Invoice>
        """;

    /// <summary>An invoice whose profile this library does not implement, to show what that looks like.</summary>
    public static string UnknownProfileInvoice =>
        MinimalUblInvoice.Replace(
            "<cbc:CustomizationID>urn:cen.eu:en16931:2017</cbc:CustomizationID>",
            "<cbc:CustomizationID>urn:acme:profile:2p0</cbc:CustomizationID>",
            StringComparison.Ordinal);
}
