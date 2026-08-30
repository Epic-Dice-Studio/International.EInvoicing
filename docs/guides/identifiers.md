# National identifiers

The identifiers a country requires on an invoice carry check digits. Validating them is not pedantry: a
mistyped SIRET makes an invoice unroutable, and a Leitweg-ID with a wrong check digit is not rejected — it is
delivered to a different authority, and nobody notices until payment does not arrive.

Every type here follows the same shape: `TryParse`, `IsValid`, `Parse`, and `ToField()` to put it on an
invoice with the scheme that gives it meaning.

## France

```csharp
using International.EInvoicing.Countries.France.Identifiers;

FrSiren siren = FrSiren.Parse("732 829 320");
siren.ToFormattedString();          // 732 829 320

FrSiret siret = FrSiret.Parse("73282932000074");
siret.Siren.Value;                  // 732829320
siret.EstablishmentNumber;          // 00074

FrVatNumber vat = FrVatNumber.ForSiren(siren);
vat.Value;                          // FR44732829320
```

Both SIREN and SIRET carry a Luhn check. **Except La Poste**: establishments under SIREN `356000000` predate
the rule and satisfy a digit sum divisible by five instead. A validator that does not know that rejects
genuine invoices from one of the largest issuers in the country, so it is handled.

A French VAT number is `FR`, a two-character key, then the SIREN — and the key is derived from the SIREN, so
the two are checked against each other. Older numbers use letters in the key, which cannot be recomputed;
those parse with `IsKeyVerified` false rather than being silently accepted or wrongly refused.

```csharp
invoice.Seller!.Identifiers.Add(siret.ToField());        // scheme 0009
invoice.Seller.VatIdentifier = vat.ToField();            // scheme 9957
```

## Germany

```csharp
using International.EInvoicing.Countries.Germany.Identifiers;

DeLeitwegId route = DeLeitwegId.Parse("04011000-1234512345-06");
route.CoarseAddress;                // 04011000 — which authority
route.FineAddress;                  // 1234512345 — which part of it

DeLeitwegId built = DeLeitwegId.Create("04011000", "12345");
invoice.BuyerReference = built.ToBuyerReference();       // BT-10
```

The check follows ISO/IEC 7064 MOD 97-10, the scheme behind IBANs: the two addresses are joined without
hyphens, letters count as their position in the alphabet plus nine, and the check is 98 minus the remainder.
Public-sector recipients require it in BT-10; ordinary B2B usually does not.

## Belgium

```csharp
using International.EInvoicing.Countries.Belgium.Identifiers;

BeEnterpriseNumber company = BeEnterpriseNumber.Parse("BE 0417.497.106");
company.ToFormattedString();        // 0417.497.106
company.VatNumber;                  // BE0417497106 — the same digits

BeStructuredCommunication reference = BeStructuredCommunication.ForInvoice(2026000123);
reference.ToString();               // +++202/6000/12397+++
invoice.Payment!.RemittanceInformation = reference.ToField();   // BT-83
```

The enterprise number and the VAT number are the same ten digits, which is why one type reads both. The
structured communication is what Belgian receivers reconcile payments on, so building it correctly matters
more than it looks: its last two digits are the remainder modulo 97, with zero written as `97` so the check
is never `00`.

## Where the examples come from

Nothing here was invented. The Leitweg-ID examples are the one from the format specification and the one used
throughout the official XRechnung test suite; the check-digit algorithms are tested against those rather than
against numbers this library made up. When a rule could not be verified, the code says so — see
`IsKeyVerified` on a French VAT number.

## Next

- [Writing a document](writing.md) — where identifiers go on an invoice
- [Lifecycle statuses](lifecycle.md) — the French statuses
- The country pages: [France](../standards/country-fr.md), [Germany](../standards/country-de.md),
  [Belgium](../standards/country-be.md)

## Run it

[`samples/International.EInvoicing.Samples/Chapters/NationalIdentifiers.cs`](../../samples/International.EInvoicing.Samples/Chapters/NationalIdentifiers.cs) is this page as code — the check digits, and what a typo does.

```bash
dotnet run --project samples/International.EInvoicing.Samples
```
