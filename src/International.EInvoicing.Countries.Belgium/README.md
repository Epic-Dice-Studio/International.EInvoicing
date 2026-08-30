# International.EInvoicing.Countries.Belgium

What Belgian electronic invoicing adds on top of the norms.

Belgium is the clearest case of the layering rule: almost everything Belgian *is* Peppol, so this package
uses `International.EInvoicing.Peppol` rather than restating it. What is genuinely national lives here — the
**KBO/BCE enterprise number** and the **structured communication**, the payment reference Belgian receivers
reconcile on.

```csharp
BeStructuredCommunication reference = BeStructuredCommunication.ForInvoice(2026_000_123);
invoice.Payment!.RemittanceInformation = reference.ToField();   // +++202/6000/12324+++
```

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
