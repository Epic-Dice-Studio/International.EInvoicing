using International.EInvoicing.Playground.Services;
using International.EInvoicing.Profiles;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Playground.Tests;

/// <summary>
/// The conversion the playground offers, held to what the library actually does.
/// </summary>
/// <remarks>
/// A visitor who converts a sample and is handed a document their receiver would reject learns the wrong
/// thing, and the loss report is the whole point of offering conversion at all — a silent one is the
/// dangerous version. So every sample the page lists is converted both ways here, on every commit.
/// </remarks>
public class ConversionOfferedTests
{
    private static readonly DocumentInspector Inspector = new();

    public static TheoryData<string, string> Cases
    {
        get
        {
            var data = new TheoryData<string, string>();

            foreach ((string key, _) in Samples.Catalogue)
            {
                data.Add(key, "ubl");
                data.Add(key, "cii");
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void EverySampleTheSiteOffersCrossesBothWays(string sample, string target)
    {
        string? document = Samples.Get(sample);

        document.ShouldNotBeNull();

        Services.DocumentKind kind = Inspector.Detect(document);

        if (kind is not (Services.DocumentKind.Ubl or Services.DocumentKind.Cii))
        {
            // The page refuses these by name rather than converting them: a lifecycle status and an
            // e-reporting transmission are documents of their own, not another syntax for an invoice.
            return;
        }

        ConversionResult result = Inspector.Library.Convert(
            document,
            target == "cii" ? DocumentFormat.Cii : DocumentFormat.Ubl);

        result.Xml.ShouldNotBeNullOrEmpty($"{sample} produced nothing at all");
        result.Invoice.ShouldNotBeNull($"{sample} came back as something that no longer reads as an invoice");
        result.Invoice!.Number.Value.ShouldNotBeNullOrEmpty();
    }

    /// <summary>What the page says it cannot convert, it really cannot — and says so rather than trying.</summary>
    [Fact]
    public void AndWhatCannotCrossIsRecognisedBeforeItIsTried()
    {
        foreach ((string key, _) in Samples.Catalogue)
        {
            string document = Samples.Get(key)!;
            Services.DocumentKind kind = Inspector.Detect(document);

            if (kind is Services.DocumentKind.Cdar or Services.DocumentKind.EReport)
            {
                Inspector.Library.Convert(document, DocumentFormat.Ubl).Xml.ShouldBeEmpty(key);
            }
        }
    }
}
