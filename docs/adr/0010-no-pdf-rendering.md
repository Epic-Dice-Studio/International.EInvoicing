# 0010 — Hybrid invoices attach to an existing PDF; we never render one

**Status:** Accepted · 2026-08-30

## Context

A Factur-X or ZUGFeRD invoice is a PDF a person reads with a CII payload embedded in it. Producing one could
mean two very different things: embedding the payload into a PDF the caller already has, or also drawing that
PDF.

Drawing it would drag in layout, fonts, localisation, templating and PDF/A conformance — an entire product,
and one that has nothing to do with electronic invoicing norms.

## Decision

This library **attaches the CII payload to a PDF the caller supplies**, and extracts it back out. It never
renders a PDF, and it does not convert a plain PDF into a PDF/A-3 one: colour spaces, embedded fonts and
output intents are properties of the document you start from.

`IPdfAttachmentWriter` writes what makes a PDF a Factur-X document — the embedded file, its declaration as an
associated file with the `Alternative` relationship, and the XMP metadata naming the profile.

## Consequences

- Callers keep their existing reporting or PDF stack, whatever it is. Nothing here competes with it.
- The two halves of a hybrid invoice agree, because the PDF is yours and the XML comes from the same model
  you rendered it from.
- A receiver that requires PDF/A-3 conformance needs a PDF/A-conforming document to start from. The support
  matrix says so rather than implying we handle it.
- `International.EInvoicing.FacturX.PdfSharp` is one implementation of the abstraction, not a dependency of
  the design: bring iText, Aspose, QuestPDF or your own.
