using International.EInvoicing.Countries.France.EReporting.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Building;

/// <summary>
/// Builds an invoice reported to the tax administration — flux 10.1.
/// </summary>
/// <remarks>
/// This is not an EN 16931 invoice and does not pretend to be one. It carries what the administration asks
/// for: who sold, who bought or at least where they are, and what the VAT comes to. Anything the builder does
/// not cover is reachable through <see cref="Extend"/>.
/// </remarks>
public sealed class FrReportedInvoiceBuilder
{
    private const string CompanyScheme = "0002";
    private const string VatQualifier = "VAT";

    private readonly FrReportedInvoice _invoice = new();
    private readonly List<Action<FrReportedInvoice>> _extensions = [];

    internal FrReportedInvoiceBuilder()
    {
        _invoice.TypeCode = "380";
        _invoice.CurrencyCode = "EUR";
        _invoice.BusinessProcess.ProfileIdentifier = FrEReportCodes.ProfileIdentifier;
    }

    /// <summary>The invoice number and when it was issued.</summary>
    /// <exception cref="ArgumentException"><paramref name="number"/> is empty.</exception>
    public FrReportedInvoiceBuilder Numbered(string number, DateOnly issuedOn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        _invoice.Identifier = number;
        _invoice.IssueDate = issuedOn;
        return this;
    }

    /// <summary>What kind of invoice this is. A commercial invoice, <c>380</c>, unless said otherwise.</summary>
    public FrReportedInvoiceBuilder OfType(string typeCode)
    {
        _invoice.TypeCode = typeCode;
        return this;
    }

    /// <summary>The invoice currency. Euro unless said otherwise.</summary>
    public FrReportedInvoiceBuilder InCurrency(string currencyCode)
    {
        _invoice.CurrencyCode = currencyCode;
        return this;
    }

    /// <summary>The invoicing framework this invoice belongs to — <c>B1</c>, <c>S1</c> and the rest.</summary>
    public FrReportedInvoiceBuilder InProcess(string businessProcessIdentifier)
    {
        _invoice.BusinessProcess.Identifier = businessProcessIdentifier;
        return this;
    }

    /// <summary>When payment is due.</summary>
    public FrReportedInvoiceBuilder DueOn(DateOnly dueDate)
    {
        _invoice.DueDate = dueDate;
        return this;
    }

    /// <summary>When VAT becomes chargeable.</summary>
    public FrReportedInvoiceBuilder TaxDueOn(string taxDueDateTypeCode)
    {
        _invoice.TaxDueDateTypeCode = taxDueDateTypeCode;
        return this;
    }

    /// <summary>The seller, identified by SIREN, with the VAT number the rules then require.</summary>
    /// <exception cref="ArgumentException">The SIREN or the VAT number is empty.</exception>
    public FrReportedInvoiceBuilder SoldBy(string siren, string vatNumber, string countryCode = "FR")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(siren);
        ArgumentException.ThrowIfNullOrWhiteSpace(vatNumber);

        _invoice.Seller = new FrReportedParty
        {
            CompanyIdentifier = new IdentifierField(siren, CompanyScheme),
            TaxRegistration = new FrReportedTaxRegistration
            {
                Identifier = new IdentifierField(vatNumber, VatQualifier),
            },
            CountryCode = countryCode,
        };

        return this;
    }

    /// <summary>The buyer, identified by SIREN.</summary>
    /// <exception cref="ArgumentException">The SIREN or the VAT number is empty.</exception>
    public FrReportedInvoiceBuilder BoughtBy(string siren, string vatNumber, string countryCode = "FR")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(siren);
        ArgumentException.ThrowIfNullOrWhiteSpace(vatNumber);

        _invoice.Buyer = new FrReportedParty
        {
            CompanyIdentifier = new IdentifierField(siren, CompanyScheme),
            TaxRegistration = new FrReportedTaxRegistration
            {
                Identifier = new IdentifierField(vatNumber, VatQualifier),
            },
            CountryCode = countryCode,
        };

        return this;
    }

    /// <summary>
    /// A buyer abroad, identified in a scheme other than the SIREN.
    /// </summary>
    /// <param name="schemeIdentifier">The scheme — <c>0223</c> a foreign registration, and the rest.</param>
    /// <param name="identifier">The identifier itself.</param>
    /// <param name="countryCode">Where the buyer is, ISO 3166-1 alpha-2.</param>
    /// <param name="vatNumber">
    /// The buyer's VAT number. Required by the rules for schemes <c>0002</c> and <c>0223</c>.
    /// </param>
    /// <exception cref="ArgumentException">The identifier is empty.</exception>
    public FrReportedInvoiceBuilder BoughtAbroadBy(
        string schemeIdentifier,
        string identifier,
        string countryCode,
        string? vatNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        _invoice.Buyer = new FrReportedParty
        {
            CompanyIdentifier = new IdentifierField(identifier, schemeIdentifier),
            TaxRegistration = vatNumber is null
                ? null
                : new FrReportedTaxRegistration { Identifier = new IdentifierField(vatNumber, VatQualifier) },
            CountryCode = countryCode,
        };

        return this;
    }

    /// <summary>An earlier invoice this one corrects or credits.</summary>
    /// <exception cref="ArgumentException"><paramref name="number"/> is empty.</exception>
    public FrReportedInvoiceBuilder Correcting(string number, DateOnly issuedOn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        _invoice.ReferencedDocuments.Add(new FrReportedDocumentReference
        {
            Identifier = number,
            IssueDate = issuedOn,
        });

        return this;
    }

    /// <summary>
    /// The VAT breakdown, from which the totals are added up.
    /// </summary>
    /// <param name="ratePercent">The rate, as a percentage.</param>
    /// <param name="taxableAmount">What the rate applies to.</param>
    /// <param name="taxAmount">The VAT it comes to.</param>
    /// <param name="categoryCode">The VAT category. Standard rate unless said otherwise.</param>
    public FrReportedInvoiceBuilder Taxed(
        decimal ratePercent,
        decimal taxableAmount,
        decimal taxAmount,
        string categoryCode = "S")
    {
        _invoice.TaxSubtotals.Add(new FrReportedTaxSubtotal
        {
            TaxableAmount = taxableAmount,
            TaxAmount = taxAmount,
            CategoryCode = categoryCode,
            Percent = ratePercent,
        });

        return this;
    }

    /// <summary>
    /// A share of the invoice that carries no VAT, with the reason the rules then require.
    /// </summary>
    /// <param name="taxableAmount">The amount exempt.</param>
    /// <param name="exemptionReasonCode">Why, as a VATEX code.</param>
    /// <param name="exemptionReason">Why, in words.</param>
    /// <exception cref="ArgumentException">The reason or its code is empty.</exception>
    public FrReportedInvoiceBuilder Exempt(
        decimal taxableAmount,
        string exemptionReasonCode,
        string exemptionReason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exemptionReasonCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(exemptionReason);

        _invoice.TaxSubtotals.Add(new FrReportedTaxSubtotal
        {
            TaxableAmount = taxableAmount,
            TaxAmount = 0m,
            CategoryCode = "E",
            Percent = 0m,
            ExemptionReasonCode = exemptionReasonCode,
            ExemptionReason = exemptionReason,
        });

        return this;
    }

    /// <summary>Everything the builder does not cover, on the invoice itself.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public FrReportedInvoiceBuilder Extend(Action<FrReportedInvoice> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        _extensions.Add(configure);
        return this;
    }

    /// <summary>Adds up the totals from the VAT breakdown, then applies whatever the caller extended.</summary>
    /// <exception cref="InvalidOperationException">The invoice carries no VAT breakdown.</exception>
    internal FrReportedInvoice Complete()
    {
        if (_invoice.TaxSubtotals.Count == 0)
        {
            throw new InvalidOperationException(
                "A reported invoice carries its VAT breakdown: Taxed(rate, taxable, tax) or Exempt(...).");
        }

        _invoice.Totals = new FrReportedTotals
        {
            TaxExclusiveAmount = _invoice.TaxSubtotals.Sum(subtotal => subtotal.TaxableAmount.Value ?? 0m),
            TaxAmount = new AmountField(
                _invoice.TaxSubtotals.Sum(subtotal => subtotal.TaxAmount.Value ?? 0m),
                _invoice.CurrencyCode.Value),
        };

        foreach (Action<FrReportedInvoice> extension in _extensions)
        {
            extension(_invoice);
        }

        return _invoice;
    }
}
