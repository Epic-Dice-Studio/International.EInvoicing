# International.EInvoicing.Peppol

Peppol BIS Billing 3.0: the profiles, the electronic address schemes, participant identifiers, and the
registration that puts the published rules to work.

```csharp
EInvoicing einvoicing = EInvoicing.Create(library => library
    .AddDefaults()
    .AddPeppol()
    .AddPeppolRulesFrom("specs/peppol/rules"));   // fetched, not shipped — see below
```

An electronic address (BT-34, BT-49) is an identifier plus the scheme it belongs to, and guessing the scheme
from the country is the shortcut that gets invoices rejected:

```csharp
PeppolParticipant buyer = PeppolParticipant.Parse("0208:0203201340");

buyer.Scheme;                  // 0208 — a Belgian enterprise number
buyer.HasKnownScheme;          // true
buyer.ToElectronicAddress();   // ready for BT-49
buyer.ToQualifiedString();     // iso6523-actorid-upis::0208:0203201340
```

## The rules are fetched, not shipped

The Peppol artefacts declare no licence, so this package carries none of them. `build/fetch-specs.sh peppol`
brings them, and `AddPeppolRulesFrom(directory)` loads all four — Peppol's own rules *and* its copy of the
EN 16931 ones, because both apply and running only the first gives a false pass.

The engine is measured against Peppol's own unit corpus: 227 of 227 UBL cases and 127 of 127 CII cases agree
with the published expected results.

## Out of scope

SMP lookup and AS4 transmission. This library performs no network I/O at all.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
