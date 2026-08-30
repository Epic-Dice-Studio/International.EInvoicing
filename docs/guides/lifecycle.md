# Lifecycle statuses

What happened to an invoice after it was sent — filed, received, approved, disputed, refused, paid. In France
these are the *statuts de cycle de vie* the 2026 reform makes mandatory between platforms, carried as
UN/CEFACT CDAR messages.

## Say who reports what, through whom, to whom

A lifecycle message has three parties, and it is easy to fill in the wrong one:

| | Who it is | Element |
|---|---|---|
| **Issuer** | who reports the status | `ram:IssuerTradeParty` |
| **Sender** | the approved platform that transmits it | `ram:SenderTradeParty` |
| **Recipient** | who it is for | `ram:RecipientTradeParty` |

So the builder reads as the sentence:

```csharp
using International.EInvoicing.Countries.France.Lifecycle;

LifecycleStatusMessage approved = FrCdar
    .FromBuyer("200000008", "ACHETEUR")                        // who reports it
    .SentBy("0003", "PA-E Acheteur")                           // their approved platform
    .ToSeller("100000009", "VENDEUR", "100000009_STATUTS")     // who it is for, and where it is delivered
    .About("F202500003", new DateOnly(2025, 7, 1))             // which invoice
    .Approved();                                               // and what happened to it

string xml = EInvoicing.CreateDefault().Write(approved);
```

A status is not one code but three: the status itself, the type code of the acknowledgement carrying it, and
the referenced document's status code. Getting the other two wrong produces a message that names the right
status and is rejected anyway — so nothing here asks you for them.

## Where it goes changes the profile

Sending a status to a trading partner and reporting one to the public portal are **two different profiles**,
not two variants of one: different context, different addressing, an extra element inside the reference. The
destination you name settles it.

```csharp
.ToSeller("100000009", "VENDEUR", "100000009_STATUTS")   // urn.cpro.gouv.fr:1p0:CDV:invoice
.ToBuyer("200000008", "ACHETEUR")                        // the same profile, the other direction
.ToPartner(partner => …)                                 // the same, described in full
.ToPublicPortal()                                        // urn.cpro.gouv.fr:1p0:CDV:einvoicingF2
```

Sending to a partner addresses the public portal as a second recipient, which that profile expects. You do
not add it yourself.

```csharp
LifecycleStatusMessage reported = FrCdar
    .FromPlatform("0003", "PA-E Vendeur")
    .ToPublicPortal()
    .About("F202500003", new DateOnly(2025, 7, 1))
    .Filed(new DateTimeOffset(2025, 7, 1, 15, 10, 0, TimeSpan.Zero));
```

## Who may report which status

This is the part that trips people up, and the builder now refuses to get it wrong.

| Start from | Who that is | The statuses it may report |
|---|---|---|
| `FrCdar.FromPlatform(id, name)` | the platform handling the invoice | 200 filed, 201 issued, 202 received, 203 made available, 213 rejected |
| `FrCdar.FromBuyer(siren, name)` | the customer | 204 taken in charge, 205 approved, 207 disputed, 210 refused, 211 payment sent |
| `FrCdar.FromSeller(siren, name)` | the supplier | 212 collected |

A platform reports on its own behalf, so it is both issuer and sender and needs no `SentBy`. A trading party
does not put messages on the network itself, so it always names the platform that does.

Get the direction wrong and you are told which entry point to use instead, before anything is written:

```
Status 205 Approuvée is reported by a trading party, not by a platform: start from
FrCdar.FromSeller(siren) for a collection, FrCdar.FromBuyer(siren) for everything else.
```

For anything these three do not cover, `FrCdar.From(party => …)` and `SentBy(party => …)` take the full
party builder.

## Every status

| Method | Code | Meaning | Needs a reason |
|---|---|---|---|
| `Filed()` | 200 | Déposée — filed on the sender's platform | |
| `IssuedByPlatform()` | 201 | Émise par plateforme | |
| `Received()` | 202 | Reçue par la plateforme | |
| `MadeAvailable()` | 203 | Mise à disposition | |
| `TakenInCharge()` | 204 | Prise en charge | |
| `Approved()` | 205 | Approuvée | |
| `Disputed(code, reason)` | 207 | En litige | yes |
| `Refused(code, reason)` | 210 | Refusée | yes |
| `PaymentSent()` | 211 | Paiement transmis | |
| `Collected(amount)` | 212 | Encaissée | amount and VAT rate |
| `Rejected(code, reason)` | 213 | Rejetée | yes |

Each takes an optional moment; without one, now is used.

A collection says how much was collected, at which rate — the rules require it, once per rate:

```csharp
LifecycleStatusMessage collected = FrCdar
    .FromSeller("100000009", "VENDEUR")
    .SentBy("0003", "PA-E Vendeur")
    .ToPublicPortal()
    .About("F202500003", new DateOnly(2025, 7, 1))
    .Collected(new FrCollectedAmount(12000m, 20m));

// Several rates:
.Collected([new FrCollectedAmount(12000m, 20m), new FrCollectedAmount(500m, 5.5m)]);
```

```csharp
LifecycleStatusMessage refused = FrCdar
    .FromBuyer("200000008", "ACHETEUR")
    .SentBy("0003", "PA-E Acheteur")
    .ToSeller("100000009", "VENDEUR")
    .About("F202500003", new DateOnly(2025, 7, 1))
    .ReceivedAt(new DateTimeOffset(2025, 7, 1, 16, 10, 0, TimeSpan.Zero))
    .Refused(
        FrStatusReason.VatRateWrong,
        "Taux de TVA erroné",
        requestedActionCode: FrRequestedAction.CorrectiveInvoice,
        requestedAction: "Créer une facture rectificative");
```

The reason lands where the DGFiP puts it, inside `ram:SpecifiedDocumentStatus` on the reference — not as a
free-text note somewhere convenient — numbered as the rules require.

### Which reasons a status accepts

Each status accepts its own list, and `FrStatusReason` carries them as named constants. To offer a choice
rather than guess at one:

```csharp
IReadOnlyList<string> reasons = FrStatusReason.AllowedFor(FrLifecycleStatus.Refused);

// The public-sector platform accepts seven more.
IReadOnlyList<string> publicSector =
    FrStatusReason.AllowedFor(FrLifecycleStatus.Refused, publicSector: true);
```

Nothing here refuses a code the published rules would accept: the lists are there to choose from, and the
rules remain the authority.

## Checking a message against the DGFiP rules

The DGFiP artefacts are published in a repository that declares no licence, so they are fetched rather than
shipped:

```bash
build/fetch-specs.sh france
```

Then run them like any other rule set:

```csharp
SchematronRuleSet rules = SchematronRuleSet.Load(
    File.ReadAllText("specs/fr-dse/rules/1.4.0.03/20260804_BR-FR-CDV-Schematron-CDAR_V1.4.0.03.sch"),
    "BR-FR-CDV (CDAR)",
    "1.4.0.03");

ValidationReport report = new SchematronValidator().Validate(xml, rules);
```

Every message this builder produces — eleven statuses, to a partner and to the public portal — is measured
against those rules on each build.

## Which codes are verified, and which are not

Seven statuses carry codes read directly from the DGFiP sample messages. The other four follow the pattern
those establish — platform events use `305`, business events `23` — and say so rather than pretending.

```csharp
FrLifecycleStatus.Filed.IsVerified;     // true — read from a sample
FrLifecycleStatus.Refused.IsVerified;   // false — inferred from the pattern
```

If the specification says otherwise for one of them, correct it without waiting for a release:

```csharp
FrLifecycleStatus corrected = FrLifecycleStatus.Refused.WithCodes(
    acknowledgementTypeCode: "23",
    documentStatusCode: "46");

LifecycleStatusMessage message = FrCdar
    .FromBuyer("200000008", "ACHETEUR")
    .SentBy("0003", "PA-E Acheteur")
    .ToSeller("100000009", "VENDEUR")
    .About("F202500003", new DateOnly(2025, 7, 1))
    .With(corrected);
```

## Reading one that arrives

```csharp
DocumentResult result = einvoicing.Read(stream);

if (result.LifecycleStatus is { } status)
{
    foreach (ReferencedDocumentStatus reference in status.References)
    {
        Console.WriteLine($"{reference.DocumentIdentifier.Value}: " +
                          $"{reference.ProcessConditionCode.Value} {reference.ProcessCondition.Value}");
    }
}
```

A message may report on several documents at once — `CoversMultipleDocuments` says whether it claims to — so
`References` is a list even when it holds one.

## A national profiling this library does not know

The reader is generic on purpose. A profiling restricts the message and gives meaning to its codes without
changing its shape, so an unknown one still parses: the codes come back uninterpreted, and the downgrade is
reported rather than hidden.

```
EIV1042  Warning  UnknownProfile  at … (BT-24)
    expected  a registered profile
    found     urn:acme:lifecycle:2p0
    fallback  generic cdar reading; no profile rules applied
```

You lose the meaning of some codes, not the message. `Profile.AllowsCompleteValidation` is false, so nothing
downstream can mistake it for a fully understood document.

## Going one layer down

The French builder produces an ordinary `LifecycleStatusMessage`; nothing stops you building one by hand for
another country, or reading and writing with the generic reader directly.

```csharp
ParseResult<LifecycleStatusMessage> read = einvoicing.Lifecycle.Read(stream);
string xml = einvoicing.LifecycleWriter.WriteToString(message);
```

## Next

- [Reading a document](reading.md)
- [Writing a document](writing.md)
- [The CDAR standard page](../standards/cdar.md), including where the structure was verified

## Run it

[`samples/International.EInvoicing.Samples/Chapters/FrenchLifecycle.cs`](../../samples/International.EInvoicing.Samples/Chapters/FrenchLifecycle.cs) is this page as code — every status, and the direction that decides who reports it.

```bash
dotnet run --project samples/International.EInvoicing.Samples
```
