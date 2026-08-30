# International.EInvoicing.Countries.France

What French electronic invoicing adds on top of the norms.

Today: the **lifecycle statuses** (*statuts de cycle de vie*) the 2026 reform makes mandatory, with a builder
that fills in what each status implies so you name the status and not the codes behind it.

```csharp
LifecycleStatusMessage refused = FrCdar
    .ToPartner(to => to.Company("100000009").Named("VENDEUR").AsSeller().ReachableAt("100000009_STATUTS"))
    .From(from => from.Platform("0003", "PA-E Vendeur"))
    .IssuedByBuyer("200000008", "ACHETEUR")
    .About("F202500003", new DateOnly(2025, 7, 1))
    .Refused("TX_TVA_ERR", "Taux de TVA erroné");
```

Sending to a partner and sending to the public portal are **different profiles**, not variants of one, so
they are different entry points: `FrCdar.ToPartner(...)` and `FrCdar.ToPublicPortal(...)`.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
