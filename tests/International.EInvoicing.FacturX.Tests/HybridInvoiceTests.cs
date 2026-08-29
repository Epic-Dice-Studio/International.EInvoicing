using International.EInvoicing.Building;
using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.FacturX.PdfSharp;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using PdfSharp.Pdf;
using Shouldly;
using Xunit;

namespace International.EInvoicing.FacturX.Tests;

/// <summary>
/// A hybrid invoice is a PDF a person reads with the machine-readable half embedded in it. These tests build
/// both halves from one model and check they survive the round trip through the container.
/// </summary>
public class HybridInvoiceTests
{
    private static readonly EInvoicingOptions Options = new();

    private static CiiInvoiceReader CiiReader() =>
        new(Options, new ProfileResolver(new ProfileRegistry(KnownProfiles.All)));

    private static FacturXReader Reader(IPdfAttachmentReader? pdf) =>
        new(Options, CiiReader(), pdf);

    private static EInvoice AnInvoice(Profile profile) =>
        EInvoiceBuilder.Create(profile)
            .WithNumber("FA-2026-001")
            .IssuedOn(new DateOnly(2026, 8, 29))
            .OfType("380")
            .InCurrency("EUR")
            .WithSeller(seller => seller.Named("Epic Dice Studio").WithVatIdentifier("FR12345678901"))
            .WithBuyer(buyer => buyer.Named("Acme"))
            .AddLine(line => line.WithIdentifier("1").WithItem("Consulting").WithNetAmount(450m))
            .Build();

    /// <summary>
    /// Stands in for the PDF a person would read. It is deliberately blank: drawing text would need a font
    /// resolver, and what these tests are about is the container, not its contents.
    /// </summary>
    private static MemoryStream AHumanReadablePdf()
    {
        using var document = new PdfDocument();
        document.AddPage();

        var stream = new MemoryStream();
        document.Save(stream, closeStream: false);
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream AHybridInvoice(EInvoice invoice)
    {
        var writer = new FacturXWriter(new CiiInvoiceWriter(), new PdfSharpAttachmentWriter());
        using MemoryStream pdf = AHumanReadablePdf();
        var hybrid = new MemoryStream();
        writer.Write(invoice, pdf, hybrid);
        hybrid.Position = 0;
        return hybrid;
    }

    [Fact]
    public void AHybridInvoiceIsStillAPdf()
    {
        using MemoryStream hybrid = AHybridInvoice(AnInvoice(FacturXProfiles.Basic));

        FacturXReader.LooksLikePdf(hybrid.ToArray()).ShouldBeTrue();
    }

    [Fact]
    public void ThePayloadComesBackOutOfThePdf()
    {
        EInvoice original = AnInvoice(FacturXProfiles.Basic);

        using MemoryStream hybrid = AHybridInvoice(original);
        ParseResult<EInvoice> result = Reader(new PdfSharpAttachmentReader()).Read(hybrid);

        result.IsUsable.ShouldBeTrue(string.Join("; ", result.Diagnostics.Select(d => d.ToString())));
        result.Value!.Number.Value.ShouldBe("FA-2026-001");
        result.Value.Lines.ShouldHaveSingleItem().Item!.Name.Value.ShouldBe("Consulting");
        result.Value.SpecificationIdentifier.ShouldBe(FacturXProfiles.Basic.Id);
    }

    [Fact]
    public void ThePayloadIsFiledUnderTheNameFacturXRequires()
    {
        using MemoryStream hybrid = AHybridInvoice(AnInvoice(FacturXProfiles.Basic));

        FacturXAttachment attachment = new PdfSharpAttachmentReader()
            .FindAttachment(hybrid, FacturXAttachment.KnownFileNames, Options.Limits.MaxAttachmentBytes)!;

        attachment.FileName.ShouldBe("factur-x.xml");
        attachment.Relationship.ShouldBe("Alternative");
    }

    [Fact]
    public void BareCiiIsReadWithoutAPdfReader()
    {
        string cii = new CiiInvoiceWriter().WriteToString(AnInvoice(FacturXProfiles.Basic));

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cii));
        Reader(pdf: null).Read(stream).Value!.Number.Value.ShouldBe("FA-2026-001");
    }

    [Fact]
    public void APdfWithNoInvoiceInsideIsReportedRatherThanThrown()
    {
        using MemoryStream plain = AHumanReadablePdf();

        ParseResult<EInvoice> result = Reader(new PdfSharpAttachmentReader()).Read(plain);

        result.IsUsable.ShouldBeFalse();
        result.Diagnostics.ShouldHaveSingleItem().Code.ShouldBe("EIV4001");
    }

    [Fact]
    public void APdfArrivingWithNoPdfReaderRegisteredIsReportedRatherThanThrown()
    {
        using MemoryStream hybrid = AHybridInvoice(AnInvoice(FacturXProfiles.Basic));

        ParseResult<EInvoice> result = Reader(pdf: null).Read(hybrid);

        result.IsUsable.ShouldBeFalse();
        result.Diagnostics.ShouldHaveSingleItem().Expected!.ShouldContain("IPdfAttachmentReader");
    }

    [Fact]
    public void AMinimumProfileIsReadButTheCallerIsToldItIsNotAnInvoice()
    {
        EInvoice minimum = AnInvoice(FacturXProfiles.Minimum);

        using MemoryStream hybrid = AHybridInvoice(minimum);
        ParseResult<EInvoice> result = Reader(new PdfSharpAttachmentReader()).Read(hybrid);

        result.IsUsable.ShouldBeTrue();
        Diagnostic diagnostic = result.Diagnostics.Single(d => d.Code == "EIV4010");
        diagnostic.BusinessTerm.ShouldBe("BT-24");
        diagnostic.Message.ShouldContain("not the lines EN 16931 requires");
    }

    [Fact]
    public void AnEn16931ProfileRaisesNoSuchWarning()
    {
        using MemoryStream hybrid = AHybridInvoice(AnInvoice(FacturXProfiles.En16931));

        ParseResult<EInvoice> result = Reader(new PdfSharpAttachmentReader()).Read(hybrid);

        result.Diagnostics.ShouldNotContain(d => d.Code == "EIV4010");
    }

    [Fact]
    public void WritingAHybridWithoutAPdfWriterSaysWhatToReference()
    {
        var writer = new FacturXWriter(new CiiInvoiceWriter());
        using MemoryStream pdf = AHumanReadablePdf();
        using var destination = new MemoryStream();

        Should.Throw<InvalidOperationException>(() => writer.Write(AnInvoice(FacturXProfiles.Basic), pdf, destination))
            .Message.ShouldContain("International.EInvoicing.FacturX.PdfSharp");
    }
}
