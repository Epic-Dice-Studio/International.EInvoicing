using International.EInvoicing.FacturX.Pdf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace International.EInvoicing.FacturX.PdfSharp;

/// <summary>Registers the PDFsharp implementation of the Factur-X PDF abstractions.</summary>
public static class PdfSharpServiceCollectionExtensions
{
    /// <summary>
    /// Uses PDFsharp to read and write the PDF half of hybrid invoices. Register your own implementation
    /// before this one to keep it: these registrations do not replace what is already there.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddFacturXPdfSharp(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IPdfAttachmentReader, PdfSharpAttachmentReader>();
        services.TryAddSingleton<IPdfAttachmentWriter, PdfSharpAttachmentWriter>();
        return services;
    }
}
