# Architecture decision records

Short records of decisions that are expensive to reverse. If you find yourself asking "why is it like this?",
the answer should be here — and if it is not, add it.

Format: context, decision, consequences. New record ⇒ next number, never renumber. A superseded record stays
in place with a link to the one that replaced it.

| # | Decision | Status |
|---|---|---|
| [0001](0001-two-model-layers.md) | Native models and a canonical model, in two layers | Accepted |
| [0002](0002-raw-preserving-fields.md) | Every data property is a raw-preserving `Field<T>` | Accepted |
| [0003](0003-diagnostics-not-exceptions.md) | Readers report diagnostics instead of throwing | Accepted |
| [0004](0004-hand-written-xml.md) | Hand-written `XmlReader`/`XmlWriter` serialisation | Accepted |
| [0005](0005-package-layout.md) | Core + one package per format + one per country | Accepted |
| [0006](0006-no-transport.md) | No network transport, ever | Accepted |
| [0007](0007-target-frameworks.md) | Target `net8.0` and `net10.0` | Accepted |
| [0008](0008-schematron-engine.md) | How to execute Schematron rules | Proposed |
| [0009](0009-artefact-licensing.md) | Licensing of the normative artefacts we redistribute | Accepted |
| [0010](0010-no-pdf-rendering.md) | Hybrid invoices attach to an existing PDF; we never render one | Accepted |
