# International.EInvoicing.Countries.Portugal

What Portuguese electronic invoicing adds on top of the norms.

Portugal exchanges **CIUS-PT**, the national CIUS published by the eSPap. Its artefact is the largest this
library has met — over two thousand assertions, because CIUS-PT bundles the EN 16931 UBL rules alongside its
own.

```csharp
EInvoicing library = EInvoicing.Create(portugal => portugal
    .AddDefaults()
    .AddPortugal()
    .AddPortugueseRulesFrom("specs/national/cius-pt/schematron"));   // build/fetch-specs.sh national
```

**Two traps.**

- **A delivery address is mandatory** (`BR-CIUS-PT-66`), which EN 16931 leaves optional.
- **Numbers must be written to two decimals.** `DT-CIUS-PT-094` and a dozen neighbours reject `1000` where
  they want `1000.00`, and the same for VAT percentages and invoiced quantities. This library now writes
  amounts, percentages and quantities with at least two decimals everywhere, which is what most
  implementations expect anyway — Portugal is simply the first to say so out loud.

`PtProfiles.Prefix` is the identifier without its version, for pinning a different one: the published rule
set accepts any version after the prefix, and `CiusPtUbl` names the one the current artefact validates.

The Portuguese rules are fetched, not shipped: `build/fetch-specs.sh national`.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
