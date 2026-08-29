using System.Xml.Linq;
using International.EInvoicing.Cdar;
using International.EInvoicing.Cdar.Model;
using International.EInvoicing.Cdar.Reading;
using International.EInvoicing.Cdar.Writing;
using International.EInvoicing.Configuration;
using International.EInvoicing.Countries.France.Lifecycle;
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

    private static FrCdar ToPartner() =>
        FrCdar.ToPartner(to => to
                .Company("100000009")
                .Named("VENDEUR")
                .AsSeller()
                .ReachableAt("100000009_STATUTS"))
            .From(from => from.Platform("0003", "PA-E Vendeur"))
            .About("F202500003", new DateOnly(2025, 7, 1));

    private static string Write(LifecycleStatusMessage message) => new CdarWriter().WriteToString(message);

    [Fact]
    public void NamingAStatusFillsInTheCodesItImplies()
    {
        LifecycleStatusMessage message = ToPartner().Filed(Moment);

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
        LifecycleStatusMessage message = ToPartner().Refused("TX_TVA_ERR", "Taux de TVA erroné", Moment);

        XElement written = XElement.Parse(Write(message));
        XElement status = written.Descendants(CdarNames.Ram + "SpecifiedDocumentStatus").ShouldHaveSingleItem();

        status.Element(CdarNames.Ram + "ReasonCode")!.Value.ShouldBe("TX_TVA_ERR");
        status.Element(CdarNames.Ram + "Reason")!.Value.ShouldBe("Taux de TVA erroné");
        status.Parent!.Name.LocalName.ShouldBe("ReferenceReferencedDocument");
    }

    [Fact]
    public void AStatusNeedingAReasonWillNotBeBuiltWithout()
    {
        Should.Throw<ArgumentException>(() => ToPartner().Refused(string.Empty, "Taux de TVA erroné"));
        Should.Throw<ArgumentException>(() => ToPartner().Disputed("TX_TVA_ERR", string.Empty));
        FrLifecycleStatus.Refused.RequiresReason.ShouldBeTrue();
        FrLifecycleStatus.Approved.RequiresReason.ShouldBeFalse();
    }

    [Fact]
    public void SendingToAPartnerAddressesThePublicPortalToo()
    {
        LifecycleStatusMessage message = ToPartner().Filed(Moment);

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
        LifecycleStatusMessage message = FrCdar.ToPublicPortal()
            .From(from => from.Platform("0003", "PA-E Vendeur"))
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
        LifecycleStatusMessage built = ToPartner().Approved(Moment);

        var reader = new CdarReader(
            new EInvoicingOptions(),
            new ProfileResolver(new ProfileRegistry(FrProfiles.All)));

        LifecycleStatusMessage read = reader.Read(Write(built)).Value!;

        read.Profile!.IsExact.ShouldBeTrue();
        read.References[0].ProcessConditionCode.Value.ShouldBe("205");
        read.References[0].ProcessCondition.Value.ShouldBe("Approuvée");
        read.StatusIssuedAt.Value.ShouldBe(Moment);
    }

    [Fact]
    public void AnIdentifierIsDerivedWhenTheCallerGivesNone()
    {
        LifecycleStatusMessage message = ToPartner().Filed(Moment);

        message.Identifier.Value.ShouldBe("F202500003_200_20250701151000#380_20250701");
    }

    [Fact]
    public void ACallerCanImposeItsOwnIdentifier()
    {
        LifecycleStatusMessage message = ToPartner().WithIdentifier("ACME-1").Filed(Moment);

        message.Identifier.Value.ShouldBe("ACME-1");
    }
}
