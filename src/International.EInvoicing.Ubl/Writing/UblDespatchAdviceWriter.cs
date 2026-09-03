using System.Diagnostics.CodeAnalysis;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl.Reading;
using International.EInvoicing.Values;

namespace International.EInvoicing.Ubl.Writing;

/// <summary>
/// Writes a despatch advice as UBL 2.1.
/// </summary>
/// <remarks>
/// Element order is normative in UBL, so this writer is explicit rather than generated: the order below
/// follows <c>UBL-DespatchAdvice-2.1.xsd</c>. What the model keeps in one place and UBL states in several —
/// a moment as a date and a time, a hazard classification inside the item element — is put back where the
/// schema expects it.
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "An instance API so a caller can replace this writer through the registry.")]
public sealed class UblDespatchAdviceWriter : IDocumentWriter<DespatchAdvice>
{
    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.Ubl;

    /// <summary>Writes <paramref name="document"/> to <paramref name="destination"/>. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public void Write(DespatchAdvice document, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        using var writer = UblDocument.Open(
            destination,
            UblDespatchAdviceNames.RootElement,
            UblDespatchAdviceNames.DespatchAdvice.NamespaceName);

        Write(document, writer);
    }

    /// <summary>Writes <paramref name="document"/> and returns it as XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public string WriteToString(DespatchAdvice document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var buffer = new MemoryStream();
        Write(document, buffer);
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <inheritdoc />
    public Task WriteAsync(DespatchAdvice document, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        return DocumentStreams.WriteAllAsync(WriteToString(document), destination, cancellationToken);
    }

    private static void Write(DespatchAdvice advice, UblDocument writer)
    {
        if (advice.SpecificationIdentifier.IsDeclared)
        {
            writer.Cbc("CustomizationID", advice.SpecificationIdentifier.Value);
        }

        writer.Identifier("ProfileID", advice.BusinessProcessType);
        writer.Identifier("ID", advice.Number);
        writer.Moment("IssueDate", "IssueTime", advice.IssuedAt);
        writer.Code("DespatchAdviceTypeCode", advice.TypeCode);
        writer.Text("Note", advice.Note);

        if (advice.OrderReference.IsSet)
        {
            writer.StartCac("OrderReference");
            writer.Identifier("ID", advice.OrderReference);
            writer.End();
        }

        foreach (AdditionalDocument document in advice.AdditionalDocuments)
        {
            WriteDocument("AdditionalDocumentReference", document, writer);
        }

        WriteWrappedParty(advice.DespatchParty, "DespatchSupplierParty", writer);
        WriteWrappedParty(advice.DeliveryParty, "DeliveryCustomerParty", writer);
        WriteWrappedParty(advice.BuyerParty, "BuyerCustomerParty", writer);
        WriteWrappedParty(advice.SellerParty, "SellerSupplierParty", writer);
        WriteWrappedParty(advice.OriginatorParty, "OriginatorCustomerParty", writer);

        WriteShipment(advice.Shipment, writer);

        foreach (DespatchLine line in advice.Lines)
        {
            WriteLine(line, writer);
        }

        writer.Extensions(advice.Extensions);
    }

    /// <remarks>
    /// The delivery role's contact is written back beside the party rather than inside it, which is where
    /// the schema puts it and where it was read from.
    /// </remarks>
    private static void WriteWrappedParty(Party? party, string role, UblDocument writer)
    {
        if (party is null)
        {
            return;
        }

        writer.StartCac(role);
        WriteParty(party, writer, role == "DeliveryCustomerParty");
        writer.End();
    }

    private static void WriteParty(Party party, UblDocument writer, bool contactBesideParty = false)
    {
        writer.StartCac("Party");
        writer.Identifier("EndpointID", party.ElectronicAddress);

        foreach (IdentifierField identifier in party.Identifiers)
        {
            writer.StartCac("PartyIdentification");
            writer.Identifier("ID", identifier);
            writer.End();
        }

        if (party.TradingName.IsSet)
        {
            writer.StartCac("PartyName");
            writer.Text("Name", party.TradingName);
            writer.End();
        }

        WriteAddress(party.Address, "PostalAddress", writer);

        if (party.Name.IsSet || party.LegalRegistrationIdentifier.IsSet)
        {
            writer.StartCac("PartyLegalEntity");
            writer.Text("RegistrationName", party.Name);
            writer.Identifier("CompanyID", party.LegalRegistrationIdentifier);
            writer.End();
        }

        if (!contactBesideParty)
        {
            WriteContact(party.Contact, "Contact", writer);
        }

        writer.Extensions(party.Extensions);
        writer.End();

        if (contactBesideParty)
        {
            WriteContact(party.Contact, "DeliveryContact", writer);
        }
    }

    private static void WriteContact(Contact? contact, string localName, UblDocument writer)
    {
        if (contact is null)
        {
            return;
        }

        writer.StartCac(localName);
        writer.Text("Name", contact.Name);
        writer.Text("Telephone", contact.Telephone);
        writer.Text("ElectronicMail", contact.Email);
        writer.Extensions(contact.Extensions);
        writer.End();
    }

    private static void WriteAddress(PostalAddress? address, string localName, UblDocument writer)
    {
        if (address is null)
        {
            return;
        }

        writer.StartCac(localName);
        writer.Text("StreetName", address.Line1);
        writer.Text("AdditionalStreetName", address.Line2);
        writer.Text("CityName", address.City);
        writer.Text("PostalZone", address.PostCode);
        writer.Text("CountrySubentity", address.CountrySubdivision);

        if (address.Line3.IsSet)
        {
            writer.StartCac("AddressLine");
            writer.Text("Line", address.Line3);
            writer.End();
        }

        if (address.CountryCode.IsSet)
        {
            writer.StartCac("Country");
            writer.Code("IdentificationCode", address.CountryCode);
            writer.End();
        }

        writer.Extensions(address.Extensions);
        writer.End();
    }

    private static void WriteShipment(Shipment? shipment, UblDocument writer)
    {
        if (shipment is null)
        {
            return;
        }

        writer.StartCac("Shipment");
        writer.Identifier("ID", shipment.Identifier);
        writer.Text("Information", shipment.Information);
        writer.Quantity("GrossWeightMeasure", shipment.GrossWeight);
        writer.Quantity("GrossVolumeMeasure", shipment.GrossVolume);
        writer.Quantity("TotalTransportHandlingUnitQuantity", shipment.HandlingUnitCount);

        if (shipment.ConsignmentIdentifier.IsSet || shipment.ConsignmentInformation.IsSet || shipment.Carrier is not null)
        {
            writer.StartCac("Consignment");
            writer.Identifier("ID", shipment.ConsignmentIdentifier);
            writer.Text("Information", shipment.ConsignmentInformation);

            if (shipment.Carrier is { } carrier)
            {
                writer.StartCac("CarrierParty");
                WriteInnerParty(carrier, writer);
                writer.End();
            }

            writer.End();
        }

        if (shipment.TransportModeCode.IsSet)
        {
            writer.StartCac("ShipmentStage");
            writer.Code("TransportModeCode", shipment.TransportModeCode);
            writer.End();
        }

        bool hasDelivery = shipment.TrackingIdentifier.IsSet
            || shipment.EstimatedDeliveryFrom.IsSet
            || shipment.EstimatedDeliveryUntil.IsSet
            || shipment.DespatchedAt.IsSet
            || shipment.DespatchAddress is not null;

        if (hasDelivery)
        {
            writer.StartCac("Delivery");
            writer.Identifier("TrackingID", shipment.TrackingIdentifier);

            if (shipment.EstimatedDeliveryFrom.IsSet || shipment.EstimatedDeliveryUntil.IsSet)
            {
                writer.StartCac("EstimatedDeliveryPeriod");
                writer.Moment("StartDate", "StartTime", shipment.EstimatedDeliveryFrom);
                writer.Moment("EndDate", "EndTime", shipment.EstimatedDeliveryUntil);
                writer.End();
            }

            if (shipment.DespatchedAt.IsSet || shipment.DespatchAddress is not null)
            {
                writer.StartCac("Despatch");
                writer.Moment("ActualDespatchDate", "ActualDespatchTime", shipment.DespatchedAt);
                WriteAddress(shipment.DespatchAddress, "DespatchAddress", writer);
                writer.End();
            }

            writer.End();
        }

        foreach (TransportHandlingUnit unit in shipment.HandlingUnits)
        {
            WriteHandlingUnit(unit, writer);
        }

        writer.Extensions(shipment.Extensions);
        writer.End();
    }

    private static void WriteDocument(string localName, AdditionalDocument document, UblDocument writer)
    {
        writer.StartCac(localName);
        writer.Identifier("ID", document.Identifier);
        writer.Text("DocumentType", document.Description);

        if (document.Attachment.IsSet || document.ExternalLocation.IsSet)
        {
            writer.StartCac("Attachment");
            writer.Binary("EmbeddedDocumentBinaryObject", document.Attachment);

            if (document.ExternalLocation.IsSet)
            {
                writer.StartCac("ExternalReference");
                writer.Text("URI", document.ExternalLocation);
                writer.End();
            }

            writer.End();
        }

        writer.Extensions(document.Extensions);
        writer.End();
    }

    /// <summary>A carrier is a party inside its role element, with no contact of its own to move.</summary>
    private static void WriteInnerParty(Party party, UblDocument writer)
    {
        foreach (IdentifierField identifier in party.Identifiers)
        {
            writer.StartCac("PartyIdentification");
            writer.Identifier("ID", identifier);
            writer.End();
        }

        if (party.TradingName.IsSet)
        {
            writer.StartCac("PartyName");
            writer.Text("Name", party.TradingName);
            writer.End();
        }

        WriteAddress(party.Address, "PostalAddress", writer);
        writer.Extensions(party.Extensions);
    }

    private static void WriteLine(DespatchLine line, UblDocument writer)
    {
        writer.StartCac("DespatchLine");
        writer.Identifier("ID", line.Identifier);
        writer.Text("Note", line.Note);
        writer.Quantity("DeliveredQuantity", line.DeliveredQuantity);
        writer.Quantity("OutstandingQuantity", line.OutstandingQuantity);
        writer.Text("OutstandingReason", line.OutstandingReason);

        if (line.OrderLineReference.IsSet || line.SalesOrderLineReference.IsSet || line.OrderReference.IsSet)
        {
            writer.StartCac("OrderLineReference");
            writer.Identifier("LineID", line.OrderLineReference);
            writer.Identifier("SalesOrderLineID", line.SalesOrderLineReference);

            if (line.OrderReference.IsSet)
            {
                writer.StartCac("OrderReference");
                writer.Identifier("ID", line.OrderReference);
                writer.End();
            }

            writer.End();
        }

        foreach (AdditionalDocument document in line.AdditionalDocuments)
        {
            WriteDocument("DocumentReference", document, writer);
        }

        WriteItem(line.Item, writer);
        WriteShipment(line.Packaging, writer);

        writer.Extensions(line.Extensions);
        writer.End();
    }

    private static void WriteItem(DespatchItem? item, UblDocument writer)
    {
        if (item is null)
        {
            return;
        }

        writer.StartCac("Item");
        writer.Text("Description", item.Description);
        writer.Text("Name", item.Name);

        WriteItemIdentifier("BuyersItemIdentification", item.BuyerIdentifier, item.BuyerIdentifierExtension, writer);
        WriteItemIdentifier("SellersItemIdentification", item.SellerIdentifier, item.SellerIdentifierExtension, writer);
        WriteItemIdentifier("StandardItemIdentification", item.StandardIdentifier, item.StandardIdentifierExtension, writer);

        foreach (CodeField classification in item.ClassificationCodes)
        {
            writer.StartCac("CommodityClassification");
            writer.Code("ItemClassificationCode", classification);
            writer.End();
        }

        if (item.DangerousGoodsCode.IsSet || item.HazardClass.IsSet)
        {
            writer.StartCac("HazardousItem");
            writer.Code("UNDGCode", item.DangerousGoodsCode);
            writer.Code("HazardClassID", item.HazardClass);
            writer.End();
        }

        foreach (ItemCharacteristic characteristic in item.Characteristics)
        {
            WriteCharacteristic(characteristic, writer);
        }

        foreach (ItemInstance instance in item.Instances)
        {
            WriteInstance(instance, writer);
        }

        writer.Extensions(item.Extensions);
        writer.End();
    }

    private static void WriteCharacteristic(ItemCharacteristic characteristic, UblDocument writer)
    {
        writer.StartCac("AdditionalItemProperty");
        writer.Text("Name", characteristic.Name);
        writer.Text("Value", characteristic.Value);
        writer.Extensions(characteristic.Extensions);
        writer.End();
    }

    private static void WriteItemIdentifier(
        string localName,
        IdentifierField field,
        IdentifierField extension,
        UblDocument writer)
    {
        if (!field.IsSet && !extension.IsSet)
        {
            return;
        }

        writer.StartCac(localName);
        writer.Identifier("ID", field);
        writer.Identifier("ExtendedID", extension);
        writer.End();
    }

    private static void WriteInstance(ItemInstance instance, UblDocument writer)
    {
        writer.StartCac("ItemInstance");
        writer.Identifier("ProductTraceID", instance.ProductTraceIdentifier);
        writer.Date("ManufactureDate", instance.ManufactureDate);
        writer.Date("BestBeforeDate", instance.BestBeforeDate);
        writer.Identifier("SerialID", instance.SerialIdentifier);

        foreach (ItemCharacteristic characteristic in instance.Characteristics)
        {
            WriteCharacteristic(characteristic, writer);
        }

        if (instance.LotIdentifier.IsSet || instance.LotExpiryDate.IsSet)
        {
            writer.StartCac("LotIdentification");
            writer.Identifier("LotNumberID", instance.LotIdentifier);
            writer.Date("ExpiryDate", instance.LotExpiryDate);
            writer.End();
        }

        writer.Extensions(instance.Extensions);
        writer.End();
    }

    private static void WriteHandlingUnit(TransportHandlingUnit unit, UblDocument writer)
    {
        writer.StartCac("TransportHandlingUnit");
        writer.Identifier("ID", unit.Identifier);
        writer.Code("TransportHandlingUnitTypeCode", unit.TypeCode);
        writer.Indicator("HazardousRiskIndicator", unit.Hazardous);
        writer.Text("ShippingMarks", unit.ShippingMarks);

        if (unit.MeasuredAttribute.IsSet || unit.Measure.IsSet)
        {
            writer.StartCac("MeasurementDimension");
            writer.Code("AttributeID", unit.MeasuredAttribute);
            writer.Quantity("Measure", unit.Measure);
            writer.End();
        }

        foreach (Package package in unit.Packages)
        {
            writer.StartCac("Package");
            writer.Identifier("ID", package.Identifier);
            writer.Code("PackagingTypeCode", package.PackagingTypeCode);
            writer.Extensions(package.Extensions);
            writer.End();
        }

        writer.Extensions(unit.Extensions);
        writer.End();
    }
}
