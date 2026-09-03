# ZUGFeRD 1.0

| | |
|---|---|
| **Source** | <https://www.ferd-net.de> — carried by <https://github.com/ZUGFeRD/mustangproject> |
| **Version** | 1.0 (2014) |
| **Retrieved** | *(fetched locally; not committed)* |
| **Licence** | FeRD |
| **Redistributable** | no |

The 2013 German hybrid invoice, replaced by ZUGFeRD 2 in 2019 and no longer published by FeRD. It is CII
from before CII settled: the vocabulary is recognisably the same, but the document namespace is FeRD's own
(`urn:ferd:CrossIndustryDocument:invoice:1p0`) and the data types are versions 12 and 15 rather than 100.

`build/fetch-specs.sh zugferd1` fills:

- `schema/` — the XSD
- `schematron/` — `ZUGFeRD_1p0.sch`, one rule set covering all three profiles
- `examples/` — the four reference documents
- `reference/` — mustangproject's own ZUGFeRD 2 conversion of one of them, which is what this library's
  migration is judged against

Nothing here is committed. The tests that need it skip when it is absent, which is also how CI runs.

This library **reads** ZUGFeRD 1.0 and does not write it.
