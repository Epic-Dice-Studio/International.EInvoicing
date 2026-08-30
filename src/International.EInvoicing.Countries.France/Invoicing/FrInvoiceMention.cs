namespace International.EInvoicing.Countries.France.Invoicing;

/// <summary>
/// The three mentions French law requires on every invoice, and their subject codes (BT-21).
/// </summary>
/// <remarks>
/// <para>
/// They are not optional and they are not conditional: an invoice without all three is rejected by
/// <c>BR-FR-05</c>, whatever else it gets right. What they must say is set by the <em>code de commerce</em>;
/// the wording is the seller's, and the codes here are what makes each one findable.
/// </para>
/// <para>
/// The suggested wordings below are the customary ones and a reasonable starting point. They are not legal
/// advice, and a business whose terms differ — a longer payment period, a discount actually offered — must
/// say so in its own words.
/// </para>
/// </remarks>
public static class FrInvoiceMention
{
    /// <summary>PMT — the fixed indemnity for recovery costs.</summary>
    public const string RecoveryCostsCode = "PMT";

    /// <summary>PMD — the late-payment penalties.</summary>
    public const string LatePaymentPenaltiesCode = "PMD";

    /// <summary>AAB — the early-payment discount, or the statement that there is none.</summary>
    public const string EarlyPaymentDiscountCode = "AAB";

    /// <summary>The customary wording for the recovery indemnity, which the code fixes at forty euros.</summary>
    public const string RecoveryCosts =
        "Indemnité forfaitaire pour frais de recouvrement en cas de retard de paiement : 40 €.";

    /// <summary>The customary wording for late-payment penalties at three times the legal interest rate.</summary>
    public const string LatePaymentPenalties =
        "Pénalités de retard exigibles le jour suivant la date de règlement figurant sur la facture, "
        + "au taux d'intérêt légal majoré, sans qu'un rappel soit nécessaire.";

    /// <summary>The wording for an invoice that offers no early-payment discount.</summary>
    public const string NoEarlyPaymentDiscount = "Escompte pour paiement anticipé : néant.";
}
