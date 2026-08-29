# UN/CEFACT Cross Domain Acknowledgement and Response

| | |
|---|---|
| **Source** | <https://unece.org/trade/uncefact/xml-schemas> |
| **Version** | generic CDAR schema module |
| **Retrieved** | *(not yet fetched)* |
| **Licence** | UN/CEFACT — free to use and redistribute |
| **Redistributable** | yes |

The generic CDAR schema is the fallback target when a lifecycle message declares a profile we do not
support: the message is still parsed, and a `UnknownProfile` diagnostic records the downgrade.

The French profiling of CDAR is described in the DGFiP external specifications (see `../fr-dse`).
