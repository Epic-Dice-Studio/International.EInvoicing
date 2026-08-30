# International.EInvoicing

Electronic invoicing for .NET, in one package.

```csharp
EInvoicing einvoicing = EInvoicing.CreateDefault();

if (einvoicing.Read(stream).TryGetInvoice(out EInvoice? invoice))
{
    Console.WriteLine(invoice.Number.Value);
}
```

In a container, one call wires the readers, the writers, the profiles and the rules, and makes the facade
injectable:

```csharp
services.AddEInvoicing(einvoicing => einvoicing.AddDefaults().AddFrance());
```

You do not say which syntax arrived — UBL, CII, a Factur-X payload or a lifecycle status message are all
detected. Nothing throws on a document you received: unknown profiles, unreadable values and unmapped
elements come back as diagnostics with documented fallbacks.

Everything underneath stays reachable when you need it: the individual readers and writers, the profile
registry, the diagnostic policy.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
