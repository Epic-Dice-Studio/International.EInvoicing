# One country, one object

Most integrations invoice in exactly one country. Doing that with the general library means knowing, before
writing a line, which profile that country expects, which business process it adds on top of EN 16931, which
rule sets have to be registered, and — in France — that four different documents arrive through the same
channel.

Three types hold that knowledge for you:

| | |
|---|---|
| `FrenchEInvoicing` | `International.EInvoicing.Countries.France` |
| `GermanEInvoicing` | `International.EInvoicing.Countries.Germany` |
| `BelgianEInvoicing` | `International.EInvoicing.Countries.Belgium` |
| `NorwegianEInvoicing` | `International.EInvoicing.Countries.Norway` |
| `SwedishEInvoicing` | `International.EInvoicing.Countries.Sweden` |
| `DanishEInvoicing` | `International.EInvoicing.Countries.Denmark` |
| `DutchEInvoicing` | `International.EInvoicing.Countries.Netherlands` |
| `IcelandicEInvoicing` | `International.EInvoicing.Countries.Iceland` |

None of them is a wall. Each exposes `.Library`, the fully assembled `EInvoicing` underneath, so anything the
shortcut does not cover is one property away.

## France

France exchanges four documents and the reform requires all of them: invoices, credit notes, lifecycle
statuses (CDAR) and e-reporting transmissions (*flux 10*). They use two syntaxes, a third vocabulary, and one
of them carries no XML namespace at all. A French integration receives all four on the same channel — so
reading takes one call and tells you which arrived.

```csharp
using International.EInvoicing.Countries.France;

FrenchEInvoicing france = FrenchEInvoicing.Create();

FrenchDocument document = france.ReadFile(path);

switch (document.Kind)
{
    case FrenchDocumentKind.Invoice:
    case FrenchDocumentKind.CreditNote:  Handle(document.Invoice!); break;
    case FrenchDocumentKind.LifecycleStatus: Handle(document.LifecycleStatus!); break;
    case FrenchDocumentKind.EReport:     Handle(document.EReport!); break;
    case FrenchDocumentKind.Unknown:     Reject(document.Errors); break;
}
```

`var (kind, invoice, status, report) = document;` says the same thing, and `TryGetInvoice`,
`TryGetLifecycleStatus` and `TryGetEReport` are there for the branch you actually want.

Writing the four:

```csharp
EInvoice invoice = france.Invoice()                       // Extended CTC FR, UBL, BT-23 filled in
    .WithNumber("F202600001")
    .IssuedOn(new DateOnly(2026, 9, 4))
    .From("Fournisseur SARL", "FR32732829320")
    .To("Client SA", "FR89552081317")
    .AddLine(line => line.WithItem("Conseil").WithNetAmount(1000m).WithVat("S", 20m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();

LifecycleStatusMessage approved = france
    .StatusFromBuyer("200000008", "ACHETEUR")             // who reports it
    .SentBy("0003", "PA-E Acheteur")                      // whose platform transmits it
    .ToSeller("100000009", "VENDEUR", "100000009_STATUTS")
    .About("F202600001", new DateOnly(2026, 9, 4))
    .Approved(DateTimeOffset.UtcNow);

FrEReport september = france
    .ReportTransactions(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30))
    .From("0003", "PA-E Vendeur")
    .For("100000009", "VENDEUR")
    .Day(new DateOnly(2026, 9, 4), FrEReportCodes.RetailTransactions, day => day.At(20m, 1000m, 200m))
    .Build();

string xml = france.Write(invoice);                       // and .Write(approved), .Write(september)
```

`france.CreditNote()` is `Invoice()` with a credit-note type code. `Invoice(syntax, businessProcess)` and
`CreditNote(syntax, businessProcess)` name the syntax and the *cas d'usage* (BT-23) explicitly.

Hybrid PDFs need the PDF half, passed in once:

```csharp
FrenchEInvoicing france = FrenchEInvoicing.Create(new PdfSharpAttachmentReader());
```

See [lifecycle statuses](lifecycle.md) and [e-reporting](e-reporting.md) for what each document means.

## Germany

Germany is XRechnung, and its rules ship with this library — nothing to fetch, so `Validate` works out of the
box.

```csharp
using International.EInvoicing.Countries.Germany;

GermanEInvoicing germany = GermanEInvoicing.Create();

EInvoice invoice = germany.InvoiceToPublicBody("04011000-1234512345-06")   // Leitweg-ID → BT-10
    .WithNumber("RE-2026-001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .From(seller => seller.Named("Epic Dice Studio GmbH").WithVatIdentifier("DE123456789"))
    .To(buyer => buyer.Named("Behörde"))
    .AddLine(line => line.WithItem("Beratung").WithNetAmount(450m).WithVat("S", 19m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();

ValidationReport report = germany.Validate(germany.Write(invoice));   // EN 16931-1:2017 (UBL), XRechnung (UBL)
```

`InvoiceToPublicBody` parses the Leitweg-ID and checks its check digits before writing it, so a routing
identifier that would have been rejected on arrival is rejected here instead, with a `FormatException` naming
what is wrong. `germany.Invoice()` is the same without the routing identifier, and both take a
`DocumentSyntax` when you want CII rather than UBL.

## Belgium

Belgium mandates Peppol BIS Billing rather than a Belgian format. Most of what the shortcut does is make that
easy to act on:

```csharp
using International.EInvoicing.Countries.Belgium;

BelgianEInvoicing belgium = BelgianEInvoicing.Create();

EInvoice invoice = belgium.Invoice()                       // Peppol BIS Billing 3.0 + BT-23
    .WithNumber("2026-0001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .From(seller => belgium.Describe(seller, "0776.914.174", "Epic Dice Studio BV"))
    .To("Klant NV", "BE0403170701")
    .AddLine(line => line.WithItem("Advies").WithNetAmount(1000m).WithVat("S", 21m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();

string reference = belgium.StructuredCommunication(123456789);   // +++012/3456/78939+++
```

`Describe` checks the KBO/BCE number modulo 97, derives the VAT identifier from it, and writes it as the
electronic address in scheme `0208` — the one Peppol reserves for Belgian enterprise numbers. Punctuation is
accepted in any form.

**The Peppol rules are not shipped**: OpenPEPPOL publishes them without a licence that permits
redistribution, so this library fetches them rather than carrying them. Until you supply them, `Validate`
reports the Peppol profile as *unchecked* rather than as passed:

```csharp
BelgianEInvoicing belgium = BelgianEInvoicing.Create(library => library
    .AddDefaults()
    .AddBelgium()
    .AddPeppolRulesFrom("specs/peppol/rules"));           // ./build/fetch-specs.sh puts them there
```

## Norway, Sweden and Denmark

The three Nordic shortcuts are the same shape as the Belgian one, because their situation is the same one:
Peppol BIS Billing, with national rules that travel **inside** the Peppol rule set rather than in a separate
artefact to fetch. What each adds is its own legal identifier, and Denmark adds one trap worth knowing about.

```csharp
NorwegianEInvoicing norge = NorwegianEInvoicing.Create();

EInvoice invoice = norge.Invoice()                       // EHF 3.0, UBL, NOK, Peppol's business process
    .WithNumber("2026-0001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .WithBuyerReference("REF-2026-0001")                 // BT-10: Peppol requires it, EN 16931 does not
    .From(seller => norge.Describe(seller, "915 442 552", "Leverandør AS"))
    .To(buyer => buyer.Named("Kunde AS"))
    .AddLine(line => line.WithItem("Rådgivning").WithNetAmount(3000m).WithVat("S", 25m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();
```

`SwedishEInvoicing` and `DanishEInvoicing` read the same way, in SEK and DKK. `Describe` checks the legal
identifier — organisasjonsnummer modulo 11, organisationsnummer by Luhn, CVR by shape — and writes it in the
scheme Peppol reserves for it. Those checks are not trusted on their own: a test hands every number the
library accepts, and a set it refuses, to **Peppol's own rule** for that scheme and fails on disagreement.

Norway declares **EHF 3.0**, the Norwegian CIUS of Peppol BIS; Sweden and Denmark declare Peppol BIS itself,
which is what they exchange.

**The Danish trap.** Payment means code `30` — plain credit transfer — is valid EN 16931 and is refused
between two Danish parties by `DK-R-005`, a fatal rule:

```csharp
invoice.Payment!.MeansTypeCode = DkPaymentMeans.SepaCreditTransfer;   // 58; DkPaymentMeans.All has the rest
```

## The Netherlands and Iceland

Same shape again, and each has one national rule that rejects an otherwise perfect invoice — which is the
whole reason these two shortcuts exist rather than a line of documentation.

**The Netherlands.** `NL-R-003` and `NL-R-005` are fatal: when the supplier is Dutch, *both* parties' legal
entity identifiers must carry scheme `0106` (KvK) or `0190` (OIN).

```csharp
DutchEInvoicing nederland = DutchEInvoicing.Create();

nederland.Invoice()
    .From(seller => nederland.Describe(seller, "12345678", "Leverancier BV"))                    // KvK
    .To(buyer => nederland.Describe(buyer, "00000001234567890000", NlLegalIdentifier.Oin, "Ministerie"));
```

**Iceland.** `IS-R-002` and `IS-R-004` say the same thing about scheme `0196`, the kennitala — whose check
digit `Describe` verifies before writing it.

```csharp
IcelandicEInvoicing island = IcelandicEInvoicing.Create();

island.Invoice().From(seller => island.Describe(seller, "120000-0350", "Seljandi ehf"));
```

NLCIUS is deliberately absent from the Dutch package: its published specification identifier is not in any
artefact this repository holds, and guessing one is how a library starts rejecting valid documents. Register
it yourself and it wins.

## Wiring one into a container

`Create(configure)` takes the same builder the general library takes, so anything you would have registered
still applies; and `Over(library)` wraps an `EInvoicing` you already resolved:

```csharp
builder.Services.AddEInvoicing(einvoicing => einvoicing.AddDefaults().AddFrance());
builder.Services.AddSingleton(provider =>
    FrenchEInvoicing.Over(provider.GetRequiredService<EInvoicing>()));
```

## Somewhere else?

Only these eight countries have a shortcut today. Every other country is reachable through the general
library — a profile, a rule set fetched from its publisher, and the identifiers it needs. What is planned,
country by country and in what order, is in the [roadmap](../roadmap.md).
