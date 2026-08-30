using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Tests;

/// <summary>
/// The two promises this file defends: an asynchronous path at every layer, and a reader of your own taking
/// the place of ours.
/// </summary>
/// <remarks>
/// The asynchrony is the transfer, not the parse — see
/// <c>docs/adr/0012-async-at-the-boundary.md</c>. What must hold is that the asynchronous path gives exactly
/// what the synchronous one gives, and that a token stops the transfer.
/// </remarks>
public class AsyncAndSubstitutionTests
{
    [Fact]
    public async Task ReadingAsynchronouslyGivesWhatReadingSynchronouslyGives()
    {
        EInvoicing library = EInvoicing.CreateDefault();
        string xml = library.Write(AnInvoice());

        DocumentResult synchronous = library.Read(xml);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        DocumentResult asynchronous = await library.ReadAsync(stream, TestContext.Current.CancellationToken);

        asynchronous.Kind.ShouldBe(synchronous.Kind);
        asynchronous.RequireInvoice().Number.Value.ShouldBe(synchronous.RequireInvoice().Number.Value);
        asynchronous.Diagnostics.Count.ShouldBe(synchronous.Diagnostics.Count);
    }

    /// <summary>Every layer, not only the facade: a caller using a reader directly gets the same choice.</summary>
    [Fact]
    public async Task EveryReaderAndWriterHasAnAsynchronousPath()
    {
        EInvoicing library = EInvoicing.CreateDefault();
        EInvoice invoice = AnInvoice();

        using var written = new MemoryStream();
        await library.UblWriter.WriteAsync(invoice, written, TestContext.Current.CancellationToken);
        written.Position = 0;

        ParseResult<EInvoice> read = await library.Ubl.ReadAsync(written, TestContext.Current.CancellationToken);

        read.IsUsable.ShouldBeTrue();
        read.Value!.Number.Value.ShouldBe(invoice.Number.Value);
        System.Text.Encoding.UTF8.GetString(written.ToArray()).ShouldBe(library.UblWriter.WriteToString(invoice));
    }

    [Fact]
    public async Task ATokenStopsTheTransfer()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("<nothing/>"));

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await EInvoicing.CreateDefault().ReadAsync(stream, cancellation.Token));
    }

    /// <summary>
    /// The first promise in the README: register your own reader and it takes the place of ours.
    /// </summary>
    [Fact]
    public void AReaderOfYourOwnTakesThePlaceOfTheBuiltInOne()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddEInvoicing(einvoicing => einvoicing.AddDefaults())
            .AddSingleton<IDocumentReader<EInvoice>, HouseUblReader>()
            .BuildServiceProvider();

        EInvoicing library = provider.GetRequiredService<EInvoicing>();

        library.Ubl.ShouldBeOfType<HouseUblReader>();
        library.Read("<nothing/>");                      // reaches ours only for UBL, so this is unaffected
        library.Cii.ShouldNotBeOfType<HouseUblReader>();

        provider.Dispose();
    }

    /// <summary>And a writer.</summary>
    [Fact]
    public void SoDoesAWriterOfYourOwn()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddEInvoicing(einvoicing => einvoicing.AddDefaults())
            .AddSingleton<IDocumentWriter<EInvoice>, HouseUblWriter>()
            .BuildServiceProvider();

        EInvoicing library = provider.GetRequiredService<EInvoicing>();

        library.UblWriter.ShouldBeOfType<HouseUblWriter>();
        library.Write(AnInvoice(), DocumentFormat.Ubl).ShouldBe("<house/>");

        provider.Dispose();
    }

    /// <summary>The concrete type and the interface resolve to the same instance, not two singletons.</summary>
    [Fact]
    public void TheConcreteTypeAndTheInterfaceAreTheSameInstance()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddEInvoicing(einvoicing => einvoicing.AddDefaults())
            .BuildServiceProvider();

        object byType = provider.GetRequiredService<Ubl.Reading.UblInvoiceReader>();
        object byInterface = provider.GetServices<IDocumentReader<EInvoice>>()
            .First(reader => reader.Syntax == DocumentSyntax.Ubl);

        byInterface.ShouldBeSameAs(byType);

        provider.Dispose();
    }

    private static EInvoice AnInvoice() => EInvoiceBuilder
        .Create(KnownProfiles.En16931Ubl)
        .WithNumber("FA-2026-001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .InCurrency("EUR")
        .From("Fournisseur SARL", "FR32732829320")
        .To("Client SA", "FR89552081317")
        .AddLine(line => line.WithItem("Conseil").WithNetAmount(450m).WithVat("S", 20m))
        .WithComputedVatBreakdown()
        .WithComputedTotals()
        .Build();

    /// <summary>A reader a company might write to accept its own dialect of UBL.</summary>
    private sealed class HouseUblReader : IDocumentReader<EInvoice>
    {
        public DocumentSyntax Syntax => DocumentSyntax.Ubl;

        public ParseResult<EInvoice> Read(Stream stream) => Read(string.Empty);

        public ParseResult<EInvoice> Read(string xml) =>
            new(new EInvoice { Number = "HOUSE-1" }, []);

        public Task<ParseResult<EInvoice>> ReadAsync(Stream stream, CancellationToken cancellationToken = default) =>
            Task.FromResult(Read(stream));
    }

    private sealed class HouseUblWriter : IDocumentWriter<EInvoice>
    {
        public DocumentSyntax Syntax => DocumentSyntax.Ubl;

        public void Write(EInvoice document, Stream destination) { }

        public string WriteToString(EInvoice document) => "<house/>";

        public Task WriteAsync(EInvoice document, Stream destination, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
