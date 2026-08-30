namespace International.EInvoicing.Countries.France.Lifecycle;

/// <summary>
/// What was collected at one VAT rate.
/// </summary>
/// <remarks>
/// A collection status must say how much was collected and at which rate, once per rate — an invoice with
/// two rates reports two of these. A status that only said "collected" is rejected.
/// </remarks>
/// <param name="Amount">The amount collected.</param>
/// <param name="VatRate">The VAT rate it was collected at, as a percentage.</param>
/// <param name="CurrencyCode">The currency, ISO 4217.</param>
public readonly record struct FrCollectedAmount(decimal Amount, decimal VatRate, string CurrencyCode = "EUR");
