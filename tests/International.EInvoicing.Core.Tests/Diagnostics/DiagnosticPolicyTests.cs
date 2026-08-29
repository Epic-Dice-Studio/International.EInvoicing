using International.EInvoicing.Diagnostics;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Core.Tests.Diagnostics;

public class DiagnosticPolicyTests
{
    /// A descriptor local to these tests: the preset semantics are exercised without adding a code to the
    /// shipped catalogue before a reader actually emits it.
    private static readonly DiagnosticDescriptor Unmapped = new(
        "EIV2020",
        DiagnosticCategory.UnmappedElement,
        DiagnosticSeverity.Info,
        "Element kept as extension data.");

    private static Diagnostic InvalidValue() =>
        Diagnostic.Create(DiagnosticCodes.InvalidValue, "29/08/2026", "a date");

    private static Diagnostic UnsupportedDateFormat() =>
        Diagnostic.Create(DiagnosticCodes.UnsupportedDateFormat, "610");

    [Fact]
    public void Balanced_ReportsWhatTheDescriptorDeclares()
    {
        Diagnostic? applied = DiagnosticPolicy.Balanced.Apply(InvalidValue());

        applied!.Severity.ShouldBe(DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Strict_MakesAnythingNotFullyInterpretedFatal()
    {
        Diagnostic? applied = DiagnosticPolicy.Strict.Apply(InvalidValue());

        applied!.Severity.ShouldBe(DiagnosticSeverity.Fatal);
    }

    [Fact]
    public void Lenient_DropsWhatACoreOnlyReaderCannotActOn()
    {
        Diagnostic unmapped = Diagnostic.Create(Unmapped);

        DiagnosticPolicy.Lenient.Apply(unmapped).ShouldBeNull();
        DiagnosticPolicy.Balanced.Apply(unmapped).ShouldNotBeNull();
    }

    [Fact]
    public void Lenient_StillReportsWhatTheCallerMustSee()
    {
        DiagnosticPolicy.Lenient.Apply(InvalidValue())!.Severity.ShouldBe(DiagnosticSeverity.Warning);
    }

    [Fact]
    public void ACodeRule_BeatsACategoryRule()
    {
        DiagnosticPolicy policy = DiagnosticPolicy.Create(o => o
            .OnCategory(DiagnosticCategory.InvalidValue, DiagnosticAction.Fail)
            .OnCode("EIV2001", DiagnosticAction.Suppress));

        policy.Apply(InvalidValue()).ShouldBeNull();
    }

    [Fact]
    public void APredicate_BeatsEverythingElse()
    {
        DiagnosticPolicy policy = DiagnosticPolicy.Create(o => o
            .OnCode("EIV2001", DiagnosticAction.Suppress)
            .OnDiagnostic(d => d.BusinessTerm == "BT-1" ? DiagnosticAction.Fail : null));

        Diagnostic critical = InvalidValue() with { BusinessTerm = "BT-1" };

        policy.Apply(critical)!.Severity.ShouldBe(DiagnosticSeverity.Fatal);
        policy.Apply(InvalidValue()).ShouldBeNull();
    }

    [Fact]
    public void APredicateReturningNull_DefersToTheRemainingRules()
    {
        DiagnosticPolicy policy = DiagnosticPolicy.Create(o => o
            .OnDiagnostic(_ => null)
            .OnCode("EIV2001", DiagnosticAction.Escalate));

        policy.Apply(InvalidValue())!.Severity.ShouldBe(DiagnosticSeverity.Error);
    }

    [Fact]
    public void Escalate_NeverLowersAnAlreadyMoreSevereDiagnostic()
    {
        DiagnosticPolicy policy = DiagnosticPolicy.Create(o => o.OnCode("EIV2001", DiagnosticAction.Escalate));
        Diagnostic alreadyFatal = InvalidValue().WithSeverity(DiagnosticSeverity.Fatal);

        policy.Apply(alreadyFatal)!.Severity.ShouldBe(DiagnosticSeverity.Fatal);
    }

    [Fact]
    public void Escalate_RaisesAnInformationalDiagnosticToError()
    {
        DiagnosticPolicy policy = DiagnosticPolicy.Create(o =>
            o.OnCode("EIV2002", DiagnosticAction.Escalate));

        policy.Apply(UnsupportedDateFormat())!.Severity.ShouldBe(DiagnosticSeverity.Error);
    }

    [Fact]
    public void Create_RejectsANullConfiguration()
        => Should.Throw<ArgumentNullException>(() => DiagnosticPolicy.Create(null!));
}
