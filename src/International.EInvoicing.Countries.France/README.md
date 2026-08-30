# International.EInvoicing.Countries.France

What French electronic invoicing adds on top of the norms.

Today: the **lifecycle statuses** (*statuts de cycle de vie*) the 2026 reform makes mandatory, with a builder
that fills in what each status implies so you name the status and not the codes behind it.

```csharp
LifecycleStatusMessage refused = FrCdar
    .FromBuyer("200000008", "ACHETEUR")                        // who reports it
    .SentBy("0003", "PA-E Acheteur")                           // their approved platform
    .ToSeller("100000009", "VENDEUR", "100000009_STATUTS")     // who it is for
    .About("F202500003", new DateOnly(2025, 7, 1))
    .Refused(FrStatusReason.VatRateWrong, "Taux de TVA erroné");
```

Who may report which status is settled by where you start: `FromPlatform` files and receives, `FromBuyer`
approves and refuses, `FromSeller` collects. Getting it the wrong way round is refused with the entry point
to use instead. Sending to a partner and reporting to the public portal are **different profiles**, not
variants of one, and the destination you name settles it.

And the **e-reporting** transmission — *flux 10* — that the reform asks for alongside invoicing: sales to
consumers, transactions with parties abroad, and when the money arrived.

```csharp
FrEReport report = FrEReporting
    .Transactions(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30))
    .From("0003", "PA-E Vendeur")
    .For("100000009", "VENDEUR")
    .Day(new DateOnly(2026, 9, 1), FrEReportCodes.RetailTransactions, split => split
        .At(20m, 1000m)
        .At(5.5m, 200m))
    .Build();
```

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
