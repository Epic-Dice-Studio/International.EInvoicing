namespace International.EInvoicing.Countries.France.Lifecycle;

/// <summary>
/// A French lifecycle status, and the codes a message carrying it must also set.
/// </summary>
/// <remarks>
/// A status is not one code but three: the status itself, the type of the acknowledgement carrying it, and a
/// document status code. Getting the accompanying two wrong produces a message that names the right status
/// and is rejected anyway, which is why nothing here asks a caller to supply them.
/// </remarks>
/// <param name="Code">The status code, as it appears in <c>ram:ProcessConditionCode</c>.</param>
/// <param name="Label">The label the DGFiP samples use in <c>ram:ProcessCondition</c>.</param>
/// <param name="AcknowledgementTypeCode">The <c>TypeCode</c> of the acknowledgement carrying this status.</param>
/// <param name="DocumentStatusCode">The <c>StatusCode</c> of the referenced document.</param>
/// <param name="IsVerified">
/// Whether the accompanying codes were read from the DGFiP sample messages. Where they were not, they follow
/// the pattern the samples establish — platform events use 305, business events use 23 — and should be
/// confirmed against the specification before production use. <see cref="WithCodes"/> overrides them.
/// </param>
public sealed record FrLifecycleStatus(
    string Code,
    string Label,
    string AcknowledgementTypeCode,
    string DocumentStatusCode,
    bool IsVerified = true)
{
    private const string PlatformEvent = "305";
    private const string BusinessEvent = "23";

    /// <summary>200 — the invoice was filed on the sender's platform.</summary>
    public static FrLifecycleStatus Filed { get; } = new("200", "Déposée", PlatformEvent, "10");

    /// <summary>201 — the platform issued the invoice.</summary>
    public static FrLifecycleStatus IssuedByPlatform { get; } =
        new("201", "Émise par plateforme", PlatformEvent, "10", IsVerified: false);

    /// <summary>202 — the recipient's platform received the invoice.</summary>
    public static FrLifecycleStatus Received { get; } = new("202", "Reçue par la plateforme", PlatformEvent, "43");

    /// <summary>203 — the invoice was made available to the recipient.</summary>
    public static FrLifecycleStatus MadeAvailable { get; } = new("203", "Mise à disposition", PlatformEvent, "48");

    /// <summary>204 — the recipient took the invoice in charge.</summary>
    public static FrLifecycleStatus TakenInCharge { get; } = new("204", "Prise en charge", BusinessEvent, "45");

    /// <summary>205 — the recipient approved the invoice.</summary>
    public static FrLifecycleStatus Approved { get; } = new("205", "Approuvée", BusinessEvent, "1");

    /// <summary>207 — the recipient disputes the invoice. Carries a reason.</summary>
    public static FrLifecycleStatus Disputed { get; } = new("207", "En_litige", BusinessEvent, "46");

    /// <summary>210 — the recipient refuses the invoice. Carries a reason.</summary>
    public static FrLifecycleStatus Refused { get; } =
        new("210", "Refusée", BusinessEvent, "46", IsVerified: false);

    /// <summary>211 — payment has been sent.</summary>
    public static FrLifecycleStatus PaymentSent { get; } = new("211", "Paiement transmis", BusinessEvent, "47");

    /// <summary>212 — the invoice has been collected.</summary>
    public static FrLifecycleStatus Collected { get; } = new("212", "Encaissée", BusinessEvent, "47");

    /// <summary>213 — the invoice was rejected, for a technical or structural reason. Carries a reason.</summary>
    public static FrLifecycleStatus Rejected { get; } =
        new("213", "Rejetée", PlatformEvent, "10", IsVerified: false);

    /// <summary>Every status this library knows.</summary>
    public static IReadOnlyList<FrLifecycleStatus> All { get; } =
    [
        Filed,
        IssuedByPlatform,
        Received,
        MadeAvailable,
        TakenInCharge,
        Approved,
        Disputed,
        Refused,
        PaymentSent,
        Collected,
        Rejected,
    ];

    /// <summary>Whether this status is one a sender must give a reason for.</summary>
    public bool RequiresReason => this == Disputed || this == Refused || this == Rejected;

    /// <summary>Finds a status by its code, or <c>null</c> when it is not one of the eleven.</summary>
    public static FrLifecycleStatus? FromCode(string? code) =>
        All.FirstOrDefault(status => string.Equals(status.Code, code, StringComparison.Ordinal));

    /// <summary>Replaces the accompanying codes, for a status this library has not verified.</summary>
    public FrLifecycleStatus WithCodes(string acknowledgementTypeCode, string documentStatusCode) =>
        this with
        {
            AcknowledgementTypeCode = acknowledgementTypeCode,
            DocumentStatusCode = documentStatusCode,
            IsVerified = true,
        };

    /// <inheritdoc />
    public override string ToString() => $"{Code} {Label}";
}
