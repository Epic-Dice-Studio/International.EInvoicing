# Diagnostic catalogue

Every diagnostic this library can emit has a page here, named after its code. CI fails when a code exists in
the source without a page — an unexplained code is worse than no code at all.

## Code ranges

| Range | Meaning |
|---|---|
| `EIV1xxx` | Profile resolution — unknown, unsupported, or downgraded profile |
| `EIV2xxx` | Content — unparsable value, unknown code list entry, unmapped element |
| `EIV3xxx` | Structure — cardinality, ordering, missing mandatory group |
| `EIV4xxx` | Container — PDF, embedded attachments, XMP metadata |
| `EIV5xxx` | Limits and safety — document too large, nesting too deep, malformed XML |
| `EIV9xxx` | Configuration — a policy or registration problem in the caller's own setup |

## Severities

| Severity | Meaning |
|---|---|
| `Info` | Something was noted and handled. Nothing is lost. |
| `Warning` | A fallback was applied. The document is usable; verify the affected data. |
| `Error` | The result is incomplete or cannot be trusted for compliance purposes. |
| `Fatal` | No usable document was produced. |

Severities are defaults. Raise or lower them per category or per code — see the diagnostic policy section of
`AGENTS.md` and the guide on diagnostics.

## Page template

Each page states: what it means, an example of the input that triggers it, the fallback applied, how to fix
the document, and how to escalate or suppress the diagnostic. See [EIV1042.md](EIV1042.md).
