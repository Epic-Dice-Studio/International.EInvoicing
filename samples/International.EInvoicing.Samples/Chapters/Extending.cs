using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Samples.Chapters;

/// <summary>
/// Adding what the library has not shipped, from your own code.
/// </summary>
/// <remarks>
/// A profile, a rule, a field the norm has no place for: none of them should need a pull request or a
/// release. Everything here is registered from outside the library and wins over what it ships.
/// </remarks>
internal static class Extending
{
    private const string AcmeNamespace = "urn:acme:invoice:1p0";

    private static readonly Profile AcmeProfile = new(
        new ProfileIdentifier("urn:cen.eu:en16931:2017#compliant#urn:acme:invoice:1p0"),
        "Acme purchasing profile",
        DocumentSyntax.Ubl,
        KnownProfiles.En16931Ubl.Id);

    public static void Run()
    {
        Report.Chapter("Extending it without forking it");

        EInvoicing einvoicing = EInvoicing.Create(library => library
            .AddDefaults()
            .AddProfile(AcmeProfile)                 // a profile of our own
            .AddRules(new NoInvoicesOnSunday()));    // a rule of our own, written in C#

        // The invoice from chapter 3, wearing our profile and dated on a Sunday.
        EInvoice invoice = Invoices.Build(announce: false);
        invoice.SpecificationIdentifier = AcmeProfile.Id;
        invoice.IssueDate = new DateOnly(2026, 9, 6);
        invoice.Extensions.Add(new ExtensionElement(
            AcmeNamespace,
            "Approval",
            $"<acme:Approval xmlns:acme=\"{AcmeNamespace}\">signed off by finance</acme:Approval>"));

        string xml = einvoicing.Write(invoice);
        DocumentResult read = einvoicing.Read(xml);

        Report.Fact("our profile resolves exactly", read.Profile?.IsExact);

        foreach (ExtensionElement element in read.RequireInvoice().Extensions)
        {
            Report.Fact("our element survived the round trip", element.QualifiedName);
        }

        ValidationReport report = einvoicing.Validate(xml);

        Report.Fact("rule sets that ran", report.RuleSets.Count(outcome => outcome.Ran));

        foreach (ValidationMessage message in report.Messages)
        {
            Report.Note($"{message.RuleIdentifier}: {message.Message}");
        }

        Report.Say("The profile, the rule and the extra element all came from outside the library.");
    }

    /// <summary>
    /// A rule that is not worth writing in Schematron. Registered like any other, and reported like any
    /// other — including in the coverage block, so nobody has to wonder whether it ran.
    /// </summary>
    private sealed class NoInvoicesOnSunday : IDocumentRuleSet
    {
        public string Name => "Acme house rules";

        public string Version => "2026-09";

        public bool AppliesTo(DocumentSyntax syntax, ProfileIdentifier specification) =>
            specification == AcmeProfile.Id;

        public ValidationReport Validate(string document)
        {
            DateOnly? issued = EInvoicing.CreateDefault().Read(document).Invoice?.IssueDate.Value;
            bool onSunday = issued?.DayOfWeek == DayOfWeek.Sunday;

            return new ValidationReport(
                onSunday
                    ? [new ValidationMessage("ACME-1", RuleSeverity.Error, "We do not invoice on Sundays.")]
                    : [],
                [new RuleSetOutcome(Name, Version, Ran: true)]);
        }
    }
}
