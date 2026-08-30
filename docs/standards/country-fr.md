# France

> Regulatory dates move. Treat this page as a map, and the DGFiP specification package as the territory.
> Recorded state: August 2026.

## The reform in one paragraph

France mandates structured electronic invoicing for domestic B2B, exchanged through **approved platforms**
(*plateformes agréées*, formerly PDP). Every company must be able to **receive** electronic invoices from
1 September 2026. **Issuing** starts on 1 September 2026 for large companies and mid-caps, and on
1 September 2027 for SMEs and micro-enterprises. The public portal (PPF) was scaled back in October 2024 to a
central directory and a collection point for e-reporting; it no longer exchanges invoices itself.

Alongside invoicing, two obligations accompany it: **lifecycle statuses** (CDAR messages) and **e-reporting**
of transactions and payment data.

## Scope for this library

| Capability | Package | Status |
|---|---|---|
| Invoice syntaxes (UBL, CII, Factur-X) | `.Ubl`, `.Cii`, `.FacturX` | implemented |
| French invoice profile (EXTENDED CTC FR) | `.Countries.France` | implemented, measured against the DGFiP rules |
| CDAR lifecycle statuses, French profiling | `.Countries.France` | implemented, measured against the DGFiP rules |
| SIREN / SIRET / VAT identifiers | `.Countries.France` | implemented |
| E-reporting (flux 10) | `.Countries.France` | implemented, measured against the DGFiP rules |
| Transmission to an approved platform | — | permanently out of scope |

## Official sources

| Source | Use it for |
|---|---|
| <https://www.impots.gouv.fr/specifications-externes-b2b> | The authoritative package: specification, annexes, XSD, API definitions. Free. |
| <https://aife.economie.gouv.fr> | Programme status, calendar, platform registry. |
| AFNOR XP Z12-012 / -013 / -014 | Semantic model, CDAR profiling, directory. Sold by AFNOR. |
| <https://fnfe-mpe.org> | Factur-X, and practical French guidance. |

The DGFiP package is **not redistributable** — download it yourself, see `specs/fr-dse/PROVENANCE.md`.

## Running the French rules

The Schematron artefacts and the DGFiP sample messages are carried by a repository that declares no licence,
so this library fetches them rather than shipping them:

```bash
build/fetch-specs.sh france
```

That fills two folders, both ignored by git:

| Folder | What it holds |
|---|---|
| `specs/fr-dse/rules/ctc/1.4.0.03/` | `BR-FR-CDV` (CDAR), `BR-FR-Flux2` and `EXTENDED-CTC-FR`, for UBL and CII |
| `specs/fr-dse/rules/flux10/1.0/` | `PPF_Flux10`, the e-reporting rules |
| `specs/fr-dse/schemas/flux10/1.0/` | The e-reporting XSDs |
| `specs/fr-dse/samples/1.4.0.03/` | The eleven DGFiP lifecycle sample messages |

They are then ordinary rule sets:

```csharp
SchematronRuleSet rules = SchematronRuleSet.Load(
    File.ReadAllText(path), "BR-FR-CDV (CDAR)", "1.4.0.03");

ValidationReport report = new SchematronValidator().Validate(xml, rules);
```

The French artefacts define twenty of their own functions in XSLT — SIRET and SIREN coherence, decimal
precision, code-list membership. The engine **runs those definitions** rather than reimplementing them, so a
revision by the DGFiP takes effect by replacing the file.

When the artefacts are absent, the tests that need them skip and say so; nothing silently passes.

## Building a French invoice

Two things France requires that EN 16931 does not, and one call adds both:

```csharp
EInvoice invoice = EInvoiceBuilder
    .Create(FrProfiles.ExtendedCtcFrUbl)
    .WithNumber("FA-2026-001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .InCurrency("EUR")
    .ForFrance()                                              // the invoicing case, and the three mentions
    .FromFrenchSeller("Fournisseur SARL", "732829320", "FR32732829320")
    .ToFrenchBuyer("Client SA", "552081317", "FR89552081317")
    .AddLine(line => line.WithItem("Conseil").WithQuantity(3m, "HUR").WithNetPrice(150m)
        .WithNetAmount(450m).WithVat("S", 20m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();
```

**The invoicing case** (BT-23, *cadre de facturation*) comes from a closed list — `B1` an ordinary invoice,
`B2` a deposit, `B4` a self-billed one, and so on. `ForFrance("B2")` names another;
`FrBusinessProcess` carries them, and anything outside the list is refused with the list.

**The three mentions** (BT-21/BT-22) are required on every invoice, whatever else it gets right: the
recovery indemnity (`PMT`), the late-payment penalties (`PMD`), and the early-payment discount or its
absence (`AAB`). `ForFrance()` writes the customary wording; where your terms differ, say so:

```csharp
.WithFrenchMention(FrInvoiceMention.EarlyPaymentDiscountCode, "Escompte de 2 % sous 10 jours.")
```

Calling it again with the same code replaces the mention rather than adding a second one. The suggested
wordings are a starting point, not legal advice.

**Both parties are identified by SIREN**, and `FromFrenchSeller` / `ToFrenchBuyer` check the digit before
writing it — a typo caught here is cheaper than an invoice delivered to the wrong company.

An invoice built this way satisfies EN 16931, `BR-FR-Flux2` and `EXTENDED-CTC-FR`, in **both** syntaxes.
That is measured on every build rather than claimed.

## The French invoice profile

The identifier is a **conformant extension**, not a CIUS:

```
urn:cen.eu:en16931:2017#conformant#urn.cpro.gouv.fr:1p0:extended-ctc-fr
```

`#conformant#` matters: a French invoice may carry what the base EN 16931 rules reject, so it is not measured
against them alone. Both syntaxes share the identifier — `FrProfiles.ExtendedCtcFrUbl` and
`FrProfiles.ExtendedCtcFrCii` differ only in syntax.

## What is specifically French

- **The minimum accepted formats** are UBL, CII and Factur-X. A receiver must accept all three, which makes
  cross-syntax conversion a real requirement rather than a convenience.
- **Mandatory mentions beyond EN 16931**: SIREN of both parties, VAT payment option (*TVA sur les débits* or
  *sur les encaissements*), delivery address when it differs, and public-procurement references where relevant.
- **Identifiers**: SIREN (9 digits, Luhn), SIRET (14 digits, Luhn), and the French intra-community VAT number
  whose two check digits derive from the SIREN. Validate them, do not merely pattern-match them.
- **Lifecycle statuses**: a set of mandatory statuses and a set of optional ones, with a defined sequence.
  See [cdar.md](cdar.md) and the [lifecycle guide](../guides/lifecycle.md).
- **Who reports a status** is part of the status. Platform events (200, 201, 202, 203, 213) are issued by the
  sending platform; business events (204, 205, 207, 210, 211) by the buyer, and a collection (212) by the
  seller. A message naming the wrong one is rejected even though every code in it is right.

## Pitfalls

- **"Factur-X" is not one thing.** MINIMUM and BASIC WL are not complete EN 16931 invoices; their legal use is
  narrow. Selecting a profile is a business decision, so the library must never pick one silently.
- **The specification is versioned and moving.** Record the version you implemented against in the tests.
- **E-reporting is not invoicing.** It has its own document — no XML namespace, its own codes, its own rules
  — and its own periodicity. See the [e-reporting guide](../guides/e-reporting.md).
- **The DGFiP publishes no sample transmissions for flux 10.** What can be measured is that the documents
  this library builds satisfy the published rules, which is what the tests do.
- **A collection status needs amounts.** Status 212 must carry at least one collected amount with its VAT
  rate; a message that only names the status is rejected.
- **Refusal reasons are not one list.** Each status has its own, and the public-sector platform (`9999`)
  accepts seven the others do not.
