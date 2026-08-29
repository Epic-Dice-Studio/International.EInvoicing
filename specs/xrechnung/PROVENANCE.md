# XRechnung Schematron and test suite

| | |
|---|---|
| **Source** | <https://github.com/itplr-kosit/xrechnung-schematron>, <https://github.com/itplr-kosit/xrechnung-testsuite> |
| **Version** | 3.x (track the KoSIT releases) |
| **Retrieved** | *(not yet fetched)* |
| **Licence** | Apache License 2.0 |
| **Redistributable** | yes |

Expected content: the XRechnung CIUS and Extension Schematron for UBL and CII, plus the official test suite.

The KoSIT validator (<https://github.com/itplr-kosit/validator>) and its configuration
(<https://github.com/itplr-kosit/validator-configuration-xrechnung>) are the reference implementation our CI
cross-checks against. It is a Java tool: CI runs it, the library never depends on it.
