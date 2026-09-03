using System.Xml.Linq;
using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Ubl.Reading;

/// <summary>
/// A <c>cac:Party</c> as the post-award documents carry it.
/// </summary>
/// <remarks>
/// The invoice reader maps a party too, and does more with it: which registration is BT-31 depends on the
/// document's tax scheme, which a despatch advice does not have. What is shared is the shape — the endpoint,
/// the identifications, the two names UBL keeps in different elements, the address and the contact.
/// </remarks>
internal static class UblParties
{
    public static Party? Read(
        XElement? element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return null;
        }

        var party = new Party
        {
            ElectronicAddress = values.ReadIdentifier(Take(element, UblNames.Cbc + "EndpointID", mapped)),
        };

        owners[element] = party;

        foreach (XElement identification in TakeAll(element, UblNames.Cac + "PartyIdentification", mapped))
        {
            owners[identification] = party;
            party.Identifiers.Add(values.ReadIdentifier(Take(identification, UblNames.Cbc + "ID", mapped)));
        }

        if (Take(element, UblNames.Cac + "PartyName", mapped) is { } name)
        {
            owners[name] = party;
            party.TradingName = values.ReadText(Take(name, UblNames.Cbc + "Name", mapped));
        }

        if (Take(element, UblNames.Cac + "PartyLegalEntity", mapped) is { } legal)
        {
            owners[legal] = party;
            party.Name = values.ReadText(Take(legal, UblNames.Cbc + "RegistrationName", mapped));
            party.LegalRegistrationIdentifier = values.ReadIdentifier(Take(legal, UblNames.Cbc + "CompanyID", mapped));

            // Where the company is registered, which is not always where it trades from. The model holds one
            // address, so the registration one is kept only when there is no trading address to lose.
            if (Take(legal, UblNames.Cac + "RegistrationAddress", mapped) is { } registration)
            {
                party.RegistrationAddress = ReadAddress(registration, values, mapped, owners);
            }
        }

        // A party may declare several tax registrations; VAT is the one EN 16931 names, and the others are
        // kept apart rather than overwriting it.
        foreach (XElement scheme in TakeAll(element, UblNames.Cac + "PartyTaxScheme", mapped))
        {
            owners[scheme] = party;
            IdentifierField identifier = values.ReadIdentifier(Take(scheme, UblNames.Cbc + "CompanyID", mapped));
            XElement? taxScheme = Take(scheme, UblNames.Cac + "TaxScheme", mapped);
            string? code = Take(taxScheme, UblNames.Cbc + "ID", mapped)?.Value.Trim();

            if (taxScheme is not null)
            {
                owners[taxScheme] = party;
            }

            if (string.Equals(code, "VAT", StringComparison.OrdinalIgnoreCase))
            {
                party.VatIdentifier = identifier;
            }
            else
            {
                party.TaxRegistrationIdentifier = identifier;
                party.TaxRegistrationScheme = new CodeField(code);
            }
        }

        party.Address = ReadAddress(Take(element, UblNames.Cac + "PostalAddress", mapped), values, mapped, owners);

        if (Take(element, UblNames.Cac + "Contact", mapped) is { } contact)
        {
            party.Contact = ReadContact(contact, values, mapped, owners);
        }

        return party;
    }

    public static PostalAddress? ReadAddress(
        XElement? element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return null;
        }

        var address = new PostalAddress
        {
            Line1 = values.ReadText(Take(element, UblNames.Cbc + "StreetName", mapped)),
            Line2 = values.ReadText(Take(element, UblNames.Cbc + "AdditionalStreetName", mapped)),
            City = values.ReadText(Take(element, UblNames.Cbc + "CityName", mapped)),
            PostCode = values.ReadText(Take(element, UblNames.Cbc + "PostalZone", mapped)),
            CountrySubdivision = values.ReadText(Take(element, UblNames.Cbc + "CountrySubentity", mapped)),
        };

        owners[element] = address;

        if (Take(element, UblNames.Cac + "AddressLine", mapped) is { } line)
        {
            owners[line] = address;
            address.Line3 = values.ReadText(Take(line, UblNames.Cbc + "Line", mapped));
        }

        if (Take(element, UblNames.Cac + "Country", mapped) is { } country)
        {
            owners[country] = address;
            address.CountryCode = values.ReadCode(Take(country, UblNames.Cbc + "IdentificationCode", mapped));
        }

        return address;
    }

    public static Contact ReadContact(
        XElement element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        var contact = new Contact
        {
            Name = values.ReadText(Take(element, UblNames.Cbc + "Name", mapped)),
            Telephone = values.ReadText(Take(element, UblNames.Cbc + "Telephone", mapped)),
            Email = values.ReadText(Take(element, UblNames.Cbc + "ElectronicMail", mapped)),
        };

        owners[element] = contact;
        return contact;
    }

    private static XElement? Take(XElement? parent, XName name, HashSet<XElement> mapped)
    {
        XElement? element = parent?.Element(name);
        if (element is not null)
        {
            mapped.Add(element);
        }

        return element;
    }

    private static List<XElement> TakeAll(XElement parent, XName name, HashSet<XElement> mapped)
    {
        List<XElement> elements = [.. parent.Elements(name)];
        foreach (XElement element in elements)
        {
            mapped.Add(element);
        }

        return elements;
    }
}
