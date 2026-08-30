using International.EInvoicing.Building;
using International.EInvoicing.Countries.France.Identifiers;

namespace International.EInvoicing.Countries.France.Invoicing;

/// <summary>
/// What a French invoice must carry beyond EN 16931.
/// </summary>
/// <remarks>
/// These are extensions on the ordinary invoice builder rather than a French builder of their own: a French
/// invoice is an EN 16931 invoice with more on it, and having two builders would mean choosing between them
/// before knowing which country the invoice is for.
/// </remarks>
public static class FrInvoiceBuilderExtensions
{
    /// <summary>
    /// Adds everything France requires that EN 16931 does not: the invoicing case, and the three mentions.
    /// </summary>
    /// <remarks>
    /// One call for the common one — a domestic invoice from a seller, no early-payment discount offered.
    /// Where the terms differ, <see cref="InFrenchProcess"/> and <see cref="WithFrenchMention"/> say each part
    /// separately.
    /// </remarks>
    /// <param name="builder">The invoice being built.</param>
    /// <param name="businessProcess">The invoicing case (BT-23). An ordinary invoice unless said otherwise.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The invoicing case is not one the published rules accept.</exception>
    public static EInvoiceBuilder ForFrance(
        this EInvoiceBuilder builder,
        string businessProcess = FrBusinessProcess.Invoice)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .InFrenchProcess(businessProcess)
            .WithFrenchMention(FrInvoiceMention.RecoveryCostsCode, FrInvoiceMention.RecoveryCosts)
            .WithFrenchMention(FrInvoiceMention.LatePaymentPenaltiesCode, FrInvoiceMention.LatePaymentPenalties)
            .WithFrenchMention(FrInvoiceMention.EarlyPaymentDiscountCode, FrInvoiceMention.NoEarlyPaymentDiscount);
    }

    /// <summary>
    /// BT-23 — the <em>cadre de facturation</em>, from the list the published rules accept.
    /// </summary>
    /// <param name="builder">The invoice being built.</param>
    /// <param name="businessProcess">The code, from <see cref="FrBusinessProcess"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The code is not one the published rules accept.</exception>
    public static EInvoiceBuilder InFrenchProcess(this EInvoiceBuilder builder, string businessProcess)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!FrBusinessProcess.IsKnown(businessProcess))
        {
            throw new ArgumentException(
                $"'{businessProcess}' is not a French invoicing case. BR-FR-08 accepts "
                + $"{string.Join(", ", FrBusinessProcess.All)}.",
                nameof(businessProcess));
        }

        return builder.Extend(invoice => invoice.BusinessProcessType = businessProcess);
    }

    /// <summary>
    /// BT-21 and BT-22 — one of the mentions French law requires, with the subject code that identifies it.
    /// </summary>
    /// <remarks>
    /// Use it to replace a suggested wording with your own: calling it again with the same code replaces the
    /// mention rather than adding a second one.
    /// </remarks>
    /// <param name="builder">The invoice being built.</param>
    /// <param name="subjectCode">The code, from <see cref="FrInvoiceMention"/>.</param>
    /// <param name="text">What it says.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The code or the text is empty.</exception>
    public static EInvoiceBuilder WithFrenchMention(
        this EInvoiceBuilder builder,
        string subjectCode,
        string text)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return builder.Extend(invoice =>
        {
            invoice.Notes.RemoveAll(note =>
                string.Equals(note.SubjectCode.Value, subjectCode, StringComparison.Ordinal));

            invoice.Notes.Add(new Model.InvoiceNote { SubjectCode = subjectCode, Text = text });
        });
    }

    /// <summary>
    /// The seller, identified as France requires: its SIREN as the legal registration, and its VAT number.
    /// </summary>
    /// <param name="builder">The invoice being built.</param>
    /// <param name="name">The seller's legal name (BT-27).</param>
    /// <param name="siren">Its SIREN, which is checked before it is written.</param>
    /// <param name="vatIdentifier">Its VAT number (BT-31).</param>
    /// <param name="configure">Anything else about the seller — its address, its contact.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">The SIREN does not satisfy its check digit.</exception>
    public static EInvoiceBuilder FromFrenchSeller(
        this EInvoiceBuilder builder,
        string name,
        string siren,
        string vatIdentifier,
        Action<PartyBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        FrSiren checkedSiren = FrSiren.Parse(siren);

        return builder.From(seller =>
        {
            seller
                .Named(name)
                .WithVatIdentifier(vatIdentifier)
                .WithLegalRegistration(checkedSiren.Value, FrIdentifierSchemes.Siren);

            configure?.Invoke(seller);
        });
    }

    /// <summary>The buyer, identified as France requires.</summary>
    /// <param name="builder">The invoice being built.</param>
    /// <param name="name">The buyer's legal name (BT-44).</param>
    /// <param name="siren">Its SIREN, which is checked before it is written.</param>
    /// <param name="vatIdentifier">Its VAT number (BT-48).</param>
    /// <param name="configure">Anything else about the buyer.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">The SIREN does not satisfy its check digit.</exception>
    public static EInvoiceBuilder ToFrenchBuyer(
        this EInvoiceBuilder builder,
        string name,
        string siren,
        string? vatIdentifier = null,
        Action<PartyBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        FrSiren checkedSiren = FrSiren.Parse(siren);

        return builder.To(buyer =>
        {
            buyer.Named(name).WithLegalRegistration(checkedSiren.Value, FrIdentifierSchemes.Siren);

            if (vatIdentifier is not null)
            {
                buyer.WithVatIdentifier(vatIdentifier);
            }

            configure?.Invoke(buyer);
        });
    }
}
