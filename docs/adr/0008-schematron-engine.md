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

## A licensing constraint, and what it settles

The EN 16931 artefacts are EUPL-1.2, not Apache-2.0 (see [0009](0009-artefact-licensing.md)). That licence is
reciprocal, and it separates the options cleanly:

- Options 1 and 2 **execute the artefacts as data**. Using a file is not deriving from it, so nothing about
  the library's own licensing changes.
- Option 3 **translates the rules into source code**, which plausibly produces a derivative work. The
  generated rules would then be EUPL inside an MIT package — workable, but it turns every consumer's licence
  review into a conversation.

## Recommendation

**Option 2**: a minimal XPath 2.0 evaluator covering the subset the published artefacts actually use, running
the `.sch` files as data.

It avoids the derivation question entirely, and the reason that matters most is not legal: an engine that
executes the official artefacts stays correct when they are revised, whereas generated or hand-written rules
drift from the norm at every upstream release. Option 1 (Saxon-HE) remains the fallback if the XPath subset
turns out to be larger than expected.

The spike still has to happen, and its questions are unchanged: how large is that subset over the real
artefacts, and does the engine run under WebAssembly for the demo site.

Whatever is chosen, the rule identifiers, severities and messages of the official artefacts are reproduced
faithfully, and the artefact version appears in every validation report.
