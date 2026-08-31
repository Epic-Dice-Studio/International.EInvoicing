using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Tests;

/// <summary>
/// Running your own logic during generation, without a fork.
/// </summary>
/// <remarks>
/// Numbering, house rounding, a signature, an element an ERP insists on: the promise is that a step runs for
/// every document the library writes, whoever asked and however. What these tests defend is the part that is
/// easy to get wrong — that there is no way past the steps, not even by taking the writer and using it
/// directly.
/// </remarks>
public class WritePipelineTests
{
    [Fact]
    public void AStepSeesTheInvoiceBeforeItIsWritten()
    {
        EInvoicing library = EInvoicing.Create(einvoicing => einvoicing
            .AddDefaults()
            .AddWriteStep((context, next) =>
            {
                context.Invoice.BuyerReference = "SERVICE-COMPTA";
                next(context);
            }));

        library.Write(AnInvoice(), DocumentFormat.Ubl).ShouldContain("SERVICE-COMPTA");
    }

    [Fact]
    public void AndTheDocumentAfterItIs()
    {
        EInvoicing library = EInvoicing.Create(einvoicing => einvoicing
            .AddDefaults()
            .AddWriteStep((context, next) =>
            {
                next(context);
                context.Xml = "<!-- signed -->" + context.Xml;
            }));

        library.Write(AnInvoice(), DocumentFormat.Cii).ShouldStartWith("<!-- signed -->");
    }

    /// <summary>The point of wrapping the writer rather than calling the steps from the facade.</summary>
    [Fact]
    public void AWriterTakenOutAndUsedDirectlyStillRunsTheSteps()
    {
        EInvoicing library = EInvoicing.Create(einvoicing => einvoicing
            .AddDefaults()
            .AddWriteStep((context, next) =>
            {
                next(context);
                context.Xml += "<!-- stamped -->";
            }));

        library.UblWriter.WriteToString(AnInvoice()).ShouldEndWith("<!-- stamped -->");
        library.CiiWriter.WriteToString(AnInvoice()).ShouldEndWith("<!-- stamped -->");
    }

    [Fact]
    public async Task IncludingWhenItWritesToAStream()
    {
        EInvoicing library = EInvoicing.Create(einvoicing => einvoicing
            .AddDefaults()
            .AddWriteStep((context, next) =>
            {
                next(context);
                context.Xml += "<!-- stamped -->";
            }));

        using var synchronous = new MemoryStream();
        WriteSynchronously(library.UblWriter, synchronous);

        using var asynchronous = new MemoryStream();
        await library.UblWriter.WriteAsync(AnInvoice(), asynchronous, TestContext.Current.CancellationToken);

        System.Text.Encoding.UTF8.GetString(synchronous.ToArray()).ShouldEndWith("<!-- stamped -->");
        System.Text.Encoding.UTF8.GetString(asynchronous.ToArray()).ShouldEndWith("<!-- stamped -->");
    }

    [Fact]
    public void StepsRunInTheOrderTheyWereAdded()
    {
        List<string> order = [];

        EInvoicing library = EInvoicing.Create(einvoicing => einvoicing
            .AddDefaults()
            .AddWriteStep((context, next) =>
            {
                order.Add("first in");
                next(context);
                order.Add("first out");
            })
            .AddWriteStep((context, next) =>
            {
                order.Add("second in");
                next(context);
                order.Add("second out");
            }));

        library.Write(AnInvoice(), DocumentFormat.Ubl);

        order.ShouldBe(["first in", "second in", "second out", "first out"]);
    }

    /// <summary>A step that declines to continue stops the write, and what it left is what comes out.</summary>
    [Fact]
    public void AStepThatDoesNotContinueStopsTheWrite()
    {
        bool reached = false;

        EInvoicing library = EInvoicing.Create(einvoicing => einvoicing
            .AddDefaults()
            .AddWriteStep((context, _) => context.Xml = "<refused/>")
            .AddWriteStep((context, next) =>
            {
                reached = true;
                next(context);
            }));

        library.Write(AnInvoice(), DocumentFormat.Ubl).ShouldBe("<refused/>");
        reached.ShouldBeFalse();
    }

    [Fact]
    public void AStepRegisteredThroughAContainerRunsToo()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddEInvoicing(einvoicing => einvoicing
                .AddDefaults()
                .AddWriteStep(new StampTheSyntax()))
            .BuildServiceProvider();

        EInvoicing library = provider.GetRequiredService<EInvoicing>();

        library.Write(AnInvoice(), DocumentFormat.Ubl).ShouldEndWith("<!-- ubl -->");
        library.Write(AnInvoice(), DocumentFormat.Cii).ShouldEndWith("<!-- cii -->");

        provider.Dispose();
    }

    /// <summary>Nothing registered, nothing wrapped: the writer you get is the writer that writes.</summary>
    [Fact]
    public void WithNoStepsTheWriterIsNotWrapped()
    {
        EInvoicing library = EInvoicing.CreateDefault();

        library.UblWriter.ShouldBeOfType<Ubl.Writing.UblInvoiceWriter>();
        library.Handlers.WriteSteps.ShouldBeEmpty();
    }

    /// <summary>Items are how one step tells the next what it worked out.</summary>
    [Fact]
    public void StepsCanHandThingsToOneAnother()
    {
        object? seen = null;

        EInvoicing library = EInvoicing.Create(einvoicing => einvoicing
            .AddDefaults()
            .AddWriteStep((context, next) =>
            {
                context.Items["house-reference"] = "REF-42";
                next(context);
            })
            .AddWriteStep((context, next) =>
            {
                seen = context.Items["house-reference"];
                next(context);
            }));

        library.Write(AnInvoice(), DocumentFormat.Ubl);

        seen.ShouldBe("REF-42");
    }

    private static void WriteSynchronously(IDocumentWriter<EInvoice> writer, Stream destination) =>
        writer.Write(AnInvoice(), destination);

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

    private sealed class StampTheSyntax : IWritePipelineStep
    {
        private static string Stamp(DocumentSyntax syntax) => syntax == DocumentSyntax.Ubl ? "ubl" : "cii";

        public void Write(WriteContext context, Action<WriteContext> next)
        {
            next(context);
            context.Xml += $"<!-- {Stamp(context.Syntax)} -->";
        }
    }
}
