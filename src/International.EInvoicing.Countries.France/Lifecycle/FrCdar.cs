using System.Globalization;
using International.EInvoicing.Cdar.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.Lifecycle;

/// <summary>
/// Builds French lifecycle status messages by saying who reports what to whom.
/// </summary>
/// <remarks>
/// <para>
/// A lifecycle message has three parties and it is easy to fill in the wrong one: the <em>issuer</em> is who
/// reports the status, the <em>sender</em> is the approved platform that transmits it, and the
/// <em>recipient</em> is who it is for. Which of them may be what depends on the status — a platform files an
/// invoice, a buyer approves one, a seller collects on one — and a message that gets it wrong names the right
/// status and is rejected anyway.
/// </para>
/// <para>
/// So the builder reads as the sentence: <c>FromBuyer(...).SentBy(...).ToSeller(...).About(...).Approved()</c>.
/// Starting from who is speaking fixes their role, the destination fixes the profile, and the status fixes
/// the codes. Getting the direction wrong is refused with a message naming the entry point to use instead.
/// </para>
/// </remarks>
public sealed class FrCdar
{
    private const string PublicPortalIdentifier = "9998";
    private const string PublicPortalRecipient = "0000";
    private const string RegulatedProcess = "REGULATED";

    private readonly LifecycleStatusMessage _message = new();
    private readonly ReferencedDocumentStatus _reference = new();
    private readonly StatusParty _issuer;

    private Profile? _profile;
    private TimeProvider _clock = TimeProvider.System;

    private FrCdar(StatusParty issuer)
    {
        _issuer = issuer;
        _message.CoversMultipleDocuments = false;
        _message.References.Add(_reference);
    }

    /// <summary>
    /// The buyer reports the status — taken in charge, approved, disputed, refused, payment sent.
    /// </summary>
    /// <param name="siren">The buyer's SIREN.</param>
    /// <param name="name">The buyer's name.</param>
    /// <exception cref="ArgumentException"><paramref name="siren"/> is empty.</exception>
    public static FrCdar FromBuyer(string siren, string? name = null) =>
        FromCompany(siren, name, FrPartyRole.Buyer);

    /// <summary>The seller reports the status — a collection, above all.</summary>
    /// <param name="siren">The seller's SIREN.</param>
    /// <param name="name">The seller's name.</param>
    /// <exception cref="ArgumentException"><paramref name="siren"/> is empty.</exception>
    public static FrCdar FromSeller(string siren, string? name = null) =>
        FromCompany(siren, name, FrPartyRole.Seller);

    /// <summary>
    /// A platform reports the status — filed, received, made available, rejected.
    /// </summary>
    /// <remarks>
    /// A platform reports on its own behalf, so it is both the issuer and the sender: there is no
    /// <see cref="SentBy(string, string)"/> to add afterwards.
    /// </remarks>
    /// <param name="platformIdentifier">The platform's four-character identifier.</param>
    /// <param name="name">The platform's name.</param>
    /// <exception cref="ArgumentException"><paramref name="platformIdentifier"/> is empty.</exception>
    public static FrCdar FromPlatform(string platformIdentifier, string? name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformIdentifier);

        var platform = new StatusParty
        {
            GlobalIdentifier = new IdentifierField(platformIdentifier, FrPartyScheme.Platform),
            RoleCode = FrPartyRole.Platform,
        };

        if (name is not null)
        {
            platform.Name = name;
        }

        var builder = new FrCdar(platform);
        builder._message.Sender = platform;
        return builder;
    }

    /// <summary>Whoever reports the status, described in full.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="issuer"/> is <c>null</c>.</exception>
    public static FrCdar From(Action<FrPartyBuilder> issuer)
    {
        ArgumentNullException.ThrowIfNull(issuer);

        return new FrCdar(FrPartyBuilder.Build(issuer));
    }

    /// <summary>
    /// The approved platform transmitting the message on the issuer's behalf.
    /// </summary>
    /// <remarks>
    /// A trading party does not put messages on the network itself; its platform does. Needed for every
    /// status a party reports, and implied when the issuer is itself a platform.
    /// </remarks>
    /// <param name="platformIdentifier">The platform's four-character identifier.</param>
    /// <param name="name">The platform's name.</param>
    /// <exception cref="ArgumentException"><paramref name="platformIdentifier"/> is empty.</exception>
    public FrCdar SentBy(string platformIdentifier, string? name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformIdentifier);

        return SentBy(platform =>
        {
            platform.Platform(platformIdentifier);

            if (name is not null)
            {
                platform.Named(name);
            }
        });
    }

    /// <summary>The platform transmitting the message, described in full.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="sender"/> is <c>null</c>.</exception>
    public FrCdar SentBy(Action<FrPartyBuilder> sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _message.Sender = FrPartyBuilder.Build(sender);
        return this;
    }

    /// <summary>
    /// The status goes to the seller, through approved platforms.
    /// </summary>
    /// <param name="siren">The seller's SIREN.</param>
    /// <param name="name">The seller's name.</param>
    /// <param name="statusAddress">Where its statuses are delivered, its routing address.</param>
    /// <exception cref="ArgumentException"><paramref name="siren"/> is empty.</exception>
    public FrCdar ToSeller(string siren, string? name = null, string? statusAddress = null) =>
        ToCompany(siren, name, statusAddress, FrPartyRole.Seller);

    /// <summary>The status goes to the buyer, through approved platforms.</summary>
    /// <param name="siren">The buyer's SIREN.</param>
    /// <param name="name">The buyer's name.</param>
    /// <param name="statusAddress">Where its statuses are delivered, its routing address.</param>
    /// <exception cref="ArgumentException"><paramref name="siren"/> is empty.</exception>
    public FrCdar ToBuyer(string siren, string? name = null, string? statusAddress = null) =>
        ToCompany(siren, name, statusAddress, FrPartyRole.Buyer);

    /// <summary>
    /// The status goes to a trading partner, described in full. The public portal is added as a second
    /// recipient, which this profile expects.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="recipient"/> is <c>null</c>.</exception>
    public FrCdar ToPartner(Action<FrPartyBuilder> recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        UseProfile(FrProfiles.LifecycleStatusToPartner);
        _message.BusinessProcessType = RegulatedProcess;
        _message.Recipients.Add(FrPartyBuilder.Build(recipient));
        _message.Recipients.Add(new StatusParty
        {
            GlobalIdentifier = new IdentifierField(PublicPortalIdentifier, FrPartyScheme.Platform),
            Name = "PPF",
            RoleCode = FrPartyRole.PublicPortal,
        });

        return this;
    }

    /// <summary>
    /// The status is reported to the public portal rather than sent to a partner — a different profile, not
    /// a different destination for the same one.
    /// </summary>
    public FrCdar ToPublicPortal()
    {
        UseProfile(FrProfiles.LifecycleStatusToPublicPortal);
        _message.Recipients.Add(new StatusParty
        {
            GlobalIdentifier = new IdentifierField(PublicPortalRecipient, FrPartyScheme.Platform),
            RoleCode = FrPartyRole.PublicPortal,
        });

        _reference.Extensions.Add(FrExtensions.ReferenceTypeCode(FrProfiles.LifecycleStatusToPublicPortal.Id.Value));
        return this;
    }

    /// <summary>
    /// Where "now" comes from, when a status is reported without a moment.
    /// </summary>
    /// <remarks>
    /// A message carries three timestamps and derives its identifier from one of them, so a test that cannot
    /// fix the clock cannot assert what it built. Pass a <see cref="TimeProvider"/> and it can.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="clock"/> is <c>null</c>.</exception>
    public FrCdar UsingClock(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        _clock = clock;
        return this;
    }

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
    public LifecycleStatusMessage Collected(FrCollectedAmount collected) => Collected([collected], null);

    /// <summary>212 — the invoice has been collected, at a moment you name.</summary>
    /// <param name="collected">How much was collected, and at which VAT rate.</param>
    /// <param name="at">When the status occurred.</param>
    public LifecycleStatusMessage Collected(FrCollectedAmount collected, DateTimeOffset? at) =>
        Collected([collected], at);

    /// <summary>212 — the invoice has been collected, at more than one VAT rate.</summary>
    /// <param name="collected">How much was collected, once per VAT rate. At least one is required.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collected"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="collected"/> is empty.</exception>
    public LifecycleStatusMessage Collected(IEnumerable<FrCollectedAmount> collected) =>
        Collected(collected, null);

    /// <summary>212 — the invoice has been collected, at more than one VAT rate.</summary>
    /// <param name="collected">How much was collected, once per VAT rate. At least one is required.</param>
    /// <param name="at">When the status occurred.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collected"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="collected"/> is empty.</exception>
    public LifecycleStatusMessage Collected(IEnumerable<FrCollectedAmount> collected, DateTimeOffset? at)
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

        DateTimeOffset moment = at ?? _clock.GetUtcNow();

        EnsureConsistent(status);

        _message.Issuer = _issuer;
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

    private static FrCdar FromCompany(string siren, string? name, string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(siren);

        var party = new StatusParty
        {
            GlobalIdentifier = new IdentifierField(siren, FrPartyScheme.Company),
            RoleCode = role,
        };

        if (name is not null)
        {
            party.Name = name;
        }

        return new FrCdar(party);
    }

    private FrCdar ToCompany(string siren, string? name, string? statusAddress, string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(siren);

        return ToPartner(partner =>
        {
            partner.Company(siren).InRole(role);

            if (name is not null)
            {
                partner.Named(name);
            }

            if (statusAddress is not null)
            {
                partner.ReachableAt(statusAddress);
            }
        });
    }

    private void UseProfile(Profile profile)
    {
        if (_profile is not null)
        {
            throw new InvalidOperationException(
                $"This message already goes to {_profile.Name}. A message has one destination: either a "
                + "trading partner or the public portal, not both.");
        }

        _profile = profile;
        _message.SpecificationIdentifier = profile.Id;
    }

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
    /// <summary>
    /// Checks that the message says what this status needs it to say, before it is written rather than after
    /// it is rejected.
    /// </summary>
    private void EnsureConsistent(FrLifecycleStatus status)
    {
        if (_profile is null)
        {
            throw new InvalidOperationException(
                $"Status {status} has no destination. Say where it goes: ToSeller(...), ToBuyer(...), "
                + "ToPartner(...) or ToPublicPortal().");
        }

        bool issuerIsPlatform = _issuer.RoleCode.Value == FrPartyRole.Platform;

        if (status.IsBusinessEvent && issuerIsPlatform)
        {
            throw new InvalidOperationException(
                $"Status {status} is reported by a trading party, not by a platform: start from "
                + "FrCdar.FromSeller(siren) for a collection, FrCdar.FromBuyer(siren) for everything else.");
        }

        if (!status.IsBusinessEvent && !issuerIsPlatform)
        {
            throw new InvalidOperationException(
                $"Status {status} is reported by the platform handling the invoice, not by a trading party: "
                + "start from FrCdar.FromPlatform(identifier).");
        }

        if (_message.Sender is null)
        {
            throw new InvalidOperationException(
                $"Status {status} is transmitted by an approved platform. Name it with "
                + "SentBy(platformIdentifier, name).");
        }
    }

    /// <summary>
    /// The identifier pattern the DGFiP samples use: the invoice number, the status, when it occurred, then
    /// the invoice type and date.
    /// </summary>
    private string DerivedIdentifier(FrLifecycleStatus status, DateTimeOffset moment)
    {
        string invoice = _reference.DocumentIdentifier.Value ?? "UNKNOWN";
        string type = _reference.DocumentTypeCode.Value ?? "380";
        string issued = _reference.DocumentIssueDate.Value?.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            ?? moment.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        return $"{invoice}_{status.Code}_{moment.UtcDateTime:yyyyMMddHHmmss}#{type}_{issued}";
    }
}
