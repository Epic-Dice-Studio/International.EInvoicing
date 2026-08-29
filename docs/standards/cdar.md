# CDAR — Cross Domain Acknowledgement and Response

## Scope and version

CDAR is the UN/CEFACT message used to report what happened to a document after it was sent: received,
accepted, rejected, disputed, paid. In France it is the carrier of the invoice **lifecycle statuses** that the
2026 reform makes mandatory between platforms, senders and receivers.

We implement the **generic UN/CEFACT CDAR** in `International.EInvoicing.Cdar`, and national profiles on top
of it — the French one in `International.EInvoicing.Countries.France`.

## Official sources

| Source | Use it for |
|---|---|
| <https://unece.org/trade/uncefact/xml-schemas> | The generic schema. |
| <https://www.impots.gouv.fr/specifications-externes-b2b> | The French profiling: status codes, sequencing, mandatory fields. |
| AFNOR XP Z12-013 | The French normative reference behind it. Sold by AFNOR. |

## Artefacts

`specs/cdar/` — generic schema, redistributable. The French profiling lives in the DGFiP specification
package, which is **not** committed (see `specs/fr-dse/PROVENANCE.md`).

## Structure

Three sections under `rsm:CrossDomainAcknowledgementAndResponse`:

| Section | Carries |
|---|---|
| `rsm:ExchangedDocumentContext` | The business process and the profile identifier. The French profiling declares `urn.cpro.gouv.fr:1p0:CDV:invoice`. |
| `rsm:ExchangedDocument` | The message itself: its id, its name, when it was written, and the parties — sender, issuer, and one or more recipients. |
| `rsm:AcknowledgementDocument` | What is being reported: whether it covers several documents, when the status occurred, and one `ram:ReferenceReferencedDocument` per document. |

Each `ram:ReferenceReferencedDocument` carries the invoice's identifier (BT-1) and issue date (BT-2), the
document type (BT-3), when it was received, and the status itself in `ram:ProcessConditionCode` with its
label in `ram:ProcessCondition`.

Timestamps use format `204` (`CCYYMMDDHHMMSS`), not the `102` of invoice dates: *when* a status occurred is
the point of the message, so `DateTimeField` carries them rather than `DateField`.

The French status codes are 200 filed, 201 issued by the platform, 202 received by the platform, 203 made
available, 204 taken in charge, 205 approved, 207 disputed, 210 refused, 211 payment sent, 212 collected,
213 rejected.

## Model mapping

The canonical model is `LifecycleStatusMessage`: which document is being reported on, by whom, what status,
when, why, and the chain of identifiers linking it back to the invoice.

The generic layer is what makes the fallback promise work. When a message declares a profile we do not know,
it is still parsed as generic CDAR, and the downgrade is reported:

```
EIV1042  Warning  UnknownProfile
    expected  a registered CDAR profile
    found     urn:acme:cdar:custom:2p0
    fallback  parsed as generic UN/CEFACT CDAR — national status codes not interpreted
```

The caller gets a usable message and knows precisely what was not interpreted.

## Validation

Generic schema, then the national profile's rules when one is registered. When none is, the validation report
says so and `IsComplete` is false — a lifecycle message must never appear to have passed rules that never ran.

## Pitfalls

- **Status codes are national.** The same code means different things under different profilings. Never
  resolve a code without knowing the profile.
- **Sequencing is a rule, not a suggestion.** Some statuses may only follow certain others; the French
  specification defines the automaton. Model it explicitly, do not scatter it across parsers.
- **Rejection reasons are structured**, not free text, and receivers depend on them.
- **A status message references an invoice that may not exist locally.** Identifier chaining must round-trip
  untouched even when nothing can be resolved.

## Reference implementations

Little prior art exists. [phax/phive-rules](https://github.com/phax/phive-rules) carries the French
`BR-FR-CDV` Schematron and the DGFiP lifecycle test files, which is where the structure documented above was
verified — that repository declares no licence, so nothing from it is redistributed here. The test documents
in this repository were written by hand to mirror it.

Be correspondingly careful, and cite the specification section in the tests.
