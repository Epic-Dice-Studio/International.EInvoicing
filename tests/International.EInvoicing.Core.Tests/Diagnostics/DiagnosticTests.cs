using International.EInvoicing.Diagnostics;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Core.Tests.Diagnostics;

public class DiagnosticTests
{
    [Fact]
    public void Create_FormatsTheMessageFromTheDescriptor()
    {
        Diagnostic diagnostic = Diagnostic.Create(DiagnosticCodes.InvalidValue, "1 234,56", "an amount");

        diagnostic.Message.ShouldBe("The value '1 234,56' could not be read as an amount.");
        diagnostic.Code.ShouldBe("EIV2001");
        diagnostic.Category.ShouldBe(DiagnosticCategory.InvalidValue);
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Create_FormatsAmountsInvariantlyWhateverTheCurrentCulture()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
        try
        {
            Diagnostic diagnostic = Diagnostic.Create(DiagnosticCodes.InvalidValue, 1234.56m, "an amount");

            diagnostic.Message.ShouldContain("1234.56");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Fact]
    public void EveryDescriptor_PointsAtItsCataloguePage()
    {
        DiagnosticCodes.InvalidValue.HelpLink.ShouldEndWith("docs/diagnostics/EIV2001.md");
    }

    [Fact]
    public void WithSeverity_ProducesTheDiagnosticAPolicyWouldRaise()
    {
        Diagnostic raised = Diagnostic
            .Create(DiagnosticCodes.UnsupportedDateFormat, "610")
            .WithSeverity(DiagnosticSeverity.Error);

        raised.Severity.ShouldBe(DiagnosticSeverity.Error);
        raised.Descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Info);
    }

    [Fact]
    public void ToString_SaysWhatHappenedWhereAndWhatWasDoneInstead()
    {
        Diagnostic diagnostic = Diagnostic.Create(DiagnosticCodes.InvalidValue, "29/08/2026", "a date") with
        {
            Location = new SourceLocation("/Invoice/cbc:IssueDate", 4, 9),
            BusinessTerm = "BT-2",
            AppliedFallback = "raw text preserved",
        };

        string text = diagnostic.ToString();

        text.ShouldContain("EIV2001");
        text.ShouldContain("/Invoice/cbc:IssueDate");
        text.ShouldContain("BT-2");
        text.ShouldContain("raw text preserved");
    }

    [Fact]
    public void Create_RejectsANullDescriptor()
        => Should.Throw<ArgumentNullException>(() => Diagnostic.Create(null!));
}
