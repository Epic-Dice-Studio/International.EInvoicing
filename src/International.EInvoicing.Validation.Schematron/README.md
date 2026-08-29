# International.EInvoicing.Validation.Schematron

Runs the official Schematron rule sets — EN 16931, Peppol, XRechnung — against a document.

The artefacts are executed **as data**, not translated into generated code. That is what keeps a rule set
correct when its publisher revises it: you drop in the new file and the rules change with it.

It carries its own evaluator for the XPath subset those artefacts use, with exact decimal arithmetic, because
the rules that compare invoice totals are the ones an approximate engine gets wrong.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
