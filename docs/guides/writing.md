# Writing a document

## Build it, then choose a syntax

The model is EN 16931. UBL and CII are two ways of writing the same thing, so you build once and choose at
the end.

```csharp
using International.EInvoicing;
using International.EInvoicing.Building;
using International.EInvoicing.Profiles;

EInvoice invoice = EInvoiceBuilder.Create(KnownProfiles.En16931Cii)
    .WithNumber("FA-2026-001")
    .IssuedOn(new DateOnly(2026, 8, 30))
    .DueOn(new DateOnly(2026, 9, 29))
    .OfType("380")                       // BT-3: 380 is a commercial invoice, 381 a credit note
    .InCurrency("EUR")
    .From(seller => seller
        .Named("Epic Dice Studio")
        .WithVatIdentifier("FR12345678901")
        .WithAddress(address =>
        {
            address.Line1 = "1 rue de la Facture";
            address.City = "Angers";
            address.PostCode = "49000";
            address.CountryCode = "FR";
        }))
    .To(buyer => buyer.Named("Acme").WithVatIdentifier("FR98765432109"))
    .AddLine(line => line
        .WithIdentifier("1")
        .WithItem("Consulting")
        .WithQuantity(3m, "HUR")         // BT-130: UN/ECE Recommendation 20
        .WithNetPrice(150m)
        .WithNetAmount(450m)
        .WithVat("S", 20m))              // BT-151, BT-152
    .AddVatBreakdown("S", 20m, taxableAmount: 450m, taxAmount: 90m)
    .Build();

EInvoicing einvoicing = EInvoicing.CreateDefault();

string ubl = einvoicing.Write(invoice, DocumentFormat.Ubl);
string cii = einvoicing.Write(invoice, DocumentFormat.Cii);
string asDeclared = einvoicing.Write(invoice);   // the syntax the declared profile is written in
```

`From` and `To` are the seller and the buyer — an invoice has a direction, and saying it reads better than
naming roles. `WithSeller` and `WithBuyer` say the same thing in the norm's own words; use whichever suits
the code around it. When a name and a VAT number are all you have, there is a shorter form:

```csharp
.From("Epic Dice Studio", "FR12345678901")
.To("Acme", "FR98765432109")
```

Amounts inherit the document currency as they are added, so an invoice cannot end up with lines in a currency
it never declared.

## Totals: derived if you ask, never behind your back

The `BR-CO` rules tie the totals to the lines and to the VAT breakdown, and they are where documents most
often stop validating — almost never because the arithmetic was hard, but because a total was typed in beside
the lines it summarises and then one of the two changed.

So ask for them:

```csharp
.WithComputedVatBreakdown()   // BG-23, grouped by category and rate
.WithComputedTotals()         // BT-106, BT-107, BT-108, BT-109, BT-110, BT-112, BT-115
```

`WithComputedVatBreakdown()` groups the lines by VAT category and rate, and applies each document-level
allowance or charge to the entry with the same category and rate — a discount on the whole invoice reduces
the base it was taken from, not every base. `WithComputedTotals()` then adds up the lines, the allowances and
charges, and the VAT. A prepaid amount (BT-113) or a rounding amount (BT-114) you set yourself is kept and
taken into account. Both round to two decimals; pass another number when your currency asks for one.

Call them last, once the lines are in.

They are opt-in, not automatic: computing totals behind your back would quietly replace what you meant to
send with what this library guessed. When your rounding rules are your own, set them yourself and skip both:

```csharp
.WithTotals(totals =>
{
    totals.LineTotalAmount = new AmountField(450m, "EUR");     // BT-106
    totals.TaxExclusiveAmount = new AmountField(450m, "EUR");  // BT-109
    totals.TaxAmount = new AmountField(90m, "EUR");            // BT-110
    totals.TaxInclusiveAmount = new AmountField(540m, "EUR");  // BT-112
    totals.DuePayableAmount = new AmountField(540m, "EUR");    // BT-115
})
```

Either way, [validate](validation.md) to confirm they hold.

## A credit note

Same model, different type code. Amounts stay positive.

```csharp
EInvoice creditNote = EInvoiceBuilder.Create(KnownProfiles.En16931Ubl)
    .WithNumber("AV-2026-001")
    .IssuedOn(new DateOnly(2026, 8, 30))
    .OfType("381")
    .InCurrency("EUR")
    .Extend(document => document.PrecedingInvoices.Add(new DocumentReference
    {
        Identifier = "FA-2026-001",                     // BT-25
        IssueDate = new DateOnly(2026, 7, 1),           // BT-26
    }))
    .Build();
```

Writing UBL emits an `Invoice` document; a receiver tells the two apart by BT-3, as the norm intends.

## Anything the builder does not cover

`Extend` hands you the model. Nothing is hidden behind the builder.

```csharp
EInvoiceBuilder.Create(profile)
    .WithNumber("FA-1")
    .Extend(document =>
    {
        document.TenderOrLotReference = "LOT-7";        // BT-17
        document.BuyerAccountingReference = "CC-42";    // BT-19
        document.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",                       // BT-81, UNTDID 4461
            RemittanceInformation = "FA-2026-001",      // BT-83
        };
    })
    .AddLine(line => line.Extend(l => l.BuyerAccountingReference = "CC-42"))
    .Build();
```

## Something the norm has no field for

Put it in extension data and it is written back where it belongs — inside the node that owns it, not at the
top of the file.

```csharp
invoice.Lines[0].Extensions.Add(new ExtensionElement(
    "urn:acme:invoice:1p0",
    "PurchaseOrderScan",
    "<acme:PurchaseOrderScan xmlns:acme=\"urn:acme:invoice:1p0\">PO-42</acme:PurchaseOrderScan>"));
```

This is also how content survives a round trip: an element the reader did not map is kept and written back
unchanged.

## A hybrid Factur-X invoice

This library does not render PDFs. You supply the PDF a person reads, it embeds the machine-readable half —
which is also what keeps the two halves agreeing, since both come from the same model.

```csharp
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.FacturX;
using International.EInvoicing.FacturX.PdfSharp;

var writer = new FacturXWriter(new CiiInvoiceWriter(), new PdfSharpAttachmentWriter());

using FileStream humanReadable = File.OpenRead("invoice.pdf");
using FileStream hybrid = File.Create("invoice-facturx.pdf");

writer.Write(invoice, humanReadable, hybrid);
```

The payload is filed as `factur-x.xml` with the `Alternative` relationship and the XMP metadata naming the
profile — without those, conforming readers do not find it.

Choosing the profile is a business decision, so nothing picks one for you. MINIMUM and BASIC WL carry no
invoice lines and are **not** EN 16931 invoices; a document declaring one is read and reported
([EIV4010](../diagnostics/EIV4010.md)).

## What round-tripping guarantees

A field read from a document and not modified is written back from its raw text, including a date's original
format code. A document that passes through unchanged comes out equivalent to the one that went in —
equivalent after canonicalisation, not byte for byte: namespace prefixes, insignificant whitespace and
attribute order are not normative.

## Going one layer down

```csharp
string ubl = einvoicing.UblWriter.WriteToString(invoice);
string cii = einvoicing.CiiWriter.WriteToString(invoice);

using FileStream file = File.Create("invoice.xml");
einvoicing.UblWriter.Write(invoice, file);
```

## Next

- [Lifecycle statuses](lifecycle.md)
- [Validation](validation.md)
- [Reading a document](reading.md)

## Text that XML cannot carry

Descriptions and names come from accounting systems, and some of them carry control characters — XML has no
escape for those, and writing one would fail with a message naming a hexadecimal value rather than a field.
They are dropped when a document is written; accents, symbols and characters outside the basic plane are
written exactly as given. `XmlCharacters.Sanitize` is the same helper, should your own writer need it.

## Run it

[`samples/International.EInvoicing.Samples/Chapters/Invoices.cs`](https://github.com/Epic-Dice-Studio/International.EInvoicing/blob/main/samples/International.EInvoicing.Samples/Chapters/Invoices.cs) is this page as code — building, writing and choosing a syntax.

```bash
dotnet run --project samples/International.EInvoicing.Samples
```
