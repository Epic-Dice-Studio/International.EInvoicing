# International.EInvoicing.OrderX

Order-X: the Franco-German order, order change and order response, published by FNFE-MPE and FeRD — the same
two who publish Factur-X, one document earlier in the chain.

It is CII, but it is not the Cross Industry Invoice. The Cross Industry Order is a different UN/CEFACT
message, on version 128 of the same data types, so nothing that reads an invoice reads it and every element
in the document has a different name.

The order and the order change are read and written; they fill the same `Order` model the Peppol ordering
documents do, and `DocumentResult.Kind` tells them apart. Unmapped content is kept verbatim and written back
inside the element it sat in, after the sibling it followed — element order is normative here, so where it
goes back is part of not losing it.

```csharp
services.AddEInvoicing(o => o.AddOrderX());
```

FNFE-MPE and FeRD publish the schemas and rule sets behind a registration, so this package does not ship
them. `build/fetch-specs.sh order-x` brings them, and then:

```csharp
services.AddEInvoicing(o => o
    .AddOrderX()
    .AddOrderXSchemaFrom("specs/order-x/schema")
    .AddOrderXRulesFrom("specs/order-x/schematron"));
```

The order response (type code 231) is not implemented yet.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
