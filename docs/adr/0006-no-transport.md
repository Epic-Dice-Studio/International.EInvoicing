# 0006 — No network transport, ever

**Status:** Accepted · 2026-08-29

## Context

Users will ask for Peppol AS4, French approved-platform APIs, Chorus Pro and Mercurius connectors. Those are
where certificates, credentials, retries and legal liability live.

## Decision

This library builds, reads and validates documents. It performs no network I/O at all — not even to resolve a
code list. Transport belongs to the access point, and to other libraries.

## Consequences

- The whole library runs in WebAssembly, which is what makes the browser-based demo site possible: invoices
  never leave the visitor's machine.
- No certificate handling, no secret management, no outbound-connection surface to audit.
- Code lists and validation artefacts are embedded resources, so they are versioned with the release and work
  offline.
- An architecture test enforces the rule rather than trusting review.
- Users needing transmission compose this library with their access point's SDK. The README says so plainly.
