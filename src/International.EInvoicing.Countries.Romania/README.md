# International.EInvoicing.Countries.Romania

What Romanian electronic invoicing adds on top of the norms.

Romania's **e-Factura** mandate exchanges **CIUS-RO**, the national CIUS of EN 16931, in UBL. This package
carries the profile and runs the 244 assertions Romania publishes on top of the European rules.

```csharp
EInvoicing library = EInvoicing.Create(romania => romania
    .AddDefaults()
    .AddRomania()
    .AddRomanianRulesFrom("specs/national/cius-ro/schematron"));   // build/fetch-specs.sh national

EInvoice invoice = EInvoiceBuilder.Create(RoProfiles.CiusRoUbl)
    .WithNumber("2026-0001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .InCurrency("RON")
    .From(seller => seller
        .Named("Furnizor SRL")
        .WithVatIdentifier("RO12345678")
        .WithAddress(address =>
        {
            address.City = RoBucharestSector.Of(1);              // not "Bucureşti" — see below
            address.CountrySubdivision = RoBucharestSector.Subdivision;
            address.CountryCode = "RO";
        }))
    // …
    .Build();
```

**The rule nobody expects.** `BR-RO-100` is fatal: when a Romanian party's country subdivision is `RO-B` —
Bucharest — the **city name** must be the *sector*, spelled `SECTOR1` to `SECTOR6`. Writing "Bucureşti"
there, which is what every other country in the world would want, is exactly what fails. `RoBucharestSector`
exists for that one rule.

**A version that is not the version.** The CIUS-RO identifier carries the *CIUS* version, which is not the
version of the rule set that checks it: the artefacts are published at 1.0.9 and state that they are
"CIUS-RO version 1.0.1 compatible". The identifier here is read from the artefact rather than assumed.

The Romanian rules are fetched, not shipped: `build/fetch-specs.sh national`.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
