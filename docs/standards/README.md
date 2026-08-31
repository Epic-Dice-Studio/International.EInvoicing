# Standards reference

One page per standard. Each page answers the same questions, in the same order, so that implementing a new
format is a matter of following the page rather than rediscovering the norm.

| Page | What it covers |
|---|---|
| [en16931.md](en16931.md) | The European semantic model — business terms, rules, code lists. The anchor of everything else. |
| [ubl-2.1.md](ubl-2.1.md) | OASIS UBL 2.1 syntax binding. |
| [cii-d22b.md](cii-d22b.md) | UN/CEFACT Cross Industry Invoice syntax binding. |
| [cdar.md](cdar.md) | UN/CEFACT lifecycle acknowledgement and response messages. |
| [facturx.md](facturx.md) | Factur-X / ZUGFeRD hybrid PDF/A-3 invoices and their five profiles. |
| [xrechnung.md](xrechnung.md) | The German CIUS and its extension. |
| [peppol-bis-3.md](peppol-bis-3.md) | Peppol BIS Billing 3.0, the CIUS used across much of Europe. |
| [country-fr.md](country-fr.md) | France: reform, CIUS, CDAR profiling, identifiers. |
| [country-de.md](country-de.md) | Germany: mandate, accepted formats, Leitweg-ID. |
| [country-be.md](country-be.md) | Belgium: mandate, Peppol rules, national identifiers. |
| [country-no.md](country-no.md) | Norway: EHF 3.0, the organisation number, the rules inside Peppol. |
| [country-se.md](country-se.md) | Sweden: Peppol BIS, the organisation number, Bankgiro and Plusgiro. |
| [country-dk.md](country-dk.md) | Denmark: NemHandel, the CVR number, the payment means Denmark refuses. |
| [country-nl.md](country-nl.md) | Netherlands: Peppol BIS, the KvK/OIN scheme its rules demand, why NLCIUS is absent. |
| [country-is.md](country-is.md) | Iceland: Peppol BIS, the kennitala, the scheme its rules look in. |
| [peppol-pint.md](peppol-pint.md) | Peppol PINT: the half of Peppol outside Europe, its identifiers, and why its rules do not run yet. |
| [country-hr.md](country-hr.md) | Croatia: Fiskalizacija 2.0, the OIB, and the two thirds of the mandate a document library cannot do. |
| [country-sk.md](country-sk.md) | Slovakia: Peppol BIS from 2027, and the tax data document reported beside every invoice. |
| [peppol-taxdata.md](peppol-taxdata.md) | The Peppol tax data document: Slovakia and ViDA carried, the Gulf dialect explained. |

Before a standard is declared done, its page should also record what [prior art](../prior-art.md) revealed:
mature implementations have already met the documents that break new ones.

## Page template

When adding a standard, copy this structure:

1. **Scope and version** — what the standard covers, which version we target, and why that one.
2. **Official sources** — every authoritative link, with what each one is good for.
3. **Artefacts** — what lives in `specs/`, what must be downloaded, and the licence.
4. **Model mapping** — how the standard maps onto the canonical model; the business terms that need care.
5. **Validation** — which rule sets apply, in which order, and which are advisory.
6. **Pitfalls** — the mistakes implementations actually make. This section is the reason the page exists.
7. **Reference implementations** — where to check an interpretation against a mature library.
