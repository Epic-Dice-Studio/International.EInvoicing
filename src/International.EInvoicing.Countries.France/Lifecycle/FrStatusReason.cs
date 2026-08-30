namespace International.EInvoicing.Countries.France.Lifecycle;

/// <summary>
/// The reasons a French lifecycle status can carry, as the DGFiP codes them.
/// </summary>
/// <remarks>
/// A status that carries a reason accepts only some of these, and which ones depends on the status — a
/// rejection and a dispute do not share a vocabulary. <see cref="AllowedFor"/> answers that, so a caller can
/// offer the right choice rather than discovering the wrong one at validation.
/// </remarks>
public static class FrStatusReason
{
    /// <summary>A reason outside the coded list, spelled out in words alongside.</summary>
    public const string Other = "AUTRE";

    /// <summary>The bank details are wrong.</summary>
    public const string BankDetailsWrong = "COORD_BANC_ERR";

    /// <summary>The VAT rate is wrong.</summary>
    public const string VatRateWrong = "TX_TVA_ERR";

    /// <summary>The total is wrong.</summary>
    public const string TotalWrong = "MONTANTTOTAL_ERR";

    /// <summary>A calculation is wrong.</summary>
    public const string CalculationWrong = "CALCUL_ERR";

    /// <summary>The invoice does not conform to what was agreed.</summary>
    public const string NotCompliant = "NON_CONFORME";

    /// <summary>The invoice is a duplicate.</summary>
    public const string Duplicate = "DOUBLON";

    /// <summary>The same invoice was issued twice.</summary>
    public const string InvoicedTwice = "DOUBLE_FACT";

    /// <summary>The recipient is unknown.</summary>
    public const string RecipientUnknown = "DEST_INC";

    /// <summary>The recipient is wrong.</summary>
    public const string RecipientWrong = "DEST_ERR";

    /// <summary>The transaction is unknown.</summary>
    public const string TransactionUnknown = "TRANSAC_INC";

    /// <summary>The issuer is unknown.</summary>
    public const string IssuerUnknown = "EMMET_INC";

    /// <summary>The contract has ended.</summary>
    public const string ContractEnded = "CONTRAT_TERM";

    /// <summary>The order reference is wrong.</summary>
    public const string OrderWrong = "CMD_ERR";

    /// <summary>The address is wrong.</summary>
    public const string AddressWrong = "ADR_ERR";

    /// <summary>The SIRET is wrong.</summary>
    public const string SiretWrong = "SIRET_ERR";

    /// <summary>The routing code is wrong.</summary>
    public const string RoutingCodeWrong = "CODE_ROUTAGE_ERR";

    /// <summary>Routing failed.</summary>
    public const string RoutingFailed = "ROUTAGE_ERR";

    /// <summary>The contract reference is missing.</summary>
    public const string ContractReferenceMissing = "REF_CT_ABSENT";

    /// <summary>The reference is wrong.</summary>
    public const string ReferenceWrong = "REF_ERR";

    /// <summary>A unit price is wrong.</summary>
    public const string UnitPriceWrong = "PU_ERR";

    /// <summary>A discount is wrong.</summary>
    public const string DiscountWrong = "REM_ERR";

    /// <summary>A quantity is wrong.</summary>
    public const string QuantityWrong = "QTE_ERR";

    /// <summary>An item is wrong.</summary>
    public const string ItemWrong = "ART_ERR";

    /// <summary>The payment method is wrong.</summary>
    public const string PaymentMeansWrong = "MODPAI_ERR";

    /// <summary>The goods or services are not of the agreed quality.</summary>
    public const string QualityWrong = "QUALITE_ERR";

    /// <summary>The delivery is incomplete.</summary>
    public const string DeliveryIncomplete = "LIVR_INCOMP";

    /// <summary>Supporting evidence is missing.</summary>
    public const string EvidenceMissing = "JUSTIF_ABS";

    /// <summary>The invoice was never transmitted.</summary>
    public const string NotTransmitted = "NON_TRANSMISE";

    /// <summary>The invoice fails a semantic rule.</summary>
    public const string RejectedSemantic = "REJ_SEMAN";

    /// <summary>The invoice fails a uniqueness rule.</summary>
    public const string RejectedUniqueness = "REJ_UNI";

    /// <summary>The invoice fails a coherence rule.</summary>
    public const string RejectedCoherence = "REJ_COH";

    /// <summary>The invoice fails an addressing rule.</summary>
    public const string RejectedAddressing = "REJ_ADR";

    /// <summary>The invoice fails a public-sector content rule.</summary>
    public const string RejectedPublicSectorContent = "REJ_CONT_B2G";

    /// <summary>An attachment is referenced but not present.</summary>
    public const string RejectedAttachmentReference = "REJ_REF_PJ";

    /// <summary>An attachment could not be associated with the invoice.</summary>
    public const string RejectedAttachmentAssociation = "REJ_ASS_PJ";

    /// <summary>The service was withdrawn manually.</summary>
    public const string PublicSectorServiceWithdrawn = "RETRAIT_MAN_SERV";

    /// <summary>The subcontractor or assignee is not declared.</summary>
    public const string PublicSectorSubcontractorUndeclared = "ST_CT_NON_DECLAR";

    /// <summary>A credit note removes the amount owed.</summary>
    public const string PublicSectorCreditNoteRemoval = "SUPPR_COMP_AVOIR";

    /// <summary>Payment is transferred to a public accounting office.</summary>
    public const string PublicSectorPaymentTransferred = "TRANSF_PMNT_REGIE";

    private static readonly string[] DisputeReasons =
    [
        Other, BankDetailsWrong, VatRateWrong, TotalWrong, CalculationWrong, NotCompliant, Duplicate,
        RecipientUnknown, RecipientWrong, TransactionUnknown, IssuerUnknown, ContractEnded, InvoicedTwice,
        OrderWrong, AddressWrong, SiretWrong, RoutingCodeWrong, ContractReferenceMissing, ReferenceWrong,
        UnitPriceWrong, DiscountWrong, QuantityWrong, ItemWrong, PaymentMeansWrong, QualityWrong,
        DeliveryIncomplete,
    ];

    private static readonly string[] RefusalReasons =
    [
        VatRateWrong, TotalWrong, CalculationWrong, NotCompliant, Duplicate, RecipientWrong,
        TransactionUnknown, IssuerUnknown, ContractEnded, InvoicedTwice, OrderWrong, AddressWrong,
        ContractReferenceMissing,
    ];

    private static readonly string[] PublicSectorRefusalReasons =
    [
        .. RefusalReasons,
        PublicSectorServiceWithdrawn, PublicSectorSubcontractorUndeclared, PublicSectorCreditNoteRemoval,
        PublicSectorPaymentTransferred, Other, BankDetailsWrong, DeliveryIncomplete,
    ];

    private static readonly string[] RejectionReasons =
    [
        TotalWrong, CalculationWrong, Duplicate, RecipientUnknown, AddressWrong, RejectedSemantic,
        RejectedUniqueness, RejectedCoherence, RejectedAddressing, RejectedPublicSectorContent,
        RejectedAttachmentReference, RejectedAttachmentAssociation,
    ];

    /// <summary>
    /// The reasons a status accepts. Empty for a status that carries none.
    /// </summary>
    /// <param name="status">The status the reason will accompany.</param>
    /// <param name="publicSector">
    /// Whether the message is sent by the public-sector platform, which accepts seven reasons the others
    /// do not.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="status"/> is <c>null</c>.</exception>
    /// <remarks>
    /// A convenience for building a list a user can choose from. The published rules remain the authority:
    /// nothing here refuses a code they would accept.
    /// </remarks>
    public static IReadOnlyList<string> AllowedFor(FrLifecycleStatus status, bool publicSector = false)
    {
        ArgumentNullException.ThrowIfNull(status);

        if (status == FrLifecycleStatus.Disputed)
        {
            return DisputeReasons;
        }

        if (status == FrLifecycleStatus.Refused)
        {
            return publicSector ? PublicSectorRefusalReasons : RefusalReasons;
        }

        return status == FrLifecycleStatus.Rejected ? RejectionReasons : [];
    }
}
