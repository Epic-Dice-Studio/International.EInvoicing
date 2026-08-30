# ADR 0012 — Asynchronous at the boundary, synchronous at the parse

**Status:** accepted, August 2026

## Context

"Async all the way" is the right default for .NET libraries, and it is worth being precise about why: it
exists so that a thread is not held while something *waits* — a socket, a disk, a database. Making CPU work
asynchronous buys nothing and costs a state machine, an allocation and a harder stack trace.

This library performs **no network I/O at all** ([ADR 0006](0006-no-transport.md)). The only waiting it can
possibly do is receiving the bytes of a document a caller hands it, and sending the bytes back. Everything
between is parsing, mapping and validating: work, not waiting.

There was also an inconsistency worth naming. `SecureXml` set `XmlReaderSettings.Async = true` on every
reader it created, which selects the asynchronous-capable code path inside `XmlReader` — while nothing in the
library ever called `ReadAsync`. We paid for a capability we never used.

## Decision

**Every reader and writer exposes an asynchronous entry point, and the asynchrony is the transfer.**

```csharp
ParseResult<EInvoice> Read(Stream stream);
Task<ParseResult<EInvoice>> ReadAsync(Stream stream, CancellationToken cancellationToken = default);
```

`ReadAsync` awaits `CopyToAsync` and then parses what arrived. `WriteAsync` serialises and then awaits the
send. `DocumentStreams` holds both halves so every reader and writer behaves the same way.

`XmlReaderSettings.Async` is off.

## Why not asynchronous parsing

`XmlReader.ReadAsync` throughout would be async all the way down. It was considered and rejected:

- **The readers build a full `XDocument` anyway.** Async parsing avoids buffering the raw bytes, but the
  parsed tree is bigger than the bytes and is materialised regardless. The saving is smaller than the cost.
- **Invoices are small.** An EN 16931 invoice is tens of kilobytes; the largest realistic one, carrying an
  embedded attachment, is a few megabytes, and `DocumentLimits` caps that. Buffering is not the bottleneck.
- **It would double the readers.** Around 2 500 lines of mapping exist per syntax pair. Two versions of them
  drift; one asynchronous version forces every caller to await work that never waits.

If a document large enough to matter ever appears — a batch format, say — that is a streaming reader, not an
async one, and it would be a different type with a different contract.

## Consequences

**A caller on an ASP.NET Core endpoint has an asynchronous path end to end**, at every layer rather than only
on the facade: the wait on the request body is awaited, and so is the write to the response.

**Cancellation is honoured where it can be.** A token stops the transfer. It does not interrupt parsing,
because parsing does not block.

**The documentation says which is which.** Every `…Async` method carries the same sentence: the awaiting is
the transfer. A developer should never have to guess whether an `Async` suffix means "this waits" or "this
was made to look like it waits".
