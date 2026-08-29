using International.EInvoicing.Diagnostics;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Core.Tests.Diagnostics;

public class ParseResultTests
{
    private sealed record Document(string Number);

    [Fact]
    public void ACollectorAppliesThePolicyAsDiagnosticsArrive()
    {
        var collector = new DiagnosticCollector(
            DiagnosticPolicy.Create(o => o.OnCode("EIV2002", DiagnosticAction.Suppress)));

        collector.Add(DiagnosticCodes.InvalidValue, "x", "a date");
        collector.Add(DiagnosticCodes.UnsupportedDateFormat, "610");

        collector.Diagnostics.Count.ShouldBe(1);
        collector.Diagnostics[0].Code.ShouldBe("EIV2001");
    }

    [Fact]
    public void ADocumentWithWarningsIsStillUsable()
    {
        var collector = new DiagnosticCollector();
        collector.Add(DiagnosticCodes.InvalidValue, "x", "a date");

        ParseResult<Document> result = collector.ToResult(new Document("FA-1"));

        result.IsUsable.ShouldBeTrue();
        result.HasErrors.ShouldBeFalse();
        result.Value!.Number.ShouldBe("FA-1");
    }

    [Fact]
    public void APolicyThatFailsMakesTheResultUnusable()
    {
        var collector = new DiagnosticCollector(DiagnosticPolicy.Strict);
        collector.Add(DiagnosticCodes.InvalidValue, "x", "a date");

        ParseResult<Document> result = collector.ToResult(new Document("FA-1"));

        result.IsUsable.ShouldBeFalse();
        result.HasErrors.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    public void OfAtLeast_SelectsBySeverity()
    {
        var collector = new DiagnosticCollector();
        collector.Add(DiagnosticCodes.UnsupportedDateFormat, "610");
        collector.Add(DiagnosticCodes.InvalidValue, "x", "a date");

        ParseResult<Document> result = collector.ToResult(new Document("FA-1"));

        result.OfAtLeast(DiagnosticSeverity.Warning).Count().ShouldBe(1);
        result.OfAtLeast(DiagnosticSeverity.Info).Count().ShouldBe(2);
    }

    [Fact]
    public void SuccessCarriesNoDiagnostics()
    {
        ParseResult<Document> result = ParseResult.Success(new Document("FA-1"));

        result.IsUsable.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void FailedCarriesNoDocument()
    {
        ParseResult<Document> result = ParseResult.Failed<Document>(
            [Diagnostic.Create(DiagnosticCodes.InvalidValue, "x", "a date").WithSeverity(DiagnosticSeverity.Fatal)]);

        result.IsUsable.ShouldBeFalse();
        result.ValueOr(new Document("fallback"))!.Number.ShouldBe("fallback");
    }
}
