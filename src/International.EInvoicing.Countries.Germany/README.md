# International.EInvoicing.Countries.Germany

What German electronic invoicing adds on top of the norms.

The **XRechnung** profiles, registered for both syntaxes because Germany uses one identifier for UBL and CII,
and the **Leitweg-ID** with its check digit — the routing identifier public-sector recipients require in
BT-10.

```csharp
if (DeLeitwegId.TryParse(invoice.BuyerReference.Value, out DeLeitwegId route))
{
    Console.WriteLine(route.CoarseAddress);
}
```

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
