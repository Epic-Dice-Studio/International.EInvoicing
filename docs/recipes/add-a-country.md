# Recipe — add a country

## Decide what is actually national

Before creating anything, sort each requirement into one of three buckets:

| The requirement is… | It belongs in |
|---|---|
| the same everywhere | `Core`, or the syntax package |
| shared by several countries (Peppol BIS, EN 16931 CIUS) | the format package — used by the country, never copied into it |
| genuinely national (identifiers, national codes, national profiling) | `Countries.<Country>` |

Getting this wrong is the main way this repository could rot: five countries each carrying their own copy of
Peppol rules would be unmaintainable. Belgium is the worked example — see `docs/standards/country-be.md`.

## Create the package

`src/International.EInvoicing.Countries.<Country>/`:

```
<Country>.cs                     the ICountry entry point registered by AddCountry<T>()
Profiles/                        national CIUS registrations
Rules/                           national validation rules
Identifiers/                     national identifier value types, with their check digits
<Country>ServiceCollectionExtensions.cs
```

Public types are prefixed with the ISO 3166 alpha-2 code: `FrCdarProfile`, `DeLeitwegId`,
`BeStructuredCommunication`.

For a capability that exists everywhere but behaves differently per country, implement the Core abstraction
(for example `ITaxIdentifierValidator`) rather than inventing a national API.

## Document and prove

- `docs/standards/country-<xx>.md` from the template — mandate, dates, formats, what is specifically national,
  pitfalls.
- Golden files from the country's official test suite. If the country publishes none, say so on the page and
  build a corpus from the specification's examples, citing the section.
- All five test families, plus a check-digit test per identifier with a real, published example.
- Update `docs/coverage.json`.
