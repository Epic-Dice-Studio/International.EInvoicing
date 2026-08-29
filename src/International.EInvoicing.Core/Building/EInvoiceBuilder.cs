using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;

namespace International.EInvoicing.Building;

/// <summary>
/// Builds an invoice from ordinary .NET values. The model keeps the raw text of everything it reads, which
/// makes it verbose; that verbosity stops here.
/// </summary>
/// <remarks>
/// Amounts are stamped with the document currency as they are added, so an invoice cannot end up with lines
/// in a currency the document never declared.
/// </remarks>
public sealed class EInvoiceBuilder
{
    private readonly EInvoice _invoice = new();

    private EInvoiceBuilder()
    {
    }

    /// <summary>Starts an invoice that claims to conform to <paramref name="specification"/> (BT-24).</summary>
    public static EInvoiceBuilder Create(ProfileIdentifier specification)
    {
        var builder = new EInvoiceBuilder();
        builder._invoice.SpecificationIdentifier = specification;
        return builder;
    }

    /// <summary>Starts an invoice conforming to the given profile.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="profile"/> is <c>null</c>.</exception>
    public static EInvoiceBuilder Create(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return Create(profile.Id);
    }

    /// <summary>BT-1 — the invoice number.</summary>
    public EInvoiceBuilder WithNumber(string number)
    {
        _invoice.Number = number;
        return this;
    }

    /// <summary>BT-2 — the date the invoice was issued.</summary>
    public EInvoiceBuilder IssuedOn(DateOnly issueDate)
    {
        _invoice.IssueDate = issueDate;
        return this;
    }

    /// <summary>BT-3 — invoice type code (UNTDID 1001), such as <c>380</c> for a commercial invoice.</summary>
    public EInvoiceBuilder OfType(string typeCode)
    {
        _invoice.TypeCode = typeCode;
        return this;
    }

    /// <summary>BT-5 — the currency the invoice is expressed in. Amounts added afterwards inherit it.</summary>
    public EInvoiceBuilder InCurrency(string currencyCode)
    {
        _invoice.CurrencyCode = currencyCode;
        return this;
    }

    /// <summary>BT-9 — the date payment is due.</summary>
    public EInvoiceBuilder DueOn(DateOnly dueDate)
    {
        _invoice.DueDate = dueDate;
        return this;
    }

    /// <summary>BT-10 — the reference the buyer asked to see on the invoice.</summary>
    public EInvoiceBuilder WithBuyerReference(string reference)
    {
        _invoice.BuyerReference = reference;
        return this;
    }

    /// <summary>BG-4 — the seller.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public EInvoiceBuilder WithSeller(Action<PartyBuilder> configure)
    {
        _invoice.Seller = BuildParty(configure);
        return this;
    }

    /// <summary>BG-7 — the buyer.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public EInvoiceBuilder WithBuyer(Action<PartyBuilder> configure)
    {
        _invoice.Buyer = BuildParty(configure);
        return this;
    }

    /// <summary>BG-10 — the party to be paid, when it is not the seller.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public EInvoiceBuilder WithPayee(Action<PartyBuilder> configure)
    {
        _invoice.Payee = BuildParty(configure);
        return this;
    }

    /// <summary>BG-1 — adds a free-text note.</summary>
    public EInvoiceBuilder WithNote(string text, string? subjectCode = null)
    {
        _invoice.Notes.Add(new InvoiceNote { Text = text, SubjectCode = subjectCode });
        return this;
    }

    /// <summary>BG-25 — adds an invoice line.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public EInvoiceBuilder AddLine(Action<InvoiceLineBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var line = new InvoiceLineBuilder(_invoice.CurrencyCode.Value);
        configure(line);
        _invoice.Lines.Add(line.Build());
        return this;
    }

    /// <summary>BG-23 — adds a VAT breakdown entry.</summary>
    public EInvoiceBuilder AddVatBreakdown(
        string categoryCode,
        decimal rate,
        decimal taxableAmount,
        decimal taxAmount)
    {
        _invoice.VatBreakdown.Add(new VatBreakdownEntry
        {
            CategoryCode = categoryCode,
            Rate = rate,
            TaxableAmount = Amount(taxableAmount),
            TaxAmount = Amount(taxAmount),
        });

        return this;
    }

    /// <summary>BG-22 — sets the document totals.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public EInvoiceBuilder WithTotals(Action<DocumentTotals> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(_invoice.Totals);
        return this;
    }

    /// <summary>Reaches the invoice directly, for anything this builder does not cover.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public EInvoiceBuilder Extend(Action<EInvoice> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(_invoice);
        return this;
    }

    /// <summary>
    /// Returns the invoice. It is not validated here: whether it satisfies EN 16931 or a national rule set is
    /// a separate, explicit step.
    /// </summary>
    public EInvoice Build() => _invoice;

    /// <summary>An amount in the document currency (BT-5).</summary>
    public AmountField Amount(decimal value) => new(value, _invoice.CurrencyCode.Value);

    private static Party BuildParty(Action<PartyBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var party = new PartyBuilder();
        configure(party);
        return party.Build();
    }
}
