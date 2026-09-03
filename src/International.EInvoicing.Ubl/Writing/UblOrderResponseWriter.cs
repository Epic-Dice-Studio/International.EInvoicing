using System.Diagnostics.CodeAnalysis;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Ubl.Writing;

/// <summary>
/// Writes an order response as UBL 2.1.
/// </summary>
/// <remarks>
/// Element order follows <c>UBL-OrderResponse-2.1.xsd</c>. The parts an order response shares with an order
/// — a party, an item, a price, a delivery window — are written by the order's writer, so the two documents
/// cannot drift apart in how they state the same thing.
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "An instance API so a caller can replace this writer through the registry.")]
public sealed class UblOrderResponseWriter : IDocumentWriter<OrderResponse>
{
    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.Ubl;

    /// <summary>Writes <paramref name="document"/> to <paramref name="destination"/>. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public void Write(OrderResponse document, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        using var writer = UblDocument.Open(
            destination,
            UblOrderResponseNames.RootElement,
            UblOrderResponseNames.OrderResponse.NamespaceName);

        Write(document, writer);
    }

    /// <summary>Writes <paramref name="document"/> and returns it as XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public string WriteToString(OrderResponse document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var buffer = new MemoryStream();
        Write(document, buffer);
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <inheritdoc />
    public Task WriteAsync(
        OrderResponse document,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        return DocumentStreams.WriteAllAsync(WriteToString(document), destination, cancellationToken);
    }

    private static void Write(OrderResponse response, UblDocument writer)
    {
        string? currency = response.CurrencyCode.Value ?? response.CurrencyCode.Raw;

        if (response.SpecificationIdentifier.IsDeclared)
        {
            writer.Cbc("CustomizationID", response.SpecificationIdentifier.Value);
        }

        writer.Identifier("ProfileID", response.BusinessProcessType);
        writer.Identifier("ID", response.Number);
        writer.Identifier("SalesOrderID", response.SalesOrderNumber);
        writer.Moment("IssueDate", "IssueTime", response.IssuedAt);
        writer.Code("OrderResponseCode", response.ResponseCode);
        writer.Text("Note", response.Note);
        writer.Code("DocumentCurrencyCode", response.CurrencyCode);
        writer.Text("CustomerReference", response.BuyerReference);

        if (response.OrderReference.IsSet)
        {
            writer.StartCac("OrderReference");
            writer.Identifier("ID", response.OrderReference);
            writer.End();
        }

        if (response.OrderChangeReference.IsSet)
        {
            writer.StartCac("OrderChangeDocumentReference");
            writer.Identifier("ID", response.OrderChangeReference);
            writer.End();
        }

        UblOrderWriter.WriteWrappedParty(response.Seller, "SellerSupplierParty", writer);
        UblOrderWriter.WriteWrappedParty(response.Buyer, "BuyerCustomerParty", writer);
        UblOrderWriter.WriteDelivery(response.Delivery, writer);

        foreach (OrderResponseLine line in response.Lines)
        {
            WriteLine(line, writer, currency);
        }

        writer.Extensions(response.Extensions);
    }

    private static void WriteLine(OrderResponseLine line, UblDocument writer, string? currency)
    {
        writer.StartCac("OrderLine");

        writer.StartCac("LineItem");
        writer.Identifier("ID", line.Identifier);
        writer.Text("Note", line.Note);
        writer.Code("LineStatusCode", line.StatusCode);
        writer.Quantity("Quantity", line.Quantity);
        writer.Quantity("MaximumBackorderQuantity", line.MaximumBackorderQuantity);
        UblOrderWriter.WriteDelivery(line.Delivery, writer);
        UblOrderWriter.WritePrice(line.Price, writer, currency);
        UblOrderWriter.WriteItem(line.Item, writer);
        writer.End();

        if (line.SubstitutedIdentifier.IsSet || line.SubstitutedItem is not null)
        {
            writer.StartCac("SellerSubstitutedLineItem");
            writer.Identifier("ID", line.SubstitutedIdentifier);
            UblOrderWriter.WriteItem(line.SubstitutedItem, writer);
            writer.End();
        }

        if (line.OrderLineReference.IsSet)
        {
            writer.StartCac("OrderLineReference");
            writer.Identifier("LineID", line.OrderLineReference);
            writer.End();
        }

        writer.Extensions(line.Extensions);
        writer.End();
    }
}
