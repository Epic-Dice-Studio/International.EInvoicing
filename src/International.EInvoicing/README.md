# International.EInvoicing

Electronic invoicing for .NET, in one package.

```csharp
EInvoicing einvoicing = EInvoicing.CreateDefault();

DocumentResult result = einvoicing.Read(stream);

if (result.Invoice is { } invoice)
{
    Console.WriteLine(invoice.Number.Value);
}
```

You do not say which syntax arrived — UBL, CII, a Factur-X payload or a lifecycle status message are all
detected. Nothing throws on a document you received: unknown profiles, unreadable values and unmapped
elements come back as diagnostics with documented fallbacks.

Everything underneath stays reachable when you need it: the individual readers and writers, the profile
registry, the diagnostic policy.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
