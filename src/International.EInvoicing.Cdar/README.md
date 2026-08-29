# International.EInvoicing.Cdar

Lifecycle statuses: what happened to an invoice after it was sent — filed, received, approved, disputed,
paid — as UN/CEFACT Cross Domain Acknowledgement and Response messages. In France these are the *statuts de
cycle de vie* the 2026 reform makes mandatory between platforms.

This package is deliberately generic. A message whose national profiling it does not recognise is still
parsed, and the downgrade is reported, so an unknown profile costs you the meaning of some codes rather than
the whole message.

```csharp
services.AddEInvoicing(o => o.AddCdar());
```

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
