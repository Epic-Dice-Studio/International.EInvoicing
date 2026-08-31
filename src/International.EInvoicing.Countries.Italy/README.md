# International.EInvoicing.Countries.Italy

What Italian electronic invoicing adds on top of the norms **over Peppol**.

```csharp
ItalianEInvoicing italia = ItalianEInvoicing.Create();

EInvoice invoice = italia.Invoice()
    .WithNumber("2026-0001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .From(seller => italia.Describe(seller, "12345670009", "Fornitore Srl")
        .WithAddress(address =>
        {
            address.Line1 = "Via Roma 1";        // IT-R-002 to IT-R-004 want all three
            address.City = "Milano";
            address.PostCode = "20121";
            address.CountryCode = "IT";
        }))
    // …
    .Build();
```

`Describe` checks the **partita IVA** before writing it — eleven digits where odd positions count as
themselves and even positions map through `0246813579`, the total divisible by ten — and puts it in scheme
`0211` and in BT-31 with its `IT` prefix. `IT-R-002` to `IT-R-004` require a street, a city and a postcode on
an Italian party, where EN 16931 asks only for a country.

**Worth knowing:** Peppol's own check, `u:checkPIVAseIT`, reads the first two characters of the value and
returns *true* for anything that does not begin `IT`. A partita IVA written bare — which is how scheme `0211`
is normally used — is therefore never verified by the network, and a wrong one goes through unnoticed. This
library checks it either way.

**FatturaPA is not here.** The format the SDI exchanges domestically is its own XML tree rather than a
profile of EN 16931, and every invoice must carry a qualified electronic signature, which this library does
not produce by design. It is a project rather than a profile — see the
[roadmap](https://github.com/Epic-Dice-Studio/International.EInvoicing/blob/main/docs/roadmap.md).

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
