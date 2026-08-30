using International.EInvoicing.Building;
using International.EInvoicing.Countries.France;
using International.EInvoicing.Countries.France.Lifecycle;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Tests;

/// <summary>
/// The short way in. A caller hands over a document without saying what it is, and gets back what it turned
/// out to be — which is the whole point of this layer.
/// </summary>
public class EInvoicingTests
{
    private static readonly EInvoicing Library = EInvoicing.CreateDefault();

    private static EInvoice AnInvoice() =>
        EInvoiceBuilder.Create(KnownProfiles.En16931Cii)
            .WithNumber("FA-2026-001")
            .IssuedOn(new DateOnly(2026, 8, 30))
            .OfType("380")
            .InCurrency("EUR")
            .WithSeller(seller => seller.Named("Epic Dice Studio"))
            .WithBuyer(buyer => buyer.Named("Acme"))
            .AddLine(line => line.WithIdentifier("1").WithItem("Consulting").WithNetAmount(450m))
            .Build();

    [Theory]
    [InlineData(DocumentFormat.Ubl, DocumentKind.Ubl)]
    [InlineData(DocumentFormat.Cii, DocumentKind.Cii)]
    public void AnInvoiceGoesOutAndComesBackWithoutTheCallerNamingTheSyntax(
        DocumentFormat written,
        DocumentKind detected)
    {
        string xml = Library.Write(AnInvoice(), written);

        DocumentResult result = Library.Read(xml);

        result.Kind.ShouldBe(detected);
        result.IsUsable.ShouldBeTrue();
        result.Invoice!.Number.Value.ShouldBe("FA-2026-001");
        result.LifecycleStatus.ShouldBeNull();
    }

    [Fact]
    public void AStreamIsEnough()
    {
        using var stream = new MemoryStream(
            System.Text.Encoding.UTF8.GetBytes(Library.Write(AnInvoice(), DocumentFormat.Ubl)));

        Library.Read(stream).Invoice!.Number.Value.ShouldBe("FA-2026-001");
    }

    [Fact]
    public void ALifecycleMessageComesBackAsOne()
    {
        string xml = Library.Write(
            FrCdar.FromBuyer("200000008", "ACHETEUR")
                .SentBy("0003", "PA-E")
                .ToSeller("100000009", "VENDEUR")
                .About("F202500003", new DateOnly(2025, 7, 1))
                .Approved(new DateTimeOffset(2025, 7, 1, 15, 0, 0, TimeSpan.Zero)));

        DocumentResult result = Library.Read(xml);

        result.Kind.ShouldBe(DocumentKind.Cdar);
        result.LifecycleStatus!.References[0].ProcessConditionCode.Value.ShouldBe("205");
        result.Invoice.ShouldBeNull();
    }

    [Fact]
    public void ACreditNoteIsRecognisedAsOne()
    {
        EInvoice creditNote = EInvoiceBuilder.Create(KnownProfiles.En16931Ubl)
            .WithNumber("AV-2026-001")
            .IssuedOn(new DateOnly(2026, 8, 30))
            .OfType("381")
            .InCurrency("EUR")
            .Build();

        DocumentResult result = Library.Read(Library.Write(creditNote, DocumentFormat.Ubl));

        result.IsCreditNote.ShouldBeTrue();
        Library.Read(Library.Write(AnInvoice(), DocumentFormat.Ubl)).IsCreditNote.ShouldBeFalse();
    }

    [Fact]
    public void SomethingUnrecognisedIsReportedRatherThanThrown()
    {
        DocumentResult result = Library.Read("<order xmlns='urn:acme:orders:1'><id>1</id></order>");

        result.IsUsable.ShouldBeFalse();
        result.Kind.ShouldBe(DocumentKind.Unknown);
        result.Diagnostics.ShouldHaveSingleItem().Code.ShouldBe("EIV5010");
    }

    [Fact]
    public void MalformedXmlIsReportedRatherThanThrown()
    {
        DocumentResult result = Library.Read("<Invoice><unclosed>");

        result.IsUsable.ShouldBeFalse();
        result.Diagnostics.ShouldNotBeEmpty();
    }

    [Fact]
    public void APdfWithNoReaderRegisteredSaysWhatToReference()
    {
        DocumentResult result = Library.Read("%PDF-1.7\nnot really a pdf"u8.ToArray());

        result.Kind.ShouldBe(DocumentKind.Pdf);
        result.IsUsable.ShouldBeFalse();
        result.Diagnostics.ShouldNotBeEmpty();
    }

    [Fact]
    public void ValidationSaysWhatItCouldNotCheck()
    {
        string xml = Library.Write(AnInvoice(), DocumentFormat.Ubl)
            .Replace("urn:cen.eu:en16931:2017", "urn:acme:profile:2p0", StringComparison.Ordinal);

        ValidationReport report = Library.Validate(xml);

        report.IsComplete.ShouldBeFalse();
        report.RuleSets.ShouldContain(set => !set.Ran && set.SkippedBecause!.Contains("no rule set"));
    }

    /// <summary>
    /// A document nothing is registered to check is reported as unchecked, and told how to fix that — never
    /// presented as valid.
    /// </summary>
    [Fact]
    public void ValidatingSomethingNoRuleSetCoversSaysSo()
    {
        string status = Library.Write(
            FrCdar.FromBuyer("200000008", "ACHETEUR")
                .SentBy("0003", "PA-E")
                .ToSeller("100000009", "VENDEUR")
                .About("F1", new DateOnly(2026, 1, 1))
                .Approved());

        ValidationReport report = Library.Validate(status);

        report.IsComplete.ShouldBeFalse();
        report.RuleSets.ShouldHaveSingleItem().SkippedBecause!.ShouldContain("AddRules");
    }

    /// <summary>
    /// The rule sets are what a caller assembled, so adding one is all it takes for validation to use it.
    /// </summary>
    [Fact]
    public void ARuleSetAddedWhenAssemblingIsRunWhenItApplies()
    {
        EInvoicing library = EInvoicing.Create(einvoicing => einvoicing
            .AddDefaults()
            .AddFrance()
            .AddRulesFrom(
                DocumentSyntax.Cdar,
                """
                <schema xmlns="http://purl.oclc.org/dsdl/schematron">
                  <ns prefix="rsm" uri="urn:un:unece:uncefact:data:standard:CrossDomainAcknowledgementAndResponse:100"/>
                  <ns prefix="ram" uri="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100"/>
                  <pattern>
                    <rule context="rsm:ExchangedDocument">
                      <assert id="HOUSE-1" test="ram:Name">Every status message we send carries a name.</assert>
                    </rule>
                  </pattern>
                </schema>
                """,
                "House rules",
                "1.0"));

        string status = library.Write(
            FrCdar.FromBuyer("200000008", "ACHETEUR")
                .SentBy("0003", "PA-E")
                .ToSeller("100000009", "VENDEUR")
                .About("F1", new DateOnly(2026, 1, 1))
                .Approved());

        ValidationReport report = library.Validate(status);

        report.RuleSets.ShouldHaveSingleItem().Name.ShouldBe("House rules");
        report.Messages.ShouldHaveSingleItem().RuleIdentifier.ShouldBe("HOUSE-1");
    }

    [Fact]
    public void TheLayersUnderneathStayReachable()
    {
        Library.Ubl.ShouldNotBeNull();
        Library.Cii.ShouldNotBeNull();
        Library.Lifecycle.ShouldNotBeNull();
        Library.Profiles.Resolve(KnownProfiles.En16931Ubl.Id, DocumentSyntax.Ubl).IsExact.ShouldBeTrue();
    }
}
