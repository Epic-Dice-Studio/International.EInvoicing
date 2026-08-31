using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Building;

/// <summary>Builds one invoice line (BG-25).</summary>
public sealed class InvoiceLineBuilder
{
    private readonly InvoiceLine _line = new();
    private readonly string? _currencyCode;

    internal InvoiceLineBuilder(string? currencyCode) => _currencyCode = currencyCode;

    /// <summary>BT-126 — the line's identifier.</summary>
    public InvoiceLineBuilder WithIdentifier(string identifier)
    {
        _line.Identifier = identifier;
        return this;
    }

    /// <summary>BT-127 — a free-text note about the line.</summary>
    public InvoiceLineBuilder WithNote(string note)
    {
        _line.Note = note;
        return this;
    }

    /// <summary>BT-129 and BT-130 — the quantity invoiced and its unit (UN/ECE Recommendation 20).</summary>
    public InvoiceLineBuilder WithQuantity(decimal quantity, string unitCode)
    {
        _line.Quantity = new QuantityField(quantity, unitCode);
        return this;
    }

    /// <summary>BT-131 — the line's net amount, excluding VAT.</summary>
    public InvoiceLineBuilder WithNetAmount(decimal amount)
    {
        _line.NetAmount = new AmountField(amount, _currencyCode);
        return this;
    }

    /// <summary>BT-146 — the net price of one base quantity.</summary>
    public InvoiceLineBuilder WithNetPrice(decimal price, decimal? baseQuantity = null, string? unitCode = null)
    {
        _line.Price ??= new LinePrice();
        _line.Price.NetPrice = new AmountField(price, _currencyCode);

        if (baseQuantity is { } quantity)
        {
            _line.Price.BaseQuantity = new QuantityField(quantity, unitCode);
        }

        return this;
    }

    /// <summary>BT-151 and BT-152 — the line's VAT category and rate.</summary>
    public InvoiceLineBuilder WithVat(string categoryCode, decimal rate)
    {
        _line.VatCategoryCode = categoryCode;
        _line.VatRate = rate;
        return this;
    }

    /// <summary>
    /// BT-151 without BT-152 — for a category that forbids a rate rather than requiring a zero.
    /// </summary>
    /// <remarks>
    /// <em>Not subject to VAT</em> is the one: <c>BR-O-05</c> rejects a line that carries a rate at all, so a
    /// zero will not do. See <see cref="VatCategoryCodes.ForbidsRate"/>.
    /// </remarks>
    public InvoiceLineBuilder WithVat(string categoryCode)
    {
        _line.VatCategoryCode = categoryCode;
        _line.VatRate = Field<decimal>.Unset;
        return this;
    }

    /// <summary>BG-31 — what is being invoiced.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public InvoiceLineBuilder WithItem(Action<Item> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        _line.Item ??= new Item();
        configure(_line.Item);
        return this;
    }

    /// <summary>BG-31 — the common case: an item that only has a name.</summary>
    public InvoiceLineBuilder WithItem(string name) => WithItem(item => item.Name = name);

    /// <summary>Reaches the line directly, for anything this builder does not cover.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public InvoiceLineBuilder Extend(Action<InvoiceLine> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(_line);
        return this;
    }

    internal InvoiceLine Build() => _line;
}
