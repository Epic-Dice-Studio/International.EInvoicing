using International.EInvoicing.Cdar;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;

namespace International.EInvoicing.Samples.Chapters;

/// <summary>
/// What arrives from a trading partner is not what a specification describes.
/// </summary>
/// <remarks>
/// Nothing in this chapter throws. Reading a document you received reports instead: a profile nobody has
/// heard of, a date in a format nobody agreed to, an element the model has no field for, XML that stops
/// halfway. Each one keeps as much of the document as it can and says what it gave up.
/// </remarks>
internal static class HostileDocuments
{
    private const string Acme = "urn:acme:invoice:1p0";

    public static void Run(EInvoicing einvoicing)
    {
        Report.Chapter("Documents that fight back");

        AProfileNobodyHasHeardOf(einvoicing);
        AValueThatCannotBeRead(einvoicing);
        AnElementTheModelHasNoFieldFor(einvoicing);
        XmlThatStopsHalfway(einvoicing);
    }

    private static void AProfileNobodyHasHeardOf(EInvoicing einvoicing)
    {
        DocumentResult result = einvoicing.Read(AnInvoice(profile: "urn:acme:profile:2p0"));

        Report.Fact("still readable", result.IsUsable);
        Report.Fact("fell back to", result.Profile?.Profile?.Name);
        Report.Fact("complete validation possible", result.Profile?.AllowsCompleteValidation);

        foreach (Diagnostic diagnostic in result.Diagnostics.Where(d => d.Code == "EIV1042"))
        {
            Report.Note($"{diagnostic.Code} expected {diagnostic.Expected}, found {diagnostic.Found}");
            Report.Note($"        fallback: {diagnostic.AppliedFallback}");
        }
    }

    private static void AValueThatCannotBeRead(EInvoicing einvoicing)
    {
        DocumentResult result = einvoicing.Read(AnInvoice(issueDate: "le 1er septembre"));
        EInvoice invoice = result.RequireInvoice();

        Report.Fact("BT-2 typed value", invoice.IssueDate.Value);
        Report.Fact("BT-2 raw text kept", invoice.IssueDate.Raw);
        Report.Fact("BT-2 raw only", invoice.IssueDate.IsRawOnly);
        Report.Fact("and it says why", invoice.IssueDate.Diagnostic?.Code);
        Report.Say("One unreadable value does not cost you the document.");
    }

    private static void AnElementTheModelHasNoFieldFor(EInvoicing einvoicing)
    {
        DocumentResult result = einvoicing.Read(AnInvoice(extra: true));
        EInvoice invoice = result.RequireInvoice();

        foreach (ExtensionElement element in invoice.Extensions)
        {
            Report.Fact("kept as extension data", element.QualifiedName);
        }

        Report.Fact("re-written unchanged", einvoicing.Write(invoice).Contains("acme:Approval", StringComparison.Ordinal));
        Report.Say("Nothing a document contained is dropped, even when the model has no name for it.");
    }

    private static void XmlThatStopsHalfway(EInvoicing einvoicing)
    {
        DocumentResult result = einvoicing.Read("<ubl:Invoice xmlns:ubl=\"" + Ubl.UblNames.Invoice + "\"><cbc:ID>");

        Report.Fact("usable", result.IsUsable);
        Report.Fact("reported as", result.Diagnostics.Count > 0 ? result.Diagnostics[0].Code : null);
        Report.Say("Malformed XML is the one case that yields nothing — and it is still a diagnostic, not a throw.");
    }

    private static string AnInvoice(
        string profile = "urn:cen.eu:en16931:2017",
        string issueDate = "2026-09-01",
        bool extra = false) =>
        $"""
        <ubl:Invoice xmlns:ubl="{Ubl.UblNames.Invoice}" xmlns:cac="{Ubl.UblNames.Cac}"
                     xmlns:cbc="{Ubl.UblNames.Cbc}" xmlns:acme="{Acme}">
          <cbc:CustomizationID>{profile}</cbc:CustomizationID>
          <cbc:ID>FA-2026-009</cbc:ID>
          <cbc:IssueDate>{issueDate}</cbc:IssueDate>
          <cbc:DocumentCurrencyCode>EUR</cbc:DocumentCurrencyCode>
          {(extra ? "<acme:Approval>signed off by finance</acme:Approval>" : string.Empty)}
          <cac:InvoiceLine>
            <cbc:ID>1</cbc:ID>
            <cbc:LineExtensionAmount currencyID="EUR">450.00</cbc:LineExtensionAmount>
            <cac:Item><cbc:Name>Conseil</cbc:Name></cac:Item>
          </cac:InvoiceLine>
        </ubl:Invoice>
        """;
}
