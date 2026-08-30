using International.EInvoicing.Countries.France.EReporting.Model;
using International.EInvoicing.Countries.France.Lifecycle;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Building;

/// <summary>
/// Builds a French e-reporting transmission by naming what is being reported.
/// </summary>
/// <remarks>
/// A transmission reports transactions or payments, never both, so the two are separate entry points rather
/// than a flag: <see cref="Transactions"/> and <see cref="Payments"/>. Everything each one implies — the
/// profile, the role codes, the schemes, the totals — is filled in.
/// </remarks>
public sealed class FrEReporting
{
    private const string PlatformScheme = "0238";
    private const string CompanyScheme = "0002";

    private readonly FrEReport _report = new();

    private FrEReporting() =>
        _report.Document.TypeCode = FrEReportCodes.InitialTransmission;

    /// <summary>Starts a transmission of transactions over a period — flux 10.1 and 10.3.</summary>
    /// <exception cref="ArgumentException">The period ends before it starts.</exception>
    public static FrTransactionsBuilder Transactions(DateOnly from, DateOnly to) =>
        new(new FrEReporting(), Period(from, to));

    /// <summary>Starts a transmission of payments over a period — flux 10.2 and 10.4.</summary>
    /// <exception cref="ArgumentException">The period ends before it starts.</exception>
    public static FrPaymentsBuilder Payments(DateOnly from, DateOnly to) =>
        new(new FrEReporting(), Period(from, to));

    internal FrEReport Report => _report;

    /// <summary>The platform transmitting the report, by its four-character platform number.</summary>
    /// <exception cref="ArgumentException"><paramref name="platformIdentifier"/> is empty.</exception>
    public FrEReporting From(string platformIdentifier, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformIdentifier);

        _report.Document.Sender = new FrEReportParty
        {
            Identifier = new IdentifierField(platformIdentifier, PlatformScheme),
            Name = name,
            RoleCode = FrPartyRole.Platform,
        };

        return this;
    }

    /// <summary>The company the report is about, by its SIREN.</summary>
    /// <param name="siren">The company's SIREN.</param>
    /// <param name="name">Its name.</param>
    /// <param name="roleCode">What it is here — the seller by default, the buyer when reporting purchases.</param>
    /// <exception cref="ArgumentException"><paramref name="siren"/> is empty.</exception>
    public FrEReporting For(string siren, string name, string roleCode = FrPartyRole.Seller)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(siren);

        _report.Document.Issuer = new FrEReportParty
        {
            Identifier = new IdentifierField(siren, CompanyScheme),
            Name = name,
            RoleCode = roleCode,
        };

        return this;
    }

    /// <summary>The transmission's own identifier. One is derived from the period when this is not called.</summary>
    /// <exception cref="ArgumentException"><paramref name="identifier"/> is empty.</exception>
    public FrEReporting WithIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        _report.Document.Identifier = identifier;
        return this;
    }

    /// <summary>The name the sender gives the transmission.</summary>
    public FrEReporting Named(string name)
    {
        _report.Document.Name = name;
        return this;
    }

    /// <summary>When the transmission was created. Now is used when this is not called.</summary>
    public FrEReporting At(DateTimeOffset moment)
    {
        _report.Document.IssuedAt = moment;
        return this;
    }

    /// <summary>This transmission replaces an earlier one for the same period.</summary>
    public FrEReporting Replacing()
    {
        _report.Document.TypeCode = FrEReportCodes.Replacement;
        return this;
    }

    /// <summary>Fills in what was not said, and hands back the transmission.</summary>
    /// <exception cref="InvalidOperationException">The sender or the company was not named.</exception>
    internal FrEReport Complete(DateOnly from)
    {
        if (_report.Document.Sender is null)
        {
            throw new InvalidOperationException(
                "A transmission names the platform sending it: From(platformIdentifier, name).");
        }

        if (_report.Document.Issuer is null)
        {
            throw new InvalidOperationException(
                "A transmission names the company it is about: For(siren, name).");
        }

        if (!_report.Document.IssuedAt.IsSet)
        {
            _report.Document.IssuedAt = DateTimeOffset.UtcNow;
        }

        if (!_report.Document.Identifier.IsSet)
        {
            _report.Document.Identifier = string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{_report.Document.Issuer.Identifier.Value}-{from:yyyyMMdd}-{_report.Document.TypeCode.Value}");
        }

        return _report;
    }

    private static FrReportPeriod Period(DateOnly from, DateOnly to)
    {
        if (to <= from)
        {
            throw new ArgumentException("A reporting period ends after it starts.", nameof(to));
        }

        return new FrReportPeriod { StartDate = from, EndDate = to };
    }
}
