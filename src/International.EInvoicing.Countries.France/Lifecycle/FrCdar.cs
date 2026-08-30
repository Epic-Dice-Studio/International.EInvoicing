using International.EInvoicing.Cdar.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.Lifecycle;

/// <summary>
/// Builds French lifecycle status messages by naming the status, not the codes behind it.
/// </summary>
/// <remarks>
/// <para>
/// Sending a status to a trading partner and reporting one to the public portal are two different profiles,
/// which is why they are two entry points rather than a flag. Each fills in what its profile implies.
/// </para>
/// <para>
/// Everything a status implies — the acknowledgement type code, the document status code, the label — comes
/// from <see cref="FrLifecycleStatus"/>. A caller names <c>Refused</c> and gets a message that is complete.
/// </para>
/// </remarks>
public sealed class FrCdar
{
    private const string PublicPortalIdentifier = "9998";
    private const string PublicPortalRecipient = "0000";
    private const string RegulatedProcess = "REGULATED";

    private readonly LifecycleStatusMessage _message = new();
    private readonly Profile _profile;
    private readonly ReferencedDocumentStatus _reference = new();
    private StatusParty? _businessIssuer;

    private FrCdar(Profile profile)
    {
        _profile = profile;
        _message.SpecificationIdentifier = profile.Id;
        _message.CoversMultipleDocuments = false;

        if (profile == FrProfiles.LifecycleStatusToPartner)
        {
            _message.BusinessProcessType = RegulatedProcess;
        }

        _message.References.Add(_reference);
    }

    /// <summary>
    /// Starts a status message for a trading partner, exchanged through approved platforms. The public
    /// portal is added as a second recipient, which this profile expects.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="recipient"/> is <c>null</c>.</exception>
    public static FrCdar ToPartner(Action<FrPartyBuilder> recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        var builder = new FrCdar(FrProfiles.LifecycleStatusToPartner);
        builder._message.Recipients.Add(FrPartyBuilder.Build(recipient));
        builder._message.Recipients.Add(new StatusParty
        {
            GlobalIdentifier = new IdentifierField(PublicPortalIdentifier, FrPartyScheme.Platform),
            Name = "PPF",
            RoleCode = FrPartyRole.PublicPortal,
        });

        return builder;
    }

    /// <summary>Starts a status message reported to the public portal.</summary>
    public static FrCdar ToPublicPortal()
    {
        var builder = new FrCdar(FrProfiles.LifecycleStatusToPublicPortal);
        builder._message.Recipients.Add(new StatusParty
        {
            GlobalIdentifier = new IdentifierField(PublicPortalRecipient, FrPartyScheme.Platform),
            RoleCode = FrPartyRole.PublicPortal,
        });

        builder._reference.Extensions.Add(FrExtensions.ReferenceTypeCode(builder._profile.Id.Value));
        return builder;
    }

    /// <summary>The platform sending the status.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="sender"/> is <c>null</c>.</exception>
    public FrCdar From(Action<FrPartyBuilder> sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _message.Sender = FrPartyBuilder.Build(sender);
        return this;
    }

    /// <summary>
    /// The business party reporting the status — the buyer approving an invoice, the seller collecting on
    /// one.
    /// </summary>
    /// <remarks>
    /// Business statuses are reported by a party, not by a platform: a message that names the sending
    /// platform as their issuer is rejected. Platform statuses need nothing here.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="issuer"/> is <c>null</c>.</exception>
    public FrCdar IssuedBy(Action<FrPartyBuilder> issuer)
    {
        ArgumentNullException.ThrowIfNull(issuer);

        _businessIssuer = FrPartyBuilder.Build(issuer);
        return this;
    }

    /// <summary>
    /// The buyer reports the status — taken in charge, approved, disputed, refused, payment sent.
    /// </summary>
    /// <param name="siren">The buyer's SIREN.</param>
    /// <param name="name">The buyer's name.</param>
    /// <exception cref="ArgumentException"><paramref name="siren"/> is empty.</exception>
    public FrCdar IssuedByBuyer(string siren, string? name = null) =>
        IssuedByCompany(siren, name, FrPartyRole.Buyer);

    /// <summary>The seller reports the status — a collection, above all.</summary>
    /// <param name="siren">The seller's SIREN.</param>
    /// <param name="name">The seller's name.</param>
    /// <exception cref="ArgumentException"><paramref name="siren"/> is empty.</exception>
    public FrCdar IssuedBySeller(string siren, string? name = null) =>
        IssuedByCompany(siren, name, FrPartyRole.Seller);

    /// <summary>Which invoice the status is about.</summary>
    /// <param name="invoiceNumber">The invoice's BT-1.</param>
    /// <param name="invoiceIssueDate">The invoice's BT-2.</param>
    /// <param name="invoiceTypeCode">The invoice's BT-3. Defaults to <c>380</c>, a commercial invoice.</param>
    /// <param name="issuerIdentifier">Who issued the invoice, when it is not the sender of this message.</param>
    /// <exception cref="ArgumentException"><paramref name="invoiceNumber"/> is empty.</exception>
    public FrCdar About(
        string invoiceNumber,
        DateOnly invoiceIssueDate,
        string invoiceTypeCode = "380",
        string? issuerIdentifier = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);

        _reference.DocumentIdentifier = invoiceNumber;
        _reference.DocumentIssueDate = invoiceIssueDate;
        _reference.DocumentTypeCode = invoiceTypeCode;

        if (issuerIdentifier is not null)
        {
            _reference.Issuer = new StatusParty
            {
                GlobalIdentifier = new IdentifierField(issuerIdentifier, FrPartyScheme.Company),
            };
        }

        return this;
    }

    /// <summary>When the invoice was received, if that differs from when the status is being reported.</summary>
    public FrCdar ReceivedAt(DateTimeOffset moment)
    {
        _reference.ReceivedAt = moment;
        return this;
    }

    /// <summary>The message's own identifier. One is derived from the status when this is not called.</summary>
    /// <exception cref="ArgumentException"><paramref name="identifier"/> is empty.</exception>
    public FrCdar WithIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        _message.Identifier = identifier;
        return this;
    }

    /// <summary>200 — the invoice was filed on the sender's platform.</summary>
    public LifecycleStatusMessage Filed(DateTimeOffset? at = null) => With(FrLifecycleStatus.Filed, at);

    /// <summary>201 — the platform issued the invoice.</summary>
    public LifecycleStatusMessage IssuedByPlatform(DateTimeOffset? at = null) =>
        With(FrLifecycleStatus.IssuedByPlatform, at);

    /// <summary>202 — the recipient's platform received the invoice.</summary>
    public LifecycleStatusMessage Received(DateTimeOffset? at = null) => With(FrLifecycleStatus.Received, at);

    /// <summary>203 — the invoice was made available to the recipient.</summary>
    public LifecycleStatusMessage MadeAvailable(DateTimeOffset? at = null) =>
        With(FrLifecycleStatus.MadeAvailable, at);

    /// <summary>204 — the recipient took the invoice in charge.</summary>
    public LifecycleStatusMessage TakenInCharge(DateTimeOffset? at = null) =>
        With(FrLifecycleStatus.TakenInCharge, at);

    /// <summary>205 — the recipient approved the invoice.</summary>
    public LifecycleStatusMessage Approved(DateTimeOffset? at = null) => With(FrLifecycleStatus.Approved, at);

    /// <summary>207 — the recipient disputes the invoice.</summary>
    /// <param name="reasonCode">Why, as a code from the DGFiP list.</param>
    /// <param name="reason">Why, in words.</param>
    /// <param name="at">When the status occurred.</param>
    /// <param name="requestedActionCode">What the sender expects in return, from <see cref="FrRequestedAction"/>.</param>
    /// <param name="requestedAction">What the sender expects in return, in words.</param>
    public LifecycleStatusMessage Disputed(
        string reasonCode,
        string reason,
        DateTimeOffset? at = null,
        string? requestedActionCode = null,
        string? requestedAction = null) =>
        WithReason(FrLifecycleStatus.Disputed, reasonCode, reason, at, requestedActionCode, requestedAction);

    /// <summary>210 — the recipient refuses the invoice.</summary>
    /// <param name="reasonCode">Why, as a code from the DGFiP list.</param>
    /// <param name="reason">Why, in words.</param>
    /// <param name="at">When the status occurred.</param>
    /// <param name="requestedActionCode">What the sender expects in return, from <see cref="FrRequestedAction"/>.</param>
    /// <param name="requestedAction">What the sender expects in return, in words.</param>
    public LifecycleStatusMessage Refused(
        string reasonCode,
        string reason,
        DateTimeOffset? at = null,
        string? requestedActionCode = null,
        string? requestedAction = null) =>
        WithReason(FrLifecycleStatus.Refused, reasonCode, reason, at, requestedActionCode, requestedAction);

    /// <summary>213 — the invoice was rejected for a technical or structural reason.</summary>
    /// <param name="reasonCode">Why, as a code from the DGFiP list.</param>
    /// <param name="reason">Why, in words.</param>
    /// <param name="at">When the status occurred.</param>
    /// <param name="requestedActionCode">What the sender expects in return, from <see cref="FrRequestedAction"/>.</param>
    /// <param name="requestedAction">What the sender expects in return, in words.</param>
    public LifecycleStatusMessage Rejected(
        string reasonCode,
        string reason,
        DateTimeOffset? at = null,
        string? requestedActionCode = null,
        string? requestedAction = null) =>
        WithReason(FrLifecycleStatus.Rejected, reasonCode, reason, at, requestedActionCode, requestedAction);

    /// <summary>211 — payment has been sent.</summary>
    public LifecycleStatusMessage PaymentSent(DateTimeOffset? at = null) =>
        With(FrLifecycleStatus.PaymentSent, at);

    /// <summary>212 — the invoice has been collected.</summary>
    /// <param name="collected">How much was collected, and at which VAT rate.</param>
    /// <param name="at">When the status occurred.</param>
    public LifecycleStatusMessage Collected(FrCollectedAmount collected, DateTimeOffset? at = null) =>
        Collected([collected], at);

    /// <summary>212 — the invoice has been collected, at more than one VAT rate.</summary>
    /// <param name="collected">How much was collected, once per VAT rate. At least one is required.</param>
    /// <param name="at">When the status occurred.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collected"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="collected"/> is empty.</exception>
    public LifecycleStatusMessage Collected(IEnumerable<FrCollectedAmount> collected, DateTimeOffset? at = null)
    {
        ArgumentNullException.ThrowIfNull(collected);

        DocumentStatusDetail detail = Detail();

        foreach (FrCollectedAmount amount in collected)
        {
            detail.Characteristics.Add(new DocumentStatusCharacteristic
            {
                TypeCode = FrStatusValueType.CollectedAmount,
                ValueChanged = false,
                ValueAmount = new AmountField(amount.Amount, amount.CurrencyCode),
                ValuePercent = amount.VatRate,
            });
        }

        if (detail.Characteristics.Count == 0)
        {
            _reference.StatusDetails.Remove(detail);
            throw new ArgumentException(
                "A collection status must say how much was collected, at least once.",
                nameof(collected));
        }

        return With(FrLifecycleStatus.Collected, at);
    }

    /// <summary>Any status, including one carrying codes you supplied yourself.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="status"/> is <c>null</c>.</exception>
    public LifecycleStatusMessage With(FrLifecycleStatus status, DateTimeOffset? at = null)
    {
        ArgumentNullException.ThrowIfNull(status);

        DateTimeOffset moment = at ?? DateTimeOffset.UtcNow;

        _message.Issuer = IssuerFor(status);
        _message.TypeCode = status.AcknowledgementTypeCode;
        _message.StatusIssuedAt = moment;
        _message.IssuedAt = _message.IssuedAt.IsSet ? _message.IssuedAt : moment;

        _reference.StatusCode = status.DocumentStatusCode;
        _reference.ProcessConditionCode = status.Code;
        _reference.ProcessCondition = status.Label;

        if (!_reference.ReceivedAt.IsSet)
        {
            _reference.ReceivedAt = moment;
        }

        if (!_message.Identifier.IsSet)
        {
            _message.Identifier = DerivedIdentifier(status, moment);
        }

        return _message;
    }

    private LifecycleStatusMessage WithReason(
        FrLifecycleStatus status,
        string reasonCode,
        string reason,
        DateTimeOffset? at,
        string? requestedActionCode,
        string? requestedAction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        DocumentStatusDetail detail = Detail();
        detail.ReasonCode = reasonCode;
        detail.Reason = reason;
        detail.RequestedActionCode = requestedActionCode is null ? CodeField.Unset : requestedActionCode;
        detail.RequestedAction = requestedAction is null ? TextField.Unset : requestedAction;

        _reference.Reason = reason;
        return With(status, at);
    }

    private FrCdar IssuedByCompany(string siren, string? name, string role) =>
        IssuedBy(party =>
        {
            party.Company(siren).InRole(role);

            if (name is not null)
            {
                party.Named(name);
            }
        });

    /// <summary>
    /// A status detail, numbered as the DGFiP requires. Details are numbered from one within a reference.
    /// </summary>
    private DocumentStatusDetail Detail()
    {
        var detail = new DocumentStatusDetail { SequenceNumber = _reference.StatusDetails.Count + 1 };
        _reference.StatusDetails.Add(detail);
        return detail;
    }

    /// <summary>
    /// Who the message says issued the status. A platform event is issued by the platform that sends it; a
    /// business event is issued by a trading party, and saying otherwise is rejected.
    /// </summary>
    private StatusParty IssuerFor(FrLifecycleStatus status)
    {
        if (_businessIssuer is not null)
        {
            return _businessIssuer;
        }

        if (status.IsBusinessEvent)
        {
            throw new InvalidOperationException(
                $"Status {status.Code} ({status.Label}) is reported by a trading party, not by a platform. "
                + "Name that party with IssuedBy(party => party.Company(siren).AsBuyer()) — the seller for a "
                + "collection, the buyer for everything else. Platform statuses need nothing.");
        }

        return _message.Sender
            ?? throw new InvalidOperationException(
                $"Status {status.Code} ({status.Label}) is reported by the sending platform. Name it with "
                + "From(from => from.Platform(identifier, name)).");
    }

    /// <summary>
    /// The identifier pattern the DGFiP samples use: the invoice number, the status, when it occurred, then
    /// the invoice type and date.
    /// </summary>
    private string DerivedIdentifier(FrLifecycleStatus status, DateTimeOffset moment)
    {
        string invoice = _reference.DocumentIdentifier.Value ?? "UNKNOWN";
        string type = _reference.DocumentTypeCode.Value ?? "380";
        string issued = _reference.DocumentIssueDate.Value?.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture)
            ?? moment.UtcDateTime.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);

        return $"{invoice}_{status.Code}_{moment.UtcDateTime:yyyyMMddHHmmss}#{type}_{issued}";
    }
}
