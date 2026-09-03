# KoSIT validator

| | |
|---|---|
| **Source** | <https://github.com/itplr-kosit/validator>, <https://github.com/itplr-kosit/validator-configuration-xrechnung> |
| **Version** | validator 1.6.3, XRechnung configuration 3.0.2 (2026-08-31) |
| **Retrieved** | *(fetched locally; not committed)* |
| **Licence** | Apache-2.0 (validator); the configuration carries KoSIT's own terms |
| **Redistributable** | not from here — it is a 10 MB binary of somebody else's software |

The reference implementation German authorities actually run. It is here for one reason: every other check in
this repository compares this library against expected *results*, and a rule that this library and a corpus
author read the same wrong way passes all of them. Comparing against another engine is the only thing that
sees it.

`build/fetch-specs.sh kosit` fills:

- `validator-<version>-standalone.jar`
- `configuration/` — `scenarios.xml` and the XRechnung resources it names

Running it needs a JVM. The cross-check tests skip when Java, the jar or the configuration is absent, so a
build without any of them is quiet rather than falsely green.

**What it found on the first run:** this library was validating every CII document against the D22B schemas,
where EN 16931's CII syntax binding — and XRechnung, and Factur-X — name **D16B**. The two revisions share
their namespaces, so the wrong schema attached silently and rejected values the right one allows.
