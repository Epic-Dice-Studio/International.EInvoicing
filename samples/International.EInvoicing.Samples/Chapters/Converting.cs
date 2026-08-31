using International.EInvoicing.Model;

namespace International.EInvoicing.Samples.Chapters;

/// <summary>
/// Carrying a document from one syntax to the other, and being told what it cost.
/// </summary>
/// <remarks>
/// A French recipient does not choose the syntax its customers send. Converting is the integration — and the
/// dangerous version is the silent one, which hands back a well-formed document and says nothing about what
/// it dropped. The report is the feature. See <c>docs/guides/convert-between-syntaxes.md</c>.
/// </remarks>
internal static class Converting
{
    public static void Run(EInvoicing einvoicing, string ubl)
    {
        Report.Chapter("Converting between UBL and CII");

        ConversionResult toCii = einvoicing.Convert(ubl, DocumentFormat.Cii);

        Report.Fact("carried everything", toCii.IsLossless);
        Report.Fact("the buyer crossed", toCii.Invoice?.Buyer?.Name.Value);
        Report.Fact("so did the amount due", toCii.Invoice?.Totals.DuePayableAmount.Value);

        ConversionResult andBack = einvoicing.Convert(toCii.Xml, DocumentFormat.Ubl);

        Report.Fact("and it came back", andBack.Invoice?.Number.Value);

        // Now the interesting case: an element the model has no field for. It is kept when the document is
        // read, and re-emitted in the same syntax — but it has nowhere to go in the other one, and that is
        // what the report exists to say out loud.
        string withHouseContent = ubl.Replace(
            "</cac:AccountingSupplierParty>",
            "</cac:AccountingSupplierParty><cbc:HouseNote>approved by finance</cbc:HouseNote>",
            StringComparison.Ordinal);

        ConversionResult lossy = einvoicing.Convert(withHouseContent, DocumentFormat.Cii);

        Report.Fact("carried everything", lossy.IsLossless);

        foreach (ConversionLoss loss in lossy.Losses)
        {
            Report.Note(loss.ToString());
        }

        Report.Say("A lossless conversion is not a valid document: validate what comes out, in the profile");
        Report.Say("you intend to send. Conversion cannot invent a term the source never carried.");
    }
}
