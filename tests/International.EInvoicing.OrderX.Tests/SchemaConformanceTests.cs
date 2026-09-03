using International.EInvoicing.Model;
using International.EInvoicing.OrderX.Writing;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Xsd;
using Shouldly;
using Xunit;

namespace International.EInvoicing.OrderX.Tests;

/// <summary>
/// What the publisher's own schema says about the document this library writes.
/// </summary>
/// <remarks>
/// The strongest judge available for Order-X, and the one that reads what no business rule reads: element
/// order and cardinality. The schemas are fetched rather than shipped, so these skip when the folder is
/// empty — which is also how CI runs, and why the round-trip tests do not rely on them.
/// </remarks>
public class SchemaConformanceTests
{
    [Fact]
    public void ThePublishedReferenceOrderSatisfiesItsOwnSchema()
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
        string written = new OrderXOrderWriter().WriteToString(order);

        ValidationReport report = fixture.Library.Validate(written);

        report.Errors.ShouldBeEmpty(Explain(report));
    }

    /// <summary>
    /// And the check is a check: a schema with no declaration for the root approves everything, which is how
    /// a validator that never ran reads exactly like one that found nothing wrong.
    /// </summary>
    [Fact]
    public void AndTheSchemaIsActuallyJudgingTheDocument()
    {
        (EInvoicing Library, string Document) fixture = Fixture();

        // ram:ID is the first child of rsm:ExchangedDocument and the schema says so.
        string broken = fixture.Document.Replace(
            "<ram:ID>PO123456789</ram:ID>",
            "<ram:Nonsense>PO123456789</ram:Nonsense>",
            StringComparison.Ordinal);

        ValidationReport report = fixture.Library.Validate(broken);

        report.Errors.ShouldNotBeEmpty("the schema must reject an element it does not declare");
        report.RuleSets.ShouldContain(ruleSet => ruleSet.Ran);
    }

    private static (EInvoicing Library, string Document) Fixture()
    {
        string? path = OrderXCorpus.Find(OrderXCorpus.ReferenceOrder);
        string schemas = Path.Combine(OrderXCorpus.Root, "schema");

        Assert.SkipWhen(path is null || !Directory.Exists(schemas), "run build/fetch-specs.sh order-x");

        EInvoicing library = EInvoicing.Create(builder => builder
            .AddDefaults()
            .AddOrderX()
            .AddOrderXSchemaFrom(schemas));

        return (library, File.ReadAllText(path!));
    }

    private static string Explain(ValidationReport report) =>
        string.Join(Environment.NewLine, report.Errors.Select(error => error.ToString()))
        + Environment.NewLine
        + string.Join(Environment.NewLine, report.RuleSets.Select(ruleSet => ruleSet.ToString()));
}
