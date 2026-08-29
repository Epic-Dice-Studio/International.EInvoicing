# Recipe — add a profile

A *profile* is what `BT-24` declares: a CIUS, an extension, or your own private agreement with a partner.
Profiles are the main extension point of this library, and adding one from outside the library is a supported,
documented scenario — not a hack.

## From your own code

```csharp
services.AddEInvoicing(o => o
    .AddCii()
    .AddProfile(new MyCustomProfile("urn:my-company:profile:1p0")));
```

`IProfile` states: the identifier, the syntax it applies to, the rule sets that apply, and the parent profile
it restricts. The parent matters — it is what the resolver falls back to when something is off.

## Inside the library

Register it in the package that owns it: a national CIUS in `Countries.<Country>`, a cross-border one in the
format package.

## The fallback chain

When a document declares an identifier, the resolver walks:

1. the exact profile,
2. its parent CIUS,
3. the base EN 16931 profile for that syntax,
4. generic syntax reading.

Every step it takes past the first produces a diagnostic naming what was expected and what was used. Never
short-circuit this: a document parsed with a fallback and a document parsed with its real profile must be
distinguishable by the caller.

## Prove it

- A golden document per profile.
- A test that an unknown profile identifier lands on the expected fallback **and** emits `UnknownProfile`.
- A test that a supported-but-unimplemented profile marks `ValidationReport.IsComplete` false.
