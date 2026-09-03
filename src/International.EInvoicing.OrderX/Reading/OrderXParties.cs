using System.Xml.Linq;
using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Model;
using International.EInvoicing.Values;
using static International.EInvoicing.OrderX.Reading.OrderXNodes;

namespace International.EInvoicing.OrderX.Reading;

/// <summary>
/// The parts of an Order-X document that read the same wherever they appear: a party, its address, its
/// contact, a period, an allowance, a referenced document.
/// </summary>
/// <remarks>
/// Order-X states a party in seven places — seller, buyer, requisitioner, ship-to, ship-from, invoicee, and
/// again on a line — with the same children each time, so reading it once is what keeps the three readers
/// about their own documents.
/// </remarks>
internal static class OrderXParties
{
    private static XNamespace Ram => OrderXNames.Ram;

    public static Party? ReadParty(
        XElement? element,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return null;
        }

        var party = new Party
        {
            Name = values.ReadText(In(values, element, Ram + "Name")),
            AdditionalLegalInformation = values.ReadText(In(values, element, Ram + "Description")),
            ElectronicAddress = values.ReadIdentifier(
                In(values, In(values, element, Ram + "URIUniversalCommunication"), Ram + "URIID")),
            Address = ReadAddress(In(values, element, Ram + "PostalTradeAddress"), values, owners),
            Contact = ReadContact(In(values, element, Ram + "DefinedTradeContact"), values, owners),
        };

        foreach (XElement identifier in AllIn(values, element, Ram + "ID"))
        {
            party.Identifiers.Add(values.ReadIdentifier(identifier));
        }

        foreach (XElement identifier in AllIn(values, element, Ram + "GlobalID"))
        {
            party.Identifiers.Add(values.ReadIdentifier(identifier));
        }

        if (In(values, element, Ram + "SpecifiedLegalOrganization") is { } legal)
        {
            party.LegalRegistrationIdentifier = values.ReadIdentifier(In(values, legal, Ram + "ID"));
            party.TradingName = values.ReadText(In(values, legal, Ram + "TradingBusinessName"));
        }

        foreach (XElement registration in AllIn(values, element, Ram + "SpecifiedTaxRegistration"))
        {
            IdentifierField field = values.ReadIdentifier(In(values, registration, Ram + "ID"));

            if (string.Equals(field.SchemeId, "VA", StringComparison.OrdinalIgnoreCase))
            {
                party.VatIdentifier = field;
            }
            else
            {
                party.TaxRegistrationIdentifier = field;
            }
        }

        owners[element] = party;
        return party;
    }

    public static PostalAddress? ReadAddress(
        XElement? element,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return null;
        }

        var address = new PostalAddress
        {
            Line1 = values.ReadText(In(values, element, Ram + "LineOne")),
            Line2 = values.ReadText(In(values, element, Ram + "LineTwo")),
            Line3 = values.ReadText(In(values, element, Ram + "LineThree")),
            City = values.ReadText(In(values, element, Ram + "CityName")),
            PostCode = values.ReadText(In(values, element, Ram + "PostcodeCode")),
            CountrySubdivision = values.ReadText(In(values, element, Ram + "CountrySubDivisionName")),
            CountryCode = values.ReadCode(In(values, element, Ram + "CountryID")),
        };

        owners[element] = address;
        return address;
    }

    public static Contact? ReadContact(
        XElement? element,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return null;
        }

        var contact = new Contact
        {
            Name = values.ReadText(In(values, element, Ram + "PersonName")),
            Department = values.ReadText(In(values, element, Ram + "DepartmentName")),
            TypeCode = values.ReadCode(In(values, element, Ram + "TypeCode")),
            Telephone = values.ReadText(
                In(values, In(values, element, Ram + "TelephoneUniversalCommunication"), Ram + "CompleteNumber")),
            Email = values.ReadText(
                In(values, In(values, element, Ram + "EmailURIUniversalCommunication"), Ram + "URIID")),
        };

        owners[element] = contact;
        return contact;
    }

    public static InvoicingPeriod? ReadPeriod(XElement? element, CiiValueReader values) =>
        element is null
            ? null
            : new InvoicingPeriod
            {
                StartDate = values.ReadDate(In(values, element, Ram + "StartDateTime")),
                EndDate = values.ReadDate(In(values, element, Ram + "EndDateTime")),
            };

    public static AllowanceCharge ReadAllowanceCharge(
        XElement element,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        XElement? category = In(values, element, Ram + "CategoryTradeTax");

        var allowanceCharge = new AllowanceCharge
        {
            IsCharge = values.ReadIndicator(In(values, element, Ram + "ChargeIndicator")).Value ?? false,
            Amount = values.ReadAmount(In(values, element, Ram + "ActualAmount")),
            BaseAmount = values.ReadAmount(In(values, element, Ram + "BasisAmount")),
            Percentage = values.ReadDecimal(In(values, element, Ram + "CalculationPercent")),
            Reason = values.ReadText(In(values, element, Ram + "Reason")),
            ReasonCode = values.ReadCode(In(values, element, Ram + "ReasonCode")),
            VatCategoryCode = values.ReadCode(In(values, category, Ram + "CategoryCode")),
            VatRate = values.ReadDecimal(In(values, category, Ram + "RateApplicablePercent")),
        };

        // The tax type is always VAT and is written back from nothing, so it has to be marked as read or it
        // comes back as extension data inside an element that allows no such child.
        values.Consume(category?.Element(Ram + "TypeCode"));

        owners[element] = allowanceCharge;
        return allowanceCharge;
    }

    /// <summary>
    /// Reads a <c>ReferencedDocument</c> in full. Order-X states a dozen of them, most carrying only the
    /// issuer's identifier, but the additional ones carry a type, a name and a URI as well.
    /// </summary>
    public static AdditionalDocument ReadDocument(
        XElement element,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        var document = new AdditionalDocument
        {
            Identifier = values.ReadIdentifier(In(values, element, Ram + "IssuerAssignedID")),
            ExternalLocation = values.ReadText(In(values, element, Ram + "URIID")),
            LineReference = values.ReadIdentifier(In(values, element, Ram + "LineID")),
            TypeCode = values.ReadCode(In(values, element, Ram + "TypeCode")),
            Description = values.ReadText(In(values, element, Ram + "Name")),
            ReferenceTypeCode = values.ReadCode(In(values, element, Ram + "ReferenceTypeCode")),
        };

        owners[element] = document;
        return document;
    }

    /// <summary>The issuer's identifier of a reference that carries nothing else.</summary>
    public static IdentifierField ReadReference(CiiValueReader values, XElement? parent, string localName) =>
        values.ReadIdentifier(In(values, In(values, parent, Ram + localName), Ram + "IssuerAssignedID"));
}
