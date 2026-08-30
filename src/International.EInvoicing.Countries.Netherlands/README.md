# International.EInvoicing.Countries.Netherlands

What Dutch electronic invoicing adds on top of the norms.

The Netherlands exchanges **Peppol BIS Billing**, with Dutch rules that travel inside the Peppol rule set. So
this package builds on `International.EInvoicing.Peppol`, and what lives here is the thing those rules are
strict about.

**The trap.** `NL-R-003` and `NL-R-005` are fatal: when the supplier is Dutch, *both* parties' legal entity
identifiers must carry scheme `0106` (KvK) or `0190` (OIN). An invoice that names both companies perfectly
and omits the scheme is refused.

```csharp
DutchEInvoicing nederland = DutchEInvoicing.Create();

EInvoice invoice = nederland.Invoice()                   // Peppol BIS Billing, UBL, EUR
    .WithNumber("2026-0001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .WithBuyerReference("REF-2026-0001")                 // BT-10: Peppol requires it, EN 16931 does not
    .From(seller => nederland.Describe(seller, "12345678", "Leverancier BV"))          // KvK
    .To(buyer => nederland.Describe(buyer, "00000001234567890000", NlLegalIdentifier.Oin, "Ministerie"))
    .AddLine(line => line.WithItem("Advies").WithNetAmount(3000m).WithVat("S", 21m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();
```

`NL-R-002` and `NL-R-004` also want a street, a city and a postcode on both Dutch parties.

**NLCIUS is deliberately absent.** Its published specification identifier is not in any artefact this
repository carries, and a guessed identifier is how a library starts rejecting valid documents. Register it
from your own code when you have it — a registered profile wins over anything built in.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
