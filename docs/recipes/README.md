# Recipes

Step-by-step procedures for the four things contributors actually do. Each recipe lists the files to create,
the abstractions to implement and the tests that make the work complete.

- [add-a-format.md](add-a-format.md) — a new syntax (a new XML dialect)
- [add-a-country.md](add-a-country.md) — a new country package
- [add-a-profile.md](add-a-profile.md) — a CIUS, an extension, or a private profile
- [add-a-rule.md](add-a-rule.md) — a validation rule, or suppressing one

Two of them have a worked example that compiles:
[`Extending.cs`](../../samples/International.EInvoicing.Samples/Chapters/Extending.cs) registers a profile and
a rule from outside the library, both of which then take part in reading and validation exactly as the
shipped ones do.

If a recipe no longer matches the code, the recipe is the bug: fix it in the same pull request.
