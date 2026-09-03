using International.EInvoicing.Model;
using International.EInvoicing.OrderX.Writing;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Xsd;
using Shouldly;
using Xunit;

namespace International.EInvoicing.OrderX.Tests;

/// <summary>
/// What Order-X's own rules say about the document this library writes.
/// </summary>
/// <remarks>
/// FNFE-MPE publishes these as source Schematron, one rule set per profile, so they run through the same
/// engine as everything else rather than through the compiled-XSLT reader. They are fetched, so these skip
/// when the folder is empty.
/// </remarks>
public class RuleConformanceTests
{
    [Fact]
    public void TheReferenceOrderSatisfiesTheProfileRulesItDeclares()
    {
        (EInvoicing Library, string Document) fixture = Fixture();

        ValidationReport report = fixture.Library.Validate(fixture.Document);

        report.Errors.ShouldBeEmpty(Explain(report));
    }

    /// <summary>And so does the document this library writes from it.</summary>
    [Fact]
    public void AndSoDoesWhatWeWriteBackFromIt()
    {
        (EInvoicing Library, string Document) fixture = Fixture();

        Order order = fixture.Library.Read(fixture.Document).RequireOrder();
        ValidationReport report = fixture.Library.Validate(new OrderXOrderWriter().WriteToString(order));

        report.Errors.ShouldBeEmpty(Explain(report));
    }

    /// <summary>
    /// And the rules were rules: a rule set whose contexts match nothing reports a clean document.
    /// </summary>
    [Fact]
    public void AndTheRulesActuallyRan()
    {
        (EInvoicing Library, string Document) fixture = Fixture();

        ValidationReport report = fixture.Library.Validate(fixture.Document);

        report.RuleSets.ShouldContain(ruleSet => ruleSet.Ran, Explain(report));
        report.NotRun.ShouldBeEmpty(Explain(report));
    }

    /// <summary>
    /// And a document missing something the rules require is refused. The seller's name is BT-27's
    /// equivalent here, and no Order-X profile lets an order go without one.
    /// </summary>
    [Fact]
    public void AndAnOrderWithNoSellerNameIsRefused()
    {
        (EInvoicing Library, string Document) fixture = Fixture();

        Order order = fixture.Library.Read(fixture.Document).RequireOrder();
        order.Seller!.Name = default;

        ValidationReport report = fixture.Library.Validate(new OrderXOrderWriter().WriteToString(order));

        report.Errors.ShouldNotBeEmpty("an order with no seller name satisfies no Order-X profile");
    }

    private static (EInvoicing Library, string Document) Fixture()
    {
        string? path = OrderXCorpus.Find(OrderXCorpus.ReferenceOrder);
        string rules = Path.Combine(OrderXCorpus.Root, "schematron");
        string schemas = Path.Combine(OrderXCorpus.Root, "schema");

        Assert.SkipWhen(
            path is null || !Directory.Exists(rules) || !Directory.Exists(schemas),
            "run build/fetch-specs.sh order-x");

        EInvoicing library = EInvoicing.Create(builder => builder
            .AddDefaults()
            .AddOrderX()
            .AddOrderXSchemaFrom(schemas)
            .AddOrderXRulesFrom(rules));

        return (library, File.ReadAllText(path!));
    }

    private static string Explain(ValidationReport report) =>
        string.Join(Environment.NewLine, report.Errors.Select(error => error.ToString()))
        + Environment.NewLine
        + string.Join(Environment.NewLine, report.RuleSets.Select(ruleSet => ruleSet.ToString()));
}
