using International.EInvoicing.Building;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Tests;

/// <summary>
/// An invoice written against the 2026 edition of EN 16931, read by a library that implements the 2017 one.
/// </summary>
/// <remarks>
/// This is the situation the whole ecosystem is about to be in for several years, and it is where the three
/// promises have to hold at once: the document still parses, nothing in it is lost, and the caller is told
/// plainly that they are not reading everything it says.
/// </remarks>
public class En16931EditionReadingTests
{
    private static readonly EInvoicing Library = EInvoicing.CreateDefault();

    [Fact]
    public void ItStillParsesAndSaysWhichEditionItCouldNotRead()
    {
        DocumentResult result = Library.Read(A2026Invoice());

        result.TryGetInvoice(out EInvoice? invoice).ShouldBeTrue();
        invoice.Number.Value.ShouldBe("FA-2026-001");
        invoice.Totals.DuePayableAmount.Value.ShouldBe(1200m);

        Diagnostic edition = result.Diagnostics
            .Where(diagnostic => diagnostic.Code == "EIV1044")
            .ShouldHaveSingleItem();
        edition.Severity.ShouldBe(DiagnosticSeverity.Error);
        edition.Found.ShouldBe("EN 16931-1:2026");
    }

    /// <summary>Whatever the newer edition added that we have no field for is kept, not dropped.</summary>
    [Fact]
    public void AndKeepsWhatTheNewerEditionAddedThatWeHaveNoFieldFor()
    {
        DocumentResult result = Library.Read(A2026Invoice());

        result.RequireInvoice().Extensions.ShouldNotBeEmpty();
    }

    /// <summary>
    /// The rules that ran are the 2017 ones. Reporting a pass without saying that would be the dishonest
    /// version of this.
    /// </summary>
    [Fact]
    public void ValidationNamesTheEditionItsRulesAreFor()
    {
        ValidationReport report = Library.Validate(A2026Invoice());

        report.RuleSets.ShouldContain(set => set.Name.Contains("2017", StringComparison.Ordinal));
    }

    private static string A2026Invoice()
    {
        string xml = Library.Write(EInvoiceBuilder
            .Create(KnownProfiles.En16931Ubl)
            .WithNumber("FA-2026-001")
            .IssuedOn(new DateOnly(2026, 9, 1))
            .InCurrency("EUR")
            .From("Fournisseur SARL", "FR32732829320")
            .To("Client SA", "FR89552081317")
            .AddLine(line => line.WithItem("Conseil").WithNetAmount(1000m).WithVat("S", 20m))
            .WithComputedVatBreakdown()
            .WithComputedTotals()
            .Build());

        return xml
            .Replace(
                "urn:cen.eu:en16931:2017",
                "urn:cen.eu:en16931:2026",
                StringComparison.Ordinal)
            .Replace(
                "<cbc:ID>FA-2026-001</cbc:ID>",
                "<cbc:ID>FA-2026-001</cbc:ID><cbc:SomethingTheRevisionAdded>X</cbc:SomethingTheRevisionAdded>",
                StringComparison.Ordinal);
    }
}
