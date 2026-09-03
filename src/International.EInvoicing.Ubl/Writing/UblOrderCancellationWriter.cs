using System.Diagnostics.CodeAnalysis;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Ubl.Writing;

/// <summary>
/// Writes an order cancellation as UBL 2.1.
/// </summary>
/// <remarks>Element order follows <c>UBL-OrderCancellation-2.1.xsd</c>.</remarks>
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "An instance API so a caller can replace this writer through the registry.")]
public sealed class UblOrderCancellationWriter : IDocumentWriter<OrderCancellation>
{
    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.Ubl;

    /// <summary>Writes <paramref name="document"/> to <paramref name="destination"/>. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public void Write(OrderCancellation document, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        using var writer = UblDocument.Open(
            destination,
            UblOrderCancellationNames.RootElement,
            UblOrderCancellationNames.OrderCancellation.NamespaceName);

        Write(document, writer);
    }

    /// <summary>Writes <paramref name="document"/> and returns it as XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public string WriteToString(OrderCancellation document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var buffer = new MemoryStream();
        Write(document, buffer);
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <inheritdoc />
    public Task WriteAsync(
        OrderCancellation document,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        return DocumentStreams.WriteAllAsync(WriteToString(document), destination, cancellationToken);
    }

    private static void Write(OrderCancellation cancellation, UblDocument writer)
    {
        writer.Node(cancellation.Extensions);
        if (cancellation.SpecificationIdentifier.IsDeclared)
        {
            writer.Cbc("CustomizationID", cancellation.SpecificationIdentifier.Value);
        }

        writer.Identifier("ProfileID", cancellation.BusinessProcessType);
        writer.Identifier("ID", cancellation.Number);
        writer.Moment("IssueDate", "IssueTime", cancellation.IssuedAt);
        writer.Notes(cancellation.Notes);
        writer.Text("CancellationNote", cancellation.Reason);

        if (cancellation.OrderReference.IsSet)
        {
            writer.StartCac("OrderReference");
            writer.Identifier("ID", cancellation.OrderReference);
            writer.End();
        }

        if (cancellation.OriginatorReference.IsSet)
        {
            writer.StartCac("OriginatorDocumentReference");
            writer.Identifier("ID", cancellation.OriginatorReference);
            writer.End();
        }

        foreach (AdditionalDocument document in cancellation.AdditionalDocuments)
        {
            writer.StartCac("AdditionalDocumentReference", document.Extensions);
            writer.Identifier("ID", document.Identifier);
            writer.Text("DocumentType", document.Description);
            writer.End();
        }

        if (cancellation.ContractReference.IsSet)
        {
            writer.StartCac("Contract");
            writer.Identifier("ID", cancellation.ContractReference);
            writer.End();
        }

        UblOrderWriter.WriteWrappedParty(cancellation.Buyer, "BuyerCustomerParty", writer);
        UblOrderWriter.WriteWrappedParty(cancellation.Seller, "SellerSupplierParty", writer);
        UblOrderWriter.WriteWrappedParty(cancellation.Originator, "OriginatorCustomerParty", writer);

    }
}
