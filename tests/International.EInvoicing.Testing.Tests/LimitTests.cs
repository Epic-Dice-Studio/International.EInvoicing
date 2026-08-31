using International.EInvoicing.Configuration;
using International.EInvoicing.Model;
using International.EInvoicing.Values;
using International.EInvoicing.Xml;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Testing.Tests;

/// <summary>
/// The limits, actually enforced.
/// </summary>
/// <remarks>
/// A limit that is declared and not enforced is worse than no limit: it is documented reassurance a reader
/// relies on. Each of these was declared on <see cref="DocumentLimits"/> before anything checked it, which is
/// exactly the sort of thing the hostile corpus exists to find.
/// </remarks>
public class LimitTests
{
    [Fact]
    public void ADocumentNestedPastTheLimitIsRefusedRatherThanHandedOn()
    {
        EInvoicing library = EInvoicing.CreateDefault();
        HostileDocument deep = HostileDocuments.All.Single(document => document.Name == "nested-a-thousand-deep");

        DocumentResult result = library.Read(deep.Xml);

        result.IsUsable.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Message.Contains("nests deeper", StringComparison.Ordinal));
    }

    /// <summary>Under the limit, nothing changes — the guard must not cost ordinary documents anything.</summary>
    [Fact]
    public void AnOrdinaryDocumentIsUnaffected()
    {
        EInvoicing library = EInvoicing.CreateDefault();

        library.Read(library.Write(SampleInvoices.Conforming(), DocumentFormat.Ubl)).IsUsable.ShouldBeTrue();
    }

    /// <summary>
    /// The attachment is measured before it is decoded, which is the whole point.
    /// </summary>
    /// <remarks>
    /// Decoding first and measuring after is how a document with a hundred-megabyte payload takes the process
    /// down, and no <c>catch</c> recovers from that.
    /// </remarks>
    [Theory]
    [InlineData(DocumentFormat.Ubl)]
    [InlineData(DocumentFormat.Cii)]
    public void AnAttachmentOverTheLimitIsRefusedAndItsTextKept(DocumentFormat format)
    {
        EInvoicing library = WithLimits(new DocumentLimits { MaxAttachmentBytes = 1_024 });
        EInvoice invoice = WithAttachment(new byte[6_000]);

        DocumentResult result = library.Read(library.Write(invoice, format));

        BinaryField attachment = result.RequireInvoice().AdditionalDocuments.Single().Attachment;

        attachment.Value.ShouldBeNull();
        attachment.Raw.ShouldNotBeNullOrEmpty();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "EIV4003");
    }

    [Theory]
    [InlineData(DocumentFormat.Ubl)]
    [InlineData(DocumentFormat.Cii)]
    public void AnAttachmentUnderTheLimitIsDecoded(DocumentFormat format)
    {
        EInvoicing library = WithLimits(new DocumentLimits { MaxAttachmentBytes = 1_024 });
        EInvoice invoice = WithAttachment("a small receipt"u8.ToArray());

        DocumentResult result = library.Read(library.Write(invoice, format));

        System.Text.Encoding.UTF8
            .GetString(result.RequireInvoice().AdditionalDocuments.Single().Attachment.Value!)
            .ShouldBe("a small receipt");
    }

    [Theory]
    [InlineData(DocumentFormat.Ubl)]
    [InlineData(DocumentFormat.Cii)]
    public void MoreLinesThanTheLimitAllowsStopsAndSaysSo(DocumentFormat format)
    {
        EInvoicing library = WithLimits(new DocumentLimits { MaxDocumentLines = 3 });

        EInvoice invoice = SampleInvoices.Conforming(configure: builder =>
        {
            for (int line = 2; line <= 10; line++)
            {
                int number = line;
                builder.AddLine(item => item
                    .WithIdentifier(number.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .WithItem($"Item {number}")
                    .WithQuantity(1m, "C62")
                    .WithNetPrice(10m)
                    .WithNetAmount(10m)
                    .WithVat(VatCategoryCodes.Standard, 20m));
            }
        });

        DocumentResult result = library.Read(library.Write(invoice, format));

        result.RequireInvoice().Lines.Count.ShouldBe(3);
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "EIV4004");
    }

    [Theory]
    [InlineData(DocumentFormat.Ubl)]
    [InlineData(DocumentFormat.Cii)]
    public void MoreAttachmentsThanTheLimitAllowsStopsAndSaysSo(DocumentFormat format)
    {
        EInvoicing library = WithLimits(new DocumentLimits { MaxAttachmentCount = 2 });

        EInvoice invoice = SampleInvoices.Conforming(configure: builder => builder.Extend(built =>
        {
            for (int index = 1; index <= 5; index++)
            {
                built.AdditionalDocuments.Add(new AdditionalDocument
                {
                    Identifier = $"DOC-{index}",
                    Description = "A receipt",
                });
            }
        }));

        DocumentResult result = library.Read(library.Write(invoice, format));

        result.RequireInvoice().AdditionalDocuments.Count.ShouldBe(2);
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "EIV4004");
    }

    /// <summary>A limit of zero is no limit: the escape hatch for a caller who trusts the source.</summary>
    [Fact]
    public void ALimitOfZeroIsNoLimit()
    {
        Limits.Exceeded(soFar: 1_000_000, limit: 0).ShouldBeFalse();

        EInvoicing library = WithLimits(DocumentLimits.Unlimited);
        HostileDocument deep = HostileDocuments.All.Single(document => document.Name == "nested-a-thousand-deep");

        library.Read(deep.Xml).Diagnostics
            .ShouldNotContain(diagnostic => diagnostic.Message.Contains("nests deeper", StringComparison.Ordinal));
    }

    private static EInvoicing WithLimits(DocumentLimits limits) =>
        EInvoicing.Create(einvoicing => einvoicing.AddDefaults().UseLimits(limits));

    private static EInvoice WithAttachment(byte[] content) =>
        SampleInvoices.Conforming(configure: builder => builder.Extend(invoice =>
            invoice.AdditionalDocuments.Add(new AdditionalDocument
            {
                Identifier = "ATTACHMENT-1",
                Description = "A receipt",
                Attachment = new BinaryField(content, "application/pdf", "receipt.pdf"),
            })));
}
