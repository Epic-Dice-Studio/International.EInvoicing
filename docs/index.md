# International.EInvoicing

Generate, read and validate electronic invoices in .NET — for every country, without fighting the library.

> **Pre-alpha.** Nothing is published on NuGet yet. What follows describes the design being built; the
> support matrix in the [repository README](https://github.com/Epic-Dice-Studio/International.EInvoicing)
> is the honest state of things.

## Try it without installing anything

**[Open the playground →](https://epic-dice-studio.github.io/International.EInvoicing/demo/)**

Pick a country, build an invoice, check one you already have, and look inside any field — all of it running
in your browser. No document you open there reaches a server, because there is no server: the library is
compiled to WebAssembly and runs on your machine.

## Start here

| | |
|---|---|
| [Playground](https://epic-dice-studio.github.io/International.EInvoicing/demo/) | The library itself, running in your browser |
| [Guides](guides/README.md) | How to do a specific thing, with code |
| [Standards](standards/README.md) | One page per norm: sources, mappings, pitfalls |
| [Diagnostics](diagnostics/README.md) | What each `EIV` code means and how to act on it |
| [Recipes](recipes/README.md) | Add a format, a country, a profile, a rule |
| [Decisions](adr/README.md) | Why the library is shaped the way it is |
| [Roadmap](roadmap.md) | What comes next, and why it is next |

## The three promises

**Extensible without forking.** Register your own reader, writer, profile or rule set and it takes precedence
over ours. A profile we have not shipped is something you can add today.

**Nothing is lost, nothing explodes.** Every field keeps the raw text and XML attributes it came with — see
[raw values](guides/raw-values.md). Readers never throw on a document you received: unknown profiles, illegal
values and unmapped elements become diagnostics with documented fallbacks.

**Honest about its limits.** A profile we do not support is reported in the diagnostics, marks the validation
report incomplete, and appears as unsupported in the matrix. A partial validation is never a success.
