using International.EInvoicing.Building;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl;
using International.EInvoicing.Ubl.Writing;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.Xsd.Tests;

/// <summary>
/// What the schema catches that no business rule does.
/// </summary>
/// <remarks>
/// Element order and cardinality are normative in UBL and no Schematron rule looks at either. This library
/// shipped a document with two <c>cac:PayeeFinancialAccount</c> in one <c>cac:PaymentMeans</c> — a shape UBL
/// does not allow — and all 955 EN 16931 assertions plus Peppol's said it was fine. The test below is that
/// document, and this rule set is what would have caught it.
/// </remarks>
public class UblSchemaTests
{
    private static readonly UblSchemaRuleSet Schema = new();

    [Fact]
    public void TheSchemasLoadAndJudgeUblOnly()
    {
        Schema.AppliesTo(DocumentSyntax.Ubl, KnownProfiles.En16931Ubl.Id).ShouldBeTrue();
        Schema.AppliesTo(DocumentSyntax.Cii, KnownProfiles.En16931Cii.Id).ShouldBeFalse();
        Schema.Version.ShouldBe("2.1");
    }

    [Fact]
    public void AnInvoiceThisLibraryWritesIsSchemaValid()
    {
        ValidationReport report = Schema.Validate(new UblInvoiceWriter().WriteToString(AnInvoice()));

        report.IsValid.ShouldBeTrue(Describe(report));
        report.RuleSets[0].Ran.ShouldBeTrue();
    }

    /// <summary>
    /// The defect this library had until today, put back in one string.
    /// </summary>
    /// <remarks>
    /// Two accounts in one payment-means block. Every rule set this library runs accepts it; the schema does
    /// not, because <c>cac:PayeeFinancialAccount</c> may appear once.
    /// </remarks>
    [Fact]
    public void AndTheShapeNoRuleSetCaughtIsCaughtHere()
    {
        string valid = new UblInvoiceWriter().WriteToString(AnInvoice());

        string broken = valid.Replace(
            "</cac:PayeeFinancialAccount>",
            "</cac:PayeeFinancialAccount><cac:PayeeFinancialAccount>"
            + "<cbc:ID>DE02120300000000202051</cbc:ID></cac:PayeeFinancialAccount>",
            StringComparison.Ordinal);

        ValidationReport report = Schema.Validate(broken);

        report.IsValid.ShouldBeFalse();
        report.Errors.ShouldContain(message => message.RuleIdentifier == "XSD-SEQUENCE", Describe(report));
        report.Errors.First().Location.ShouldNotBeNull();
    }

    [Fact]
    public void AnElementOutOfOrderIsRefused()
    {
        string valid = new UblInvoiceWriter().WriteToString(AnInvoice());

        // BT-2 before BT-1, which every reader tolerates and the schema does not.
        string broken = valid
            .Replace("<cbc:ID>2026-0001</cbc:ID>", string.Empty, StringComparison.Ordinal)
            .Replace("<cbc:DueDate>", "<cbc:ID>2026-0001</cbc:ID><cbc:DueDate>", StringComparison.Ordinal);

        Schema.Validate(broken).IsValid.ShouldBeFalse();
    }

    /// <summary>A document that is not XML has no shape to judge, and the report says so rather than passing.</summary>
    [Fact]
    public void SomethingThatIsNotXmlIsNotJudged()
    {
        ValidationReport report = Schema.Validate("this is not a document");

        report.IsValid.ShouldBeFalse();
        report.IsComplete.ShouldBeFalse();
        report.RuleSets[0].Ran.ShouldBeFalse();
    }

    /// <summary>Registered on the library, it runs beside the business rules rather than instead of them.</summary>
    [Fact]
    public void ThroughTheLibraryItIsOneRuleSetAmongTheOthers()
    {
        EInvoicing library = EInvoicing.Create(builder => builder.AddDefaults().AddUblSchema());

        ValidationReport report = library.Validate(library.Write(AnInvoice(), DocumentFormat.Ubl));

        report.RuleSets.ShouldContain(outcome => outcome.Name.Contains("schema", StringComparison.Ordinal));
        report.IsValid.ShouldBeTrue(Describe(report));
    }

    /// <summary>
    /// Every official example, read and written back, comes back in a shape UBL allows — and carrying
    /// nothing this library failed to understand.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The widest net available for the reader and the writer together: documents nobody here wrote, through
    /// both, judged by the schema. It began as six failures out of seventeen, all of them the same story —
    /// terms the reader did not map were kept verbatim and written back at the end of their node, where UBL
    /// does not allow them. The terms are now mapped, so there is nothing left to misplace.
    /// </para>
    /// <para>
    /// The second assertion is the one that matters most: <b>no unmapped element at all</b>. A term this
    /// library does not understand is a field a caller reads as empty, and that is worth failing over well
    /// before it becomes a shape defect.
    /// </para>
    /// </remarks>
    [Theory]
    [MemberData(nameof(OfficialExamples))]
    public void EveryOfficialExampleSurvivesTheRoundTripWithItsShapeIntact(string path)
    {
        EInvoicing library = EInvoicing.Create(builder => builder.AddDefaults());

        DocumentResult read = library.Read(File.ReadAllText(path));

        Assert.SkipWhen(read.Invoice is null, $"not an invoice this library reads: {Path.GetFileName(path)}");

        ValidationReport report = Schema.Validate(library.Write(read.Invoice!, DocumentFormat.Ubl));

        report.IsValid.ShouldBeTrue($"{Path.GetFileName(path)}{Environment.NewLine}{Describe(report)}");

        read.Diagnostics
            .Where(diagnostic => diagnostic.Code == UblDiagnostics.UnmappedElement.Code)
            .Select(diagnostic => diagnostic.Found)
            .ShouldBeEmpty($"{Path.GetFileName(path)} carries terms this library does not map");
    }

    public static TheoryData<string> OfficialExamples()
    {
        var data = new TheoryData<string>();
        string directory = Path.Combine(RepositoryRoot(), "specs", "en16931", "ubl", "examples");

        if (!Directory.Exists(directory))
        {
            return data;
        }

        foreach (string path in Directory.EnumerateFiles(directory, "*.xml").Order(StringComparer.Ordinal))
        {
            data.Add(path);
        }

        return data;
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "International.EInvoicing.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
    }

    private static EInvoice AnInvoice() => EInvoiceBuilder
        .Create(KnownProfiles.En16931Ubl)
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType(InvoiceTypeCodes.CommercialInvoice)
        .InCurrency("EUR")
        .WithBuyerReference("REF-2026-0001")
        .From(seller => seller
            .Named("Vendeur SAS")
            .WithVatIdentifier("FR40303265045")
            .WithElectronicAddress("seller@example.fr", "EM")
            .WithAddress(address =>
            {
                address.Line1 = "1 rue de la Paix";
                address.City = "Paris";
                address.PostCode = "75002";
                address.CountryCode = "FR";
            }))
        .To(buyer => buyer
            .Named("Acheteur GmbH")
            .WithElectronicAddress("buyer@example.de", "EM")
            .WithAddress(address =>
            {
                address.Line1 = "Musterstraße 1";
                address.City = "Berlin";
                address.PostCode = "10115";
                address.CountryCode = "DE";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Prestation")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m)
            .WithVat("S", 20m))
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "FR7630006000011234567890189" } },
        })
        .WithComputedVatBreakdown()
        .WithComputedTotals()
        .Build();

    private static string Describe(ValidationReport report) =>
        string.Join(
            Environment.NewLine,
            report.OfAtLeast(RuleSeverity.Error).Select(message => $"  {message.RuleIdentifier}: {message.Message}"));
}
