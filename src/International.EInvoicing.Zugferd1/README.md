# International.EInvoicing.Zugferd1

ZUGFeRD 1.0: the 2013 German hybrid invoice, replaced by ZUGFeRD 2 in 2019 and still sitting in archives.

Reading only, on purpose. What an archive needs is a way forward, not a way to make more of the format its
own publisher retired — so this package reads ZUGFeRD 1.0 into the same model as everything else, and
`EInvoicing.Convert` writes it out again as ZUGFeRD 2, Factur-X, CII or UBL.

Nothing in a 2013 document is dropped. What EN 16931 never had a term for — the German `Bankleitzahl`, most
visibly — is kept verbatim and reported, because an archive is read to find out what a document *said*.

```csharp
services.AddEInvoicing(o => o.AddZugferd1());

EInvoice invoice = library.Zugferd1.Read(bytes).Value!;

// A migration states what the document now claims to conform to. This library will not invent one.
invoice.SpecificationIdentifier = KnownProfiles.En16931Cii.Id;
string forward = library.Write(invoice, DocumentFormat.Cii);
```

FeRD no longer publishes the format, so the schema and rule set are fetched rather than shipped —
`build/fetch-specs.sh zugferd1`, then `AddZugferd1SchemaFrom` and `AddZugferd1RulesFrom`.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
