using System.Xml.Linq;
using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Model;
using International.EInvoicing.Values;
using static International.EInvoicing.Zugferd1.Reading.Zugferd1InvoiceReader;

namespace International.EInvoicing.Zugferd1.Reading;

/// <summary>
/// The parts of a ZUGFeRD 1.0 invoice that read the same wherever they appear.
/// </summary>
/// <remarks>
/// The vocabulary is the CII one, so a party, an address, a contact and an allowance look almost exactly as
/// they do in D22B. Almost: the tax rate is <c>ram:ApplicablePercent</c> rather than
/// <c>ram:RateApplicablePercent</c>, and a referenced document's identifier is <c>ram:ID</c> rather than
/// <c>ram:IssuerAssignedID</c>. Those two renames are most of what separates the two versions.
/// </remarks>
internal static class Zugferd1Parties
{
    private static XNamespace Ram => Zugferd1Names.Ram;

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
                StartDate = values.ReadDate(In(values, element, Ram + "StartDateTime"), "BT-73"),
                EndDate = values.ReadDate(In(values, element, Ram + "EndDateTime"), "BT-74"),
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
            VatRate = values.ReadDecimal(In(values, category, Ram + "ApplicablePercent")),
        };

        values.Consume(category?.Element(Ram + "TypeCode"));

        owners[element] = allowanceCharge;
        return allowanceCharge;
    }

    public static AdditionalDocument ReadDocument(
        XElement element,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        var document = new AdditionalDocument
        {
            Identifier = values.ReadIdentifier(In(values, element, Ram + "ID")),
            TypeCode = values.ReadCode(In(values, element, Ram + "TypeCode")),
            Description = values.ReadText(In(values, element, Ram + "Name")),
            ReferenceTypeCode = values.ReadCode(In(values, element, Ram + "ReferenceTypeCode")),
        };

        values.Consume(element.Element(Ram + "IssueDateTime"));

        owners[element] = document;
        return document;
    }
}
