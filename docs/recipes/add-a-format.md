# Recipe — add a syntax

A *syntax* is an XML dialect that carries invoice data: UBL, CII, CDAR. Adding one means a native model, a
reader, a writer, and a mapper to and from the canonical model.

## 1. Prepare

- Add the schemas to `specs/<syntax>/` with a `PROVENANCE.md`, via `build/fetch-specs.sh`.
- Write `docs/standards/<syntax>.md` using the template in `docs/standards/README.md`. Do this **before**
  the code: the pitfalls section is what stops you writing the same bugs as everyone else.
- Add the golden files (official samples only) under `tests/.../golden/<syntax>/`.

## 2. Create the package

`src/International.EInvoicing.<Syntax>/` with:

```
Model/          native types, faithful to the schema, every data property a Field<T>
Reading/        <Syntax>InvoiceReader    : IDocumentReader<EInvoice>
Writing/        <Syntax>InvoiceWriter    : IDocumentWriter<EInvoice>
Mapping/        <Syntax>InvoiceMapper    : IInvoiceMapper<TNative>
<Syntax>ServiceCollectionExtensions.cs   Add<Syntax>() registration
```

## 3. Implement

- Read and write with `XmlReader`/`XmlWriter`, obtained through `SecureXml`. No reflection, no
  `XmlSerializer`.
- `ReadAsync` and `WriteAsync` come from `DocumentStreams`: the transfer is awaited, the parse is not. Do not
  write a second, asynchronous copy of the mapping — see [ADR 0012](../adr/0012-async-at-the-boundary.md).
- Register both by interface and by concrete type, so a caller can inject either and substitute yours:
  `services.TryAddEnumerable(ServiceDescriptor.Singleton<IDocumentReader<EInvoice>, MyReader>())`.
- Respect the schema's element order on write — it is normative.
- Bind elements by namespace URI and local name, never by prefix.
- Preserve every attribute the schema allows on a value into the matching `Field<T>` type.
- Never throw on input. Unknown element → `ExtensionData` plus an `UnmappedElement` diagnostic. Unparsable
  value → keep the raw text, mark the field `IsRawOnly`, emit an `InvalidValue` diagnostic.
- Check `SecureXml.IsDepthExceeded` as you descend.

## 4. Prove it

All five families from `AGENTS.md` §4. In particular, round-trip every golden file and compare against the
original after C14N canonicalisation — that test is what proves the raw-preservation design actually works.

## 5. Publish the fact

Update `docs/coverage.json` and run `dotnet run --project build/Tools -- coverage`. A capability that is not in the matrix
does not exist as far as users are concerned.
