using System.Xml.Linq;
using International.EInvoicing.Cdar;
using International.EInvoicing.Cdar.Reading;
using International.EInvoicing.Cdar.Writing;
using International.EInvoicing.Configuration;
using International.EInvoicing.Countries.France.Lifecycle;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.France.Tests;

/// <summary>
/// The point of the builder is that naming a status is enough. These tests check that what a status implies
/// is actually filled in, against the codes read from the DGFiP sample messages.
/// </summary>
public class FrCdarTests
{
    private static readonly DateTimeOffset Moment = new(2025, 7, 1, 15, 10, 0, TimeSpan.Zero);

    /// <summary>A platform event: the platform that files an invoice is the one that reports it.</summary>
    private static FrCdar FromPlatform() =>
        FrCdar.FromPlatform("0003", "PA-E Vendeur")
            .ToSeller("100000009", "VENDEUR", "100000009_STATUTS")
            .About("F202500003", new DateOnly(2025, 7, 1));

    /// <summary>A business event: a trading party reports it, and its platform transmits it.</summary>
    private static FrCdar FromBuyer() =>
        FrCdar.FromBuyer("200000008", "ACHETEUR")
            .SentBy("0003", "PA-E Acheteur")
            .ToSeller("100000009", "VENDEUR", "100000009_STATUTS")
            .About("F202500003", new DateOnly(2025, 7, 1));

    private static string Write(LifecycleStatusMessage message) => new CdarWriter().WriteToString(message);

    [Fact]
    public void NamingAStatusFillsInTheCodesItImplies()
    {
        LifecycleStatusMessage message = FromPlatform().Filed(Moment);

        message.TypeCode.Value.ShouldBe("305");
        ReferencedDocumentStatus reference = message.References.ShouldHaveSingleItem();
        reference.StatusCode.Value.ShouldBe("10");
        reference.ProcessConditionCode.Value.ShouldBe("200");
        reference.ProcessCondition.Value.ShouldBe("Déposée");
    }

    [Theory]
    [InlineData("Filed", "200", "305", "10")]
    [InlineData("Received", "202", "305", "43")]
    [InlineData("MadeAvailable", "203", "305", "48")]
    [InlineData("TakenInCharge", "204", "23", "45")]
    [InlineData("Approved", "205", "23", "1")]
    [InlineData("Disputed", "207", "23", "46")]
    [InlineData("PaymentSent", "211", "23", "47")]
    [InlineData("Collected", "212", "23", "47")]
    public void TheVerifiedStatusesCarryTheCodesTheDgfipSamplesUse(
        string name,
        string statusCode,
        string acknowledgementTypeCode,
        string documentStatusCode)
    {
        FrLifecycleStatus status = FrLifecycleStatus.All.Single(s => s.Code == statusCode);

        status.IsVerified.ShouldBeTrue($"{name} was read from a sample message");
        status.AcknowledgementTypeCode.ShouldBe(acknowledgementTypeCode);
        status.DocumentStatusCode.ShouldBe(documentStatusCode);
    }

    [Fact]
    public void AStatusThisLibraryCouldNotVerifySaysSo()
    {
        FrLifecycleStatus.Refused.IsVerified.ShouldBeFalse();
        FrLifecycleStatus.Filed.IsVerified.ShouldBeTrue();

        FrLifecycleStatus corrected = FrLifecycleStatus.Refused.WithCodes("23", "46");
        corrected.IsVerified.ShouldBeTrue();
    }

    [Fact]
    public void RefusingCarriesTheReasonWhereTheDgfipPutsIt()
    {
        LifecycleStatusMessage message = FromBuyer()
            .Refused("TX_TVA_ERR", "Taux de TVA erroné", Moment);

        XElement written = XElement.Parse(Write(message));
        XElement status = written.Descendants(CdarNames.Ram + "SpecifiedDocumentStatus").ShouldHaveSingleItem();

        status.Element(CdarNames.Ram + "ReasonCode")!.Value.ShouldBe("TX_TVA_ERR");
        status.Element(CdarNames.Ram + "Reason")!.Value.ShouldBe("Taux de TVA erroné");
        status.Element(CdarNames.Ram + "SequenceNumeric")!.Value.ShouldBe("1");
        status.Parent!.Name.LocalName.ShouldBe("ReferenceReferencedDocument");
    }

    [Fact]
    public void AStatusNeedingAReasonWillNotBeBuiltWithout()
    {
        Should.Throw<ArgumentException>(() => FromPlatform().Refused(string.Empty, "Taux de TVA erroné"));
        Should.Throw<ArgumentException>(() => FromPlatform().Disputed("TX_TVA_ERR", string.Empty));
        FrLifecycleStatus.Refused.RequiresReason.ShouldBeTrue();
        FrLifecycleStatus.Approved.RequiresReason.ShouldBeFalse();
    }

    [Fact]
    public void SendingToAPartnerAddressesThePublicPortalToo()
    {
        LifecycleStatusMessage message = FromPlatform().Filed(Moment);

        message.SpecificationIdentifier.Value.ShouldBe("urn.cpro.gouv.fr:1p0:CDV:invoice");
        message.BusinessProcessType.Value.ShouldBe("REGULATED");
        message.Recipients.Count.ShouldBe(2);
        message.Recipients[0].RoleCode.Value.ShouldBe("SE");
        message.Recipients[1].Name.Value.ShouldBe("PPF");
        message.Recipients[1].GlobalIdentifier.Value.ShouldBe("9998");
    }

    [Fact]
    public void ReportingToThePublicPortalIsADifferentProfileNotAVariant()
    {
        LifecycleStatusMessage message = FrCdar.FromPlatform("0003", "PA-E Vendeur")
            .ToPublicPortal()
            .About("F202500003", new DateOnly(2025, 7, 1))
            .Filed(Moment);

        message.SpecificationIdentifier.Value.ShouldBe("urn.cpro.gouv.fr:1p0:CDV:einvoicingF2");
        message.BusinessProcessType.IsSet.ShouldBeFalse();
        message.Recipients.ShouldHaveSingleItem().GlobalIdentifier.Value.ShouldBe("0000");

        XElement written = XElement.Parse(Write(message));
        written.Descendants(CdarNames.Ram + "ReferenceTypeCode").ShouldHaveSingleItem()
            .Value.ShouldBe("urn.cpro.gouv.fr:1p0:CDV:einvoicingF2");
    }

    [Fact]
    public void AMessageBuiltHereIsReadBackByTheGenericReader()
    {
        LifecycleStatusMessage built = FromBuyer().Approved(Moment);

        var reader = new CdarReader(
            new EInvoicingOptions(),
            new ProfileResolver(new ProfileRegistry(FrProfiles.All)));

        LifecycleStatusMessage read = reader.Read(Write(built)).Value!;

        read.Profile!.IsExact.ShouldBeTrue();
        read.References[0].ProcessConditionCode.Value.ShouldBe("205");
        read.References[0].ProcessCondition.Value.ShouldBe("Approuvée");
        read.StatusIssuedAt.Value.ShouldBe(Moment);
    }

    /// <summary>
    /// A message with no moment takes it from the clock, which a test can fix — and the identifier it
    /// derives depends on it.
    /// </summary>
    [Fact]
    public void TheClockCanBeFixed()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 3, 4, 9, 30, 0, TimeSpan.Zero));

        LifecycleStatusMessage message = FromPlatform().UsingClock(clock).Filed();

        message.StatusIssuedAt.Value.ShouldBe(clock.GetUtcNow());
        message.Identifier.Value!.ShouldContain("20260304093000");
    }

    private sealed class FakeClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public void AnIdentifierIsDerivedWhenTheCallerGivesNone()
    {
        LifecycleStatusMessage message = FromPlatform().Filed(Moment);

        message.Identifier.Value.ShouldBe("F202500003_200_20250701151000#380_20250701");
    }

    [Fact]
    public void ACallerCanImposeItsOwnIdentifier()
    {
        LifecycleStatusMessage message = FromPlatform().WithIdentifier("ACME-1").Filed(Moment);

        message.Identifier.Value.ShouldBe("ACME-1");
    }
}
