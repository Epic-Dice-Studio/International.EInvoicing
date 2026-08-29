# Prior art, and how to mine it

Mature e-invoicing libraries have already met the documents that break implementations. Their test corpora
and, above all, their issue trackers are a map of the edge cases this library has not hit yet — cheaper to
read than to rediscover in production.

This page is the standing task: **before declaring a format or a country done, spend an hour in the trackers
below.** Record what you find here, as a row, whether or not it turns into work.

## Where to look

| Ecosystem | Project | Worth mining for |
|---|---|---|
| Java | [ZUGFeRD/mustangproject](https://github.com/ZUGFeRD/mustangproject) | The reference for Factur-X and ZUGFeRD. Its issues are a decade of real-world CII edge cases: profile quirks, PDF containers, rounding disputes. |
| Java | [phax/phive-rules](https://github.com/phax/phive-rules) | Which rule set version applies to which document, across many countries. Already used here to verify the CDAR structure. |
| Java | [phax/ph-schematron](https://github.com/phax/ph-schematron) | How a mature Schematron engine handles the awkward parts. Direct comparison for our XPath engine. |
| Java | [itplr-kosit/validator](https://github.com/itplr-kosit/validator) | The German reference validator. Its issues show where implementations and the norm disagree. |
| PHP | [horstoeko/zugferd](https://github.com/horstoeko/zugferd) | Profile-driven builder ergonomics, and a long tail of reported document oddities. |
| Python | [akretion/factur-x](https://github.com/akretion/factur-x), [pretix/drafthorse](https://github.com/pretix/drafthorse) | Compact implementations; their issues surface PDF and encoding problems. |
| .NET | [stephanstapel/ZUGFeRD-csharp](https://github.com/stephanstapel/ZUGFeRD-csharp) | The closest neighbour. What its users ask for is what ours will ask for. |
| .NET | [kestlerio/ZUGFeRD-csharp-extended](https://github.com/kestlerio/ZUGFeRD-csharp-extended) | A fork, so its diff shows what the original was missing. |

## What to look for

1. **Documents that broke them.** Attachments on issues are real invoices from real senders, which is exactly
   the corpus no specification provides. Check the licence before committing anything.
2. **Rules everyone gets wrong.** A rule discussed repeatedly across projects is one where the norm is
   ambiguous, not where implementers are careless. Those deserve a test and a note in the standards page.
3. **Features requested repeatedly.** A gap several communities ask for is a gap ours will have too.
4. **Where they disagree with the reference validator.** Those threads usually end with the correct reading
   of the norm, argued out by people who had to ship.
5. **What they refuse to do.** Scope boundaries other projects settled on are evidence about ours.

## Findings

Add a row when you mine something. An entry that led to no change is still worth recording — it says the
ground was covered.

| Date | Source | Finding | What we did |
|---|---|---|---|
| 2026-08-30 | phax/phive-rules | Carries the French `BR-FR-CDV` Schematron and the DGFiP lifecycle test files, which the DGFiP does not publish in a redistributable form. | Verified the CDAR structure and the French status codes against them. Nothing redistributed; see `docs/standards/cdar.md`. |
