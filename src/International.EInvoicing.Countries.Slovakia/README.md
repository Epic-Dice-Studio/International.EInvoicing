# International.EInvoicing.Countries.Slovakia

What Slovak electronic invoicing adds on top of the norms.

Slovakia's B2B obligation starts on **1 January 2027**, and it has two halves. The invoice travels between the
parties as **Peppol BIS Billing 3.0**, so this package builds on `International.EInvoicing.Peppol`. Within
fifteen minutes, a **tax data document** about it goes to the financial administration — a different document,
with its own identifier and its own 88 published assertions. That second half is what lives here.

```csharp
SlovakEInvoicing slovensko = SlovakEInvoicing.Create();

EInvoice invoice = slovensko.Invoice()
    .WithNumber("2026-0001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .From(seller => seller.Named("Dodávateľ s.r.o.").WithVatIdentifier("SK2020123456"))
    .To(buyer => buyer.Named("Odberateľ s.r.o.").WithVatIdentifier("SK2020654321"))
    .AddLine(line => line.WithItem("Poradenstvo").WithNetAmount(300m).WithVat("S", 23m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();

SkTaxData report = slovensko.TaxDataFor(invoice, uuid: "…", reportedDocumentUuid: "…");
report.Authority = new SkTaxAuthority { Id = "SK-FS" };
report.ReportingParty = new SkTaxDataEndpoint { Id = "…", SchemeId = "0158" };
report.ReceivingParty = new SkTaxDataEndpoint { Id = "…", SchemeId = SkTaxDataEndpoint.ServiceProviderScheme };

string xml = slovensko.Write(report);
```

`TaxDataFor` fills in what follows from the invoice and from the rules; the authority and the two endpoints
are the network's business, and are left to you rather than guessed.

**The reported document is a projection, not a copy.** Every rule describing it is written as *"MUST NOT
contain elements other than…"*, so the writer emits the allowed subset and drops the rest — the buyer
reference, the payment terms, the due date, the seller's contact. An invoice you can send is not a report you
can send.

**What this package does not do**, and cannot:

- **Transmit either document.** This library performs no network I/O, by design.
- **Read a tax data document back.** That is a receiver's job, and nothing has needed it yet.
- **Offer a Slovak CIUS.** None is published, and this library does not invent identifiers.
- **Check an IČO check digit.** Every check digit here is measured against the publisher's own rule; Peppol
  publishes none for Slovakia, and one tested against itself proves nothing.

The 88 assertions are not redistributable, so they are fetched rather than shipped:
`build/fetch-specs.sh national`, then `SkTaxDataValidator.LoadFrom(...)`.

See [docs/standards/country-sk.md](https://github.com/Epic-Dice-Studio/International.EInvoicing/blob/main/docs/standards/country-sk.md).

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
