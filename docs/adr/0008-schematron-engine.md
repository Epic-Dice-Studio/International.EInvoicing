# 0008 — How to execute Schematron rules

**Status:** Proposed · 2026-08-29 — decide at the start of the validation phase

## Context

EN 16931, Peppol and XRechnung publish their business rules as Schematron. Rewriting them in C# guarantees
divergence at each upstream release, so the artefacts must be executed rather than reimplemented. Schematron
compiles to XSLT 2.0, and .NET ships only XSLT 1.0. The rules also use XPath 2.0 constructs.

An additional constraint: the demo site runs the library in WebAssembly, so whatever engine is chosen must
work in a browser.

## Options

1. **Saxon-HE for .NET.** Faithful and complete. It is an IKVM port of a Java product: large, and its
   behaviour under WebAssembly is unverified.
2. **A minimal XPath 2.0 evaluator.** Implement only the subset the published artefacts actually use.
   Small and fast, but it is a language implementation, with the correctness risk that implies.
3. **Compile Schematron assertions to C# at build time.** A source generator emits one rule class per
   assertion. Fast, debuggable, AOT- and WASM-friendly, and error messages become ours to shape — which serves
   the goal of very clear validation output. Still requires evaluating the XPath subset, at build time rather
   than at runtime.

## A licensing constraint found after this ADR was written

The EN 16931 artefacts are EUPL-1.2, not Apache-2.0 (see [0009](0009-artefact-licensing.md)). Option 3
translates those rules into source code, which plausibly produces a derivative work under a reciprocal
licence. Option 3 is therefore **blocked pending legal review**, not merely pending a technical spike.
Options 1 and 2 execute the artefacts as data and do not raise the same question.

## Recommendation pending the spike

Option 3, with option 2's evaluator reused at build time. Run a spike over the real EN 16931 artefacts before
committing: measure how large the XPath subset actually is, and confirm behaviour in WebAssembly.

Whatever is chosen, the rule identifiers, severities and messages of the official artefacts are reproduced
faithfully, and the artefact version appears in every validation report.
