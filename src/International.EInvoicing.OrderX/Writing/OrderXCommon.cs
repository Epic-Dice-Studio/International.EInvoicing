using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.OrderX.Writing;

/// <summary>
/// The parts of an Order-X document that are written the same wherever they appear.
/// </summary>
/// <remarks>
/// An order, an order change and an order response share one transaction shape — the same parties, the same
/// referenced documents, the same products and prices — so writing them lives here and each writer is about
/// what its own document says.
/// </remarks>
internal static class OrderXCommon
{
    public static void WriteNotes(IEnumerable<InvoiceNote> notes, OrderXDocument writer)
    {
        foreach (InvoiceNote note in notes)
        {
            writer.StartRam("IncludedNote", note.Extensions);
            writer.Text("Content", note.Text);
            writer.Code("SubjectCode", note.SubjectCode);
            writer.End();
        }
    }

    public static void WriteAllowanceCharge(AllowanceCharge allowanceCharge, OrderXDocument writer)
    {
        writer.StartRam("SpecifiedTradeAllowanceCharge", allowanceCharge.Extensions);
        writer.Indicator("ChargeIndicator", new IndicatorField(allowanceCharge.IsCharge));
        writer.Decimal("CalculationPercent", allowanceCharge.Percentage);
        writer.Amount("BasisAmount", allowanceCharge.BaseAmount);
        writer.Amount("ActualAmount", allowanceCharge.Amount);
        writer.Code("ReasonCode", allowanceCharge.ReasonCode);
        writer.Text("Reason", allowanceCharge.Reason);

        if (allowanceCharge.VatCategoryCode.IsSet || allowanceCharge.VatRate.IsSet)
        {
            writer.StartRam("CategoryTradeTax");
            writer.Ram("TypeCode", "VAT");
            writer.Code("CategoryCode", allowanceCharge.VatCategoryCode);
            writer.Decimal("RateApplicablePercent", allowanceCharge.VatRate);
            writer.End();
        }

        writer.End();
    }

    public static void WriteParty(string elementName, Party? party, OrderXDocument writer)
    {
        if (party is null)
        {
            return;
        }

        writer.StartRam(elementName, party.Extensions);

        // A party's identifiers are one list in the model, and two elements in CII: a GLN or another scheme
        // the parties agreed goes in GlobalID, and everything else in ID.
        foreach (IdentifierField identifier in party.Identifiers.Where(id => id.IsSet && id.SchemeId is null))
        {
            writer.Identifier("ID", identifier);
        }

        foreach (IdentifierField identifier in party.Identifiers.Where(id => id.IsSet && id.SchemeId is not null))
        {
            writer.Identifier("GlobalID", identifier);
        }

        writer.Text("Name", party.Name);
        writer.Text("Description", party.AdditionalLegalInformation);

        if (party.LegalRegistrationIdentifier.IsSet || party.TradingName.IsSet)
        {
            writer.StartRam("SpecifiedLegalOrganization");
            writer.Identifier("ID", party.LegalRegistrationIdentifier);
            writer.Text("TradingBusinessName", party.TradingName);
            writer.End();
        }

        WriteContact(party.Contact, writer);
        WriteAddress(party.Address, writer);

        if (party.ElectronicAddress.IsSet)
        {
            writer.StartRam("URIUniversalCommunication");
            writer.Identifier("URIID", party.ElectronicAddress);
            writer.End();
        }

        WriteTaxRegistration(party.VatIdentifier, "VA", writer);
        WriteTaxRegistration(party.TaxRegistrationIdentifier, "FC", writer);

        writer.End();
    }

    public static void WriteTaxRegistration(IdentifierField identifier, string scheme, OrderXDocument writer)
    {
        if (!identifier.IsSet)
        {
            return;
        }

        writer.StartRam("SpecifiedTaxRegistration");
        writer.Identifier("ID", identifier.SchemeId is null ? identifier with { SchemeId = scheme } : identifier);
        writer.End();
    }

    public static void WriteContact(Contact? contact, OrderXDocument writer)
    {
        if (contact is null)
        {
            return;
        }

        writer.StartRam("DefinedTradeContact", contact.Extensions);
        writer.Text("PersonName", contact.Name);
        writer.Text("DepartmentName", contact.Department);
        writer.Code("TypeCode", contact.TypeCode);

        if (contact.Telephone.IsSet)
        {
            writer.StartRam("TelephoneUniversalCommunication");
            writer.Text("CompleteNumber", contact.Telephone);
            writer.End();
        }

        if (contact.Email.IsSet)
        {
            writer.StartRam("EmailURIUniversalCommunication");
            writer.Text("URIID", contact.Email);
            writer.End();
        }

        writer.End();
    }

    public static void WriteAddress(PostalAddress? address, OrderXDocument writer)
    {
        if (address is null)
        {
            return;
        }

        writer.StartRam("PostalTradeAddress", address.Extensions);
        writer.Text("PostcodeCode", address.PostCode);
        writer.Text("LineOne", address.Line1);
        writer.Text("LineTwo", address.Line2);
        writer.Text("LineThree", address.Line3);
        writer.Text("CityName", address.City);
        writer.Code("CountryID", address.CountryCode);
        writer.Text("CountrySubDivisionName", address.CountrySubdivision);
        writer.End();
    }

    public static void WriteReference(string elementName, IdentifierField identifier, OrderXDocument writer)
    {
        if (!identifier.IsSet)
        {
            return;
        }

        writer.StartRam(elementName);
        writer.Identifier("IssuerAssignedID", identifier);
        writer.End();
    }

    public static void WriteDocument(string elementName, AdditionalDocument document, OrderXDocument writer)
    {
        writer.StartRam(elementName, document.Extensions);
        writer.Identifier("IssuerAssignedID", document.Identifier);
        writer.Text("URIID", document.ExternalLocation);
        writer.Identifier("LineID", document.LineReference);
        writer.Code("TypeCode", document.TypeCode);
        writer.Text("Name", document.Description);
        writer.Code("ReferenceTypeCode", document.ReferenceTypeCode);
        writer.End();
    }

    public static void WritePeriod(string elementName, InvoicingPeriod? period, OrderXDocument writer)
    {
        if (period is null)
        {
            return;
        }

        writer.StartRam(elementName, period.Extensions);
        WriteDate("StartDateTime", period.StartDate, writer);
        WriteDate("EndDateTime", period.EndDate, writer);
        writer.End();
    }

    public static void WriteDate(string elementName, DateField field, OrderXDocument writer)
    {
        if (field.IsSet)
        {
            writer.Moment(
                elementName,
                new DateTimeField(
                    field.Value is { } date ? new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null,
                    field.FormatCode ?? DateField.FormatCcyyMmDd,
                    field.Source));
        }
    }

    public static void WriteAppliedAdjustment(AllowanceCharge adjustment, OrderXDocument writer)
    {
        writer.StartRam("AppliedTradeAllowanceCharge", adjustment.Extensions);
        writer.Indicator("ChargeIndicator", new IndicatorField(adjustment.IsCharge));
        writer.Decimal("CalculationPercent", adjustment.Percentage);
        writer.Amount("BasisAmount", adjustment.BaseAmount);
        writer.Amount("ActualAmount", adjustment.Amount);
        writer.Code("ReasonCode", adjustment.ReasonCode);
        writer.Text("Reason", adjustment.Reason);
        writer.End();
    }
    public static void WriteItem(string elementName, OrderItem? item, OrderXDocument writer)
    {
        if (item is null)
        {
            return;
        }

        writer.StartRam(elementName, item.Extensions);
        writer.Identifier("GlobalID", item.StandardIdentifier);
        writer.Identifier("SellerAssignedID", item.SellerIdentifier);
        writer.Identifier("BuyerAssignedID", item.BuyerIdentifier);
        writer.Text("Name", item.Name);
        writer.Text("Description", item.Description);
        writer.Identifier("BatchID", item.BatchIdentifier);
        writer.Text("BrandName", item.BrandName);

        foreach (OrderItemProperty characteristic in item.Characteristics)
        {
            writer.StartRam("ApplicableProductCharacteristic", characteristic.Extensions);
            writer.Code("TypeCode", characteristic.NameCode);
            writer.Text("Description", characteristic.Name);
            writer.Quantity("ValueMeasure", characteristic.ValueQuantity);
            writer.Text("Value", characteristic.Value);
            writer.End();
        }

        foreach (ItemClassification classification in item.Classifications.Where(c => c.Code.IsSet))
        {
            writer.StartRam("DesignatedProductClassification", classification.Extensions);
            writer.Code("ClassCode", classification.Code);
            writer.Text("ClassName", classification.Name);
            writer.End();
        }

        foreach (ItemInstance instance in item.Instances)
        {
            writer.StartRam("IndividualTradeProductInstance", instance.Extensions);
            writer.Identifier("BatchID", instance.LotIdentifier);
            writer.Identifier("SerialID", instance.SerialIdentifier);
            writer.End();
        }

        if (item.Packaging is { } packaging)
        {
            writer.StartRam("ApplicableSupplyChainPackaging", packaging.Extensions);
            writer.Code("TypeCode", packaging.TypeCode);

            if (packaging.Width.IsSet || packaging.Length.IsSet || packaging.Height.IsSet)
            {
                writer.StartRam("LinearSpatialDimension");
                writer.Quantity("WidthMeasure", packaging.Width);
                writer.Quantity("LengthMeasure", packaging.Length);
                writer.Quantity("HeightMeasure", packaging.Height);
                writer.End();
            }

            writer.End();
        }

        if (item.OriginCountryCode.IsSet)
        {
            writer.StartRam("OriginTradeCountry");
            writer.Code("ID", item.OriginCountryCode);
            writer.End();
        }

        if (item.SpecificationDocument is { } specification)
        {
            OrderXCommon.WriteDocument("AdditionalReferenceReferencedDocument", specification, writer);
        }

        writer.End();
    }
    /// <summary>
    /// Writes the gross and net prices. A per-unit allowance belongs to the gross price, because that is
    /// what makes the net follow from it; a line's allowances are amounts and go in the settlement.
    /// </summary>
    public static void WritePrice(LinePrice? price, OrderXDocument writer)
    {
        if (price is null)
        {
            return;
        }

        if (price.GrossPrice.IsSet || price.Discount.IsSet || price.Adjustments.Count > 0)
        {
            writer.StartRam("GrossPriceProductTradePrice");
            writer.Amount("ChargeAmount", price.GrossPrice);
            writer.Quantity("BasisQuantity", price.BaseQuantity);

            // The list is the full account when there is one. A model filled from a syntax that carries only
            // BT-147 has no list, and the single discount stands in for it.
            if (price.Adjustments.Count > 0)
            {
                foreach (AllowanceCharge adjustment in price.Adjustments)
                {
                    OrderXCommon.WriteAppliedAdjustment(adjustment, writer);
                }
            }
            else if (price.Discount.IsSet)
            {
                writer.StartRam("AppliedTradeAllowanceCharge");
                writer.Indicator("ChargeIndicator", new IndicatorField(false));
                writer.Amount("ActualAmount", price.Discount);
                writer.End();
            }

            writer.End();
        }

        if (price.NetPrice.IsSet || price.BaseQuantity.IsSet)
        {
            writer.StartRam("NetPriceProductTradePrice", price.Extensions);
            writer.Amount("ChargeAmount", price.NetPrice);
            writer.Quantity("BasisQuantity", price.BaseQuantity);
            writer.End();
        }
    }

    /// <summary>
    /// When the seller undertakes to deliver: a moment when only one end is stated, a window when both are.
    /// </summary>
    /// <remarks>
    /// The same element an order uses to ask. On a response it is an undertaking, and the model keeps the
    /// promised pair apart from the requested one so a buyer can see the two side by side.
    /// </remarks>
    public static void WritePromisedDelivery(OrderDelivery delivery, OrderXDocument writer)
    {
        if (!delivery.PromisedAt.IsSet && !delivery.PromisedFrom.IsSet && !delivery.PromisedUntil.IsSet)
        {
            return;
        }

        writer.StartRam("RequestedDeliverySupplyChainEvent");
        writer.Moment("OccurrenceDateTime", delivery.PromisedAt);

        if (delivery.PromisedFrom.IsSet || delivery.PromisedUntil.IsSet)
        {
            writer.StartRam("OccurrenceSpecifiedPeriod");
            writer.Moment("StartDateTime", delivery.PromisedFrom);
            writer.Moment("EndDateTime", delivery.PromisedUntil);
            writer.End();
        }

        writer.End();
    }
}
