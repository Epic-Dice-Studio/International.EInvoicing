using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;

namespace International.EInvoicing.Samples.Chapters;

/// <summary>
/// The answer a Peppol receiver owes the sender: what happened to the invoice.
/// </summary>
/// <remarks>
/// Without it a supplier who has sent an invoice into the network knows nothing until the money arrives or
/// does not. It is a UBL <c>ApplicationResponse</c>, and it fills the same lifecycle model the French CDAR
/// messages do — one statement, two syntaxes, exactly as an invoice is one document in UBL and CII.
/// </remarks>
internal static class PeppolResponses
{
    public static void Run()
    {
        Report.Chapter("Peppol — the answer a receiver owes the sender");

        EInvoicing library = EInvoicing.Create(builder => builder.AddDefaults().AddPeppol());

        string xml = library.Write(Rejecting("inv021"), DocumentSyntax.Ubl);
        Report.Snippet(xml, lines: 12);

        DocumentResult read = library.Read(xml);
        LifecycleStatusMessage message = read.RequireLifecycleStatus();
        ReferencedDocumentStatus status = message.References[0];

        Report.Fact("read back as", read.Kind);
        Report.Fact("profile", message.SpecificationIdentifier.Value);
        Report.Fact("about invoice", status.DocumentIdentifier.Value);
        Report.Fact("status (UNCL 4343)", status.ProcessConditionCode.Value);

        // UBL states a reason and a requested action as two cac:Status elements, told apart by their code
        // list, so a message read back carries one detail for each rather than one holding both.
        Report.Fact("why", status.StatusDetails.Select(detail => detail.ReasonCode.Value).FirstOrDefault(v => v is not null));
        Report.Fact("in words", status.StatusDetails.Select(detail => detail.Reason.Value).FirstOrDefault(v => v is not null));
        Report.Fact(
            "what is expected next",
            status.StatusDetails.Select(detail => detail.RequestedActionCode.Value).FirstOrDefault(v => v is not null));

        Report.Note("AP is the buyer approving the invoice; PD says the money has been sent. Two different");
        Report.Note("answers to \"can I stop chasing this?\", and a receiver that confuses them says the wrong one.");
        Report.Say("The same message written as CDAR instead is one argument away — einvoicing.Write(message).");
    }

    /// <summary>
    /// A rejection that says why and what the supplier should do — which Peppol requires of one.
    /// </summary>
    /// <remarks>
    /// <c>PEPPOL-T111-R001</c> makes a clarification mandatory for RE, UQ and CA: rejecting an invoice
    /// without saying why leaves the supplier nothing to act on.
    /// </remarks>
    private static LifecycleStatusMessage Rejecting(string invoiceNumber)
    {
        var message = new LifecycleStatusMessage
        {
            SpecificationIdentifier = PeppolResponseProfiles.InvoiceResponse.Id,
            BusinessProcessType = new IdentifierField("urn:fdc:peppol.eu:poacc:bis:invoice_response:3"),
            Identifier = new IdentifierField("resp-2026-0007"),
            IssuedAt = new DateTimeField(new DateTimeOffset(2026, 9, 3, 10, 30, 0, TimeSpan.Zero)),
            Sender = new StatusParty
            {
                ElectronicAddress = new IdentifierField("0203201340", "0208"),
                Name = "Acheteur SA",
            },
        };

        message.Recipients.Add(new StatusParty
        {
            ElectronicAddress = new IdentifierField("0876543210", "0208"),
            Name = "Vendeur SAS",
        });

        var status = new ReferencedDocumentStatus
        {
            ProcessConditionCode = new CodeField(PeppolResponseCodes.Rejected),
            DocumentIdentifier = new IdentifierField(invoiceNumber),
            DocumentIssueDate = new DateOnly(2026, 9, 1),
            DocumentTypeCode = new CodeField(InvoiceTypeCodes.CommercialInvoice),
        };

        status.StatusDetails.Add(new DocumentStatusDetail
        {
            ReasonCode = new CodeField("REF", "OPStatusReason"),
            Reason = "The purchase order number does not match any order we placed.",
            RequestedActionCode = new CodeField("NIN"),
            RequestedAction = "Send a new invoice quoting the correct order.",
        });

        message.References.Add(status);
        return message;
    }
}
