using International.EInvoicing.Building;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Cli.Tests;

/// <summary>
/// The tool, run without a process.
/// </summary>
/// <remarks>
/// What matters most here is the exit code, because it is the only part a pipeline reads. The three cases
/// have to stay apart: the document is fine, the document is not, and the tool could not tell. A CI job that
/// treats the third as the first passes while checking nothing.
/// </remarks>
public sealed class CliTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("einvoice-cli-tests").FullName;

    [Fact]
    public void ValidatingAConformingInvoiceSucceeds()
    {
        string path = Write("invoice.xml", AnInvoice());

        (int code, string output, _) = Run("validate", path);

        code.ShouldBe(0, output);
        output.ShouldContain("conforming");
        output.ShouldContain("1/1 conforming.");
    }

    /// <summary>What ran is part of the answer, not a footnote.</summary>
    [Fact]
    public void AndSaysWhichRuleSetsCheckedIt()
    {
        string path = Write("invoice.xml", AnInvoice());

        (_, string output, _) = Run("validate", path);

        output.ShouldContain("checked      EN 16931");
    }

    [Fact]
    public void ValidatingAnInvoiceThatBreaksARuleFails()
    {
        // BR-CO-10: the sum of line net amounts must equal BT-106. Wrong on purpose.
        EInvoice invoice = AnInvoice();
        invoice.Totals.LineTotalAmount = 999m;

        (int code, string output, _) = Run("validate", Write("wrong.xml", invoice));

        code.ShouldBe(1);
        output.ShouldContain("rejected");
    }

    [Fact]
    public void PointingItAtNothingIsNotTheSameAsPassing()
    {
        (int code, _, string errors) = Run("validate", Path.Combine(_directory, "absent.xml"));

        code.ShouldBe(2, "a file that is not there is a tool failure, not a rejected document");
        errors.ShouldContain("no file at");
    }

    [Fact]
    public void ADirectoryIsValidatedWhole()
    {
        Write("one.xml", AnInvoice());
        Write("two.xml", AnInvoice());

        (int code, string output, _) = Run("validate", _directory);

        code.ShouldBe(0);
        output.ShouldContain("2/2 conforming.");
    }

    [Fact]
    public void TheJsonReportCarriesTheVerdictAndTheRuleSets()
    {
        string path = Write("invoice.xml", AnInvoice());

        (_, string output, _) = Run("validate", path, "--json");

        using System.Text.Json.JsonDocument json = System.Text.Json.JsonDocument.Parse(output);
        System.Text.Json.JsonElement first = json.RootElement[0];

        first.GetProperty("valid").GetBoolean().ShouldBeTrue();
        first.GetProperty("complete").GetBoolean().ShouldBeTrue();
        first.GetProperty("ruleSets").GetArrayLength().ShouldBeGreaterThan(0);
    }

    [Fact]
    public void InspectSaysWhatTheDocumentIs()
    {
        string path = Write("invoice.xml", AnInvoice());

        (int code, string output, _) = Run("inspect", path);

        code.ShouldBe(0);
        output.ShouldContain("kind         Ubl");
        output.ShouldContain("FA-2026-001");
        output.ShouldContain("resolved     exactly");
    }

    [Fact]
    public void ConvertWritesTheOtherSyntaxAndSaysWhatItCost()
    {
        string path = Write("invoice.xml", AnInvoice());

        (int code, string output, string errors) = Run("convert", path, "--to", "cii");

        code.ShouldBe(0);
        output.ShouldContain("CrossIndustryInvoice");
        errors.ShouldContain("carried everything");
    }

    [Fact]
    public void ConvertWithoutATargetSyntaxRefusesRatherThanGuessing()
    {
        (int code, _, string errors) = Run("convert", Write("invoice.xml", AnInvoice()));

        code.ShouldBe(2);
        errors.ShouldContain("--to");
    }

    [Fact]
    public void ConvertCanWriteToAFile()
    {
        string destination = Path.Combine(_directory, "converted.xml");

        Run("convert", Write("invoice.xml", AnInvoice()), "--to", "cii", "--out", destination);

        File.ReadAllText(destination).ShouldContain("CrossIndustryInvoice");
    }

    [Fact]
    public void RulesListsWhatThisBuildCanJudgeWith()
    {
        (int code, string output, _) = Run("rules");

        code.ShouldBe(0);
        output.ShouldContain("EN 16931");
        output.ShouldContain("XRechnung");
    }

    [Fact]
    public void ProfilesListsWhatItKnows()
    {
        (int code, string output, _) = Run("profiles");

        code.ShouldBe(0);
        output.ShouldContain("urn:cen.eu:en16931:2017");
    }

    [Fact]
    public void NoArgumentsPrintsTheHelpAndFails()
    {
        (int code, string output, _) = Run();

        code.ShouldBe(2);
        output.ShouldContain("einvoice validate");
    }

    [Fact]
    public void AskingForHelpSucceeds()
    {
        (int code, string output, _) = Run("--help");

        code.ShouldBe(0);
        output.ShouldContain("Exit codes");
    }

    [Fact]
    public void AnUnknownCommandFails()
    {
        (int code, _, string errors) = Run("frobnicate");

        code.ShouldBe(2);
        errors.ShouldContain("is not a command");
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private static (int Code, string Output, string Errors) Run(params string[] arguments)
    {
        using var output = new StringWriter();
        using var errors = new StringWriter();

        int code = Cli.Run(arguments, output, errors);

        return (code, output.ToString(), errors.ToString());
    }

    private string Write(string name, EInvoice invoice)
    {
        string path = Path.Combine(_directory, name);
        File.WriteAllText(path, EInvoicing.CreateDefault().Write(invoice, DocumentFormat.Ubl));
        return path;
    }

    /// <summary>An invoice EN 16931 accepts, so a failing test means the tool, not the fixture.</summary>
    private static EInvoice AnInvoice() => EInvoiceBuilder
        .Create(KnownProfiles.En16931Ubl)
        .WithNumber("FA-2026-001")
        .OfType(InvoiceTypeCodes.CommercialInvoice)
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .InCurrency("EUR")
        .WithBuyerReference("SERVICE-COMPTA")
        .From(seller => seller
            .Named("Fournisseur SARL")
            .WithVatIdentifier("FR32732829320")
            .WithAddress(address =>
            {
                address.Line1 = "12 rue de la Paix";
                address.City = "Paris";
                address.PostCode = "75002";
                address.CountryCode = "FR";
            }))
        .To(buyer => buyer
            .Named("Client SA")
            .WithVatIdentifier("FR89552081317")
            .WithAddress(address =>
            {
                address.Line1 = "3 avenue des Champs";
                address.City = "Lyon";
                address.PostCode = "69002";
                address.CountryCode = "FR";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Conseil")
            .WithQuantity(1m, "DAY")
            .WithNetPrice(450m)
            .WithNetAmount(450m)
            .WithVat("S", 20m))
        .WithComputedVatBreakdown()
        .WithComputedTotals()
        .Build();
}
