# Lifecycle statuses

What happened to an invoice after it was sent — filed, received, approved, disputed, refused, paid. In France
these are the *statuts de cycle de vie* the 2026 reform makes mandatory between platforms, carried as
UN/CEFACT CDAR messages.

## Name the status, not its codes

A status is not one code but three: the status itself, the type code of the acknowledgement carrying it, and
the referenced document's status code. Getting the other two wrong produces a message that names the right
status and is rejected anyway — so nothing here asks you for them.

```csharp
using International.EInvoicing.Countries.France.Lifecycle;

LifecycleStatusMessage approved = FrCdar
    .ToPartner(to => to
        .Company("100000009")                    // the partner's SIREN
        .Named("VENDEUR")
        .AsSeller()
        .ReachableAt("100000009_STATUTS"))       // where its statuses are delivered
    .From(from => from.Platform("0003", "PA-E Vendeur"))
    .About("F202500003", new DateOnly(2025, 7, 1))
    .Approved();

string xml = EInvoicing.CreateDefault().Write(approved);
```

## Where you are sending changes the profile

Sending a status to a trading partner and reporting one to the public portal are **two different profiles**,
not two variants of one: different context, different addressing, an extra element inside the reference. They
are therefore two entry points.

```csharp
// To a partner, through approved platforms. The public portal is addressed as well, which this profile
// expects: urn.cpro.gouv.fr:1p0:CDV:invoice
FrCdar.ToPartner(to => to.Company("100000009").AsSeller().ReachableAt("100000009_STATUTS"))

// Reported to the public portal: urn.cpro.gouv.fr:1p0:CDV:einvoicingF2
FrCdar.ToPublicPortal()
```

Everything after that is the same.

```csharp
LifecycleStatusMessage reported = FrCdar.ToPublicPortal()
    .From(from => from.Platform("0003", "PA-E Vendeur"))
    .About("F202500003", new DateOnly(2025, 7, 1))
    .Filed(new DateTimeOffset(2025, 7, 1, 15, 10, 0, TimeSpan.Zero));
```

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
| `Collected()` | 212 | Encaissée | |
| `Rejected(code, reason)` | 213 | Rejetée | yes |

Each takes an optional moment; without one, now is used.

```csharp
LifecycleStatusMessage refused = FrCdar
    .ToPartner(to => to.Company("100000009").AsSeller())
    .From(from => from.Platform("0003", "PA-E Vendeur"))
    .About("F202500003", new DateOnly(2025, 7, 1))
    .ReceivedAt(new DateTimeOffset(2025, 7, 1, 16, 10, 0, TimeSpan.Zero))
    .Refused("TX_TVA_ERR", "Taux de TVA erroné");
```

The reason lands where the DGFiP puts it, inside `ram:SpecifiedDocumentStatus` on the reference — not as a
free-text note somewhere convenient.

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
    .ToPartner(to => to.Company("100000009").AsSeller())
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
