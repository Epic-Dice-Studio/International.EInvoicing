using International.EInvoicing.Configuration;
using International.EInvoicing.Countries.Belgium;
using International.EInvoicing.Countries.France;
using International.EInvoicing.Countries.Germany;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.FacturX.PdfSharp;
using International.EInvoicing.Ubl.Writing;
using International.EInvoicing.Validation.XRechnung;
using Microsoft.Extensions.DependencyInjection;

namespace International.EInvoicing.Samples.Chapters;

/// <summary>Assembling the library — the same calls with or without a container.</summary>
internal static class Wiring
{
    /// <summary>Without a container: one call, and everything below it is reachable.</summary>
    public static EInvoicing Assemble()
    {
        Report.Chapter("Assembling the library");

        EInvoicing einvoicing = EInvoicing.Create(
            library => library
                .AddDefaults()                 // UBL, CII, lifecycle messages, Factur-X, the EN 16931 rules
                .AddFrance()                   // French profiles, and the lifecycle plumbing they need
                .AddGermany()
                .AddBelgium()
                .AddXRechnungRules()           // only for documents that declare an XRechnung profile
                .UseDiagnosticPreset(DiagnosticPreset.Balanced),
            pdf: new PdfSharpAttachmentReader());

        Report.Fact("profiles this instance knows", einvoicing.KnownProfiles.Count);
        Report.Fact("rule sets registered", einvoicing.RuleSets.Count);

        foreach (Validation.IDocumentRuleSet ruleSet in einvoicing.RuleSets)
        {
            Report.Note($"{ruleSet.Name} {ruleSet.Version}");
        }

        return einvoicing;
    }

    /// <summary>With a container: the same calls, and the facade becomes injectable.</summary>
    public static void ThroughAContainer()
    {
        Report.Chapter("The same, in a container");

        ServiceProvider provider = new ServiceCollection()
            .AddEInvoicing(library => library.AddDefaults().AddFrance())
            .AddFacturXPdfSharp()
            .BuildServiceProvider();

        Report.Fact("EInvoicing resolves", provider.GetService<EInvoicing>() is not null);
        Report.Fact("so do the writers under it", provider.GetService<UblInvoiceWriter>() is not null);
        Report.Fact("and the lifecycle reader", provider.GetService<Cdar.Reading.CdarReader>() is not null);
        Report.Note("One registration. There is no second list of Add…Services() calls to remember.");

        provider.Dispose();
    }
}
