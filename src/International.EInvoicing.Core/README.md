# International.EInvoicing.Core

Core building blocks shared by every International.EInvoicing package.

- the EN 16931 canonical invoice model,
- the `Field<T>` value system, which keeps the **raw** text and XML attributes of every field alongside the
  typed value,
- the diagnostic engine — readers never throw on untrusted input, they report,
- the format/profile/country registry used to plug in your own readers, writers, profiles and rules,
- hardened XML plumbing (XXE and entity-expansion safe by construction).

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
