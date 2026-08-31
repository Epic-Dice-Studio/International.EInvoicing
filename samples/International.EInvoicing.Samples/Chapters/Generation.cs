using International.EInvoicing.Documents;
using International.EInvoicing.Model;

namespace International.EInvoicing.Samples.Chapters;

/// <summary>
/// Running your own logic inside generation, for every document written.
/// </summary>
/// <remarks>
/// The thing every company has: a numbering scheme, a house rounding rule, a signature, an element one large
/// customer demands. Forking the library to add it means owning the merge forever; doing it at each call site
/// works until somebody adds a call site. See <c>docs/guides/hook-into-generation.md</c>.
/// </remarks>
internal static class Generation
{
    public static void Run(EInvoice invoice)
    {
        Report.Chapter("Running our own logic during generation");

        EInvoicing einvoicing = EInvoicing.Create(library => library
            .AddDefaults()
            .AddWriteStep(new StampTheHouseReference())
            .AddWriteStep((context, next) =>
            {
                // After next, the document exists as text — where a signature or an audit line goes.
                next(context);
                context.Xml += $"{Environment.NewLine}<!-- sent {DateOnly.FromDateTime(DateTime.UtcNow)} -->";
            }));

        string ubl = einvoicing.Write(invoice, DocumentFormat.Ubl);

        Report.Fact("our reference reached the document", ubl.Contains("SERVICE-COMPTA", StringComparison.Ordinal));
        Report.Fact("and our trailer did too", ubl.TrimEnd().EndsWith("-->", StringComparison.Ordinal));

        // The point of wrapping the writer rather than calling the steps from the facade: a colleague who
        // reaches for the writer directly cannot break the rule by accident.
        string direct = einvoicing.UblWriter.WriteToString(invoice);

        Report.Fact("the writer used directly runs them too", direct.Contains("SERVICE-COMPTA", StringComparison.Ordinal));
        Report.Note("Rules belong in a rule set, not in a step: named, reportable, suppressible one by one.");
    }

    private sealed class StampTheHouseReference : IWritePipelineStep
    {
        public void Write(WriteContext context, Action<WriteContext> next)
        {
            context.Invoice.BuyerReference = "SERVICE-COMPTA";
            next(context);
        }
    }
}
