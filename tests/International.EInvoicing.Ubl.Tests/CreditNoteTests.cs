using System.Xml.Linq;
using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl.Reading;
using International.EInvoicing.Ubl.Writing;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Ubl.Tests;

/// <summary>
/// UBL gives a credit note its own root element, and renames three things inside it.
/// </summary>
/// <remarks>
/// EN 16931 does not: there, an invoice and a credit note are the same document with a different BT-3, which
/// is why the model has one type for both. Reading a credit note as an invoice therefore produces something
/// that looks fine and has no type code and no lines — the failure a real integration meets on the day a
/// customer sends its first refund.
/// </remarks>
public class CreditNoteTests
{
    private static readonly XNamespace CreditNote = UblNames.CreditNote;

    private static UblInvoiceReader Reader() =>
        new(new EInvoicingOptions(), new ProfileResolver(new ProfileRegistry(KnownProfiles.All)));

    /// <summary>The official EN 16931 credit note, which is the only one published under a licence.</summary>
    private static string OfficialCreditNote() =>
        File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "specs",
            "en16931",
            "ubl",
            "examples",
            "ubl-tc434-creditnote1.xml"));

    [Fact]
    public void TheOfficialCreditNoteIsReadWithItsTypeCodeAndItsLines()
    {
        EInvoice creditNote = Reader().Read(OfficialCreditNote()).Value!;

        creditNote.TypeCode.Value.ShouldBe("381");
        creditNote.Lines.ShouldNotBeEmpty();
        creditNote.Lines[0].Quantity.HasValue.ShouldBeTrue();
        creditNote.Number.IsSet.ShouldBeTrue();
    }

    /// <summary>Nothing about it should end up as extension data — every element has a home.</summary>
    [Fact]
    public void NothingAboutItIsLeftUnmapped()
    {
        Diagnostics.ParseResult<EInvoice> result = Reader().Read(OfficialCreditNote());

        string[] creditNoteElements = ["CreditNoteTypeCode", "CreditNoteLine", "CreditedQuantity"];

        result.Diagnostics
            .Where(diagnostic => diagnostic.Code == "EIV2020")
            .Select(diagnostic => diagnostic.Found)
            .Intersect(creditNoteElements, StringComparer.Ordinal)
            .ShouldBeEmpty();
    }

    [Fact]
    public void ItIsWrittenBackAsACreditNoteRatherThanAnInvoice()
    {
        EInvoice creditNote = Reader().Read(OfficialCreditNote()).Value!;

        XElement written = XElement.Parse(new UblInvoiceWriter().WriteToString(creditNote));

        written.Name.ShouldBe(CreditNote + "CreditNote");
        written.Elements(UblNames.Cbc + "CreditNoteTypeCode").ShouldHaveSingleItem();
        written.Elements(UblNames.Cac + "CreditNoteLine").ShouldNotBeEmpty();
        written.Descendants(UblNames.Cbc + "CreditedQuantity").ShouldNotBeEmpty();
        written.Descendants(UblNames.Cbc + "InvoicedQuantity").ShouldBeEmpty();
    }

    [Fact]
    public void ACreditNoteSurvivesTheRoundTrip()
    {
        string original = OfficialCreditNote();
        EInvoice read = Reader().Read(original).Value!;
        EInvoice again = Reader().Read(new UblInvoiceWriter().WriteToString(read)).Value!;

        again.Number.Value.ShouldBe(read.Number.Value);
        again.TypeCode.Value.ShouldBe(read.TypeCode.Value);
        again.Lines.Count.ShouldBe(read.Lines.Count);
        again.Totals.DuePayableAmount.Value.ShouldBe(read.Totals.DuePayableAmount.Value);
        Counted(again, CreditNote + "CreditNote").ShouldBe(Counted(read, CreditNote + "CreditNote"));
    }

    /// <summary>The type code decides the root, so a credit note built in code is written as one.</summary>
    [Theory]
    [InlineData("380", "Invoice")]
    [InlineData("381", "CreditNote")]
    [InlineData("261", "CreditNote")]
    [InlineData("384", "Invoice")]
    public void TheTypeCodeDecidesTheRootElement(string typeCode, string expectedRoot)
    {
        EInvoice document = EInvoiceBuilder
            .Create(KnownProfiles.En16931Ubl)
            .WithNumber("AV-1")
            .OfType(typeCode)
            .InCurrency("EUR")
            .AddLine(line => line.WithItem("Remboursement").WithNetAmount(100m).WithVat("S", 20m))
            .Build();

        XElement written = XElement.Parse(new UblInvoiceWriter().WriteToString(document));

        written.Name.LocalName.ShouldBe(expectedRoot);
    }

    private static int Counted(EInvoice invoice, XName name) =>
        XElement.Parse(new UblInvoiceWriter().WriteToString(invoice)).DescendantsAndSelf(name).Count();

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
