# Security policy

## Reporting a vulnerability

Report privately through GitHub Security Advisories on this repository
("Security" → "Report a vulnerability"). Please do not open a public issue.

Include the affected version, a minimal document or code sample that reproduces the problem, and what an
attacker gains. We aim to acknowledge within five working days.

## Threat model

This library parses documents **received from third parties**. That is its threat model, and it drives
several non-negotiable design rules:

- All XML is read through `SecureXml`: DTD processing prohibited, no XML resolver, bounded entity expansion,
  bounded document size. This closes XXE, external-entity SSRF and entity-expansion attacks.
- Resource limits (`DocumentLimits`) bound document size, nesting depth, line count and embedded attachment
  size. Exceeding one produces a fatal diagnostic, never an unbounded allocation.
- Incoming PDFs are treated as hostile: no embedded JavaScript is executed, no external reference is
  followed, and extracted attachments are handed back as streams — the library never writes them to disk.
- The library performs **no network I/O**. Code lists and validation artefacts are embedded resources; nothing
  is fetched at runtime.
- Compiled validation artefacts ship as embedded resources. Loading an external rule set is possible but
  explicit, and is documented as executing third-party logic.

If you find a way around any of the above, that is a vulnerability — please report it.
