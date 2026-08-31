using System.Globalization;
using System.Text;
using System.Xml.Linq;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl;

namespace International.EInvoicing.Countries.Croatia;

/// <summary>
/// Puts the two things CIUS-HR asks for that the canonical model does not carry into the document as it is
/// written: the time of issue, and who issued it.
/// </summary>
/// <remarks>
/// Both are ordinary UBL elements in ordinary UBL positions — <c>cbc:IssueTime</c> straight after
/// <c>cbc:IssueDate</c>, <c>cac:SellerContact</c> last inside <c>cac:AccountingSupplierParty</c> — so this
/// edits the written document rather than the model. EN 16931 defines neither, and the model is EN 16931.
/// </remarks>
internal sealed class HrOperatorStep(Func<EInvoice, HrOperator?> operatorFor, TimeProvider clock)
    : IWritePipelineStep
{
    public void Write(WriteContext context, Action<WriteContext> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        next(context);

        if (context.Syntax != DocumentSyntax.Ubl || context.Xml.Length == 0)
        {
            return;
        }

        HrOperator? issuer = operatorFor(context.Invoice);

        if (issuer is null)
        {
            return;
        }

        XDocument document = XDocument.Parse(context.Xml, LoadOptions.PreserveWhitespace);

        if (document.Root is not { } root)
        {
            return;
        }

        AddIssueTime(root, clock);
        AddSellerContact(root, issuer);

        using var text = new Utf8StringWriter();
        document.Save(text, SaveOptions.DisableFormatting);
        context.Xml = text.ToString();
    }

    private static void AddIssueTime(XContainer root, TimeProvider clock)
    {
        if (root.Element(UblNames.Cbc + "IssueTime") is not null
            || root.Element(UblNames.Cbc + "IssueDate") is not { } issueDate)
        {
            return;
        }

        issueDate.AddAfterSelf(new XElement(
            UblNames.Cbc + "IssueTime",
            clock.GetLocalNow().ToString("HH:mm:ss", CultureInfo.InvariantCulture)));
    }

    private static void AddSellerContact(XContainer root, HrOperator issuer)
    {
        if (root.Element(UblNames.Cac + "AccountingSupplierParty") is not { } supplier
            || supplier.Element(UblNames.Cac + "SellerContact") is not null)
        {
            return;
        }

        supplier.Add(new XElement(
            UblNames.Cac + "SellerContact",
            new XElement(UblNames.Cbc + "ID", issuer.Oib.Value),
            new XElement(UblNames.Cbc + "Name", issuer.Name)));
    }

    /// <summary>A <see cref="StringWriter"/> that says UTF-8, so the declaration written is not UTF-16.</summary>
    private sealed class Utf8StringWriter() : StringWriter(CultureInfo.InvariantCulture)
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
