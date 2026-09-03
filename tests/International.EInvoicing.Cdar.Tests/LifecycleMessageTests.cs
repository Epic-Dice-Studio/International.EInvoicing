using System.Xml.Linq;
using International.EInvoicing.Cdar.Reading;
using International.EInvoicing.Cdar.Writing;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Cdar.Tests;

/// <summary>
/// The sample below mirrors the structure of the DGFiP lifecycle test files, which are not redistributable
/// and so are not committed here — see <c>specs/fr-dse/PROVENANCE.md</c>. Element names, the profile
/// identifier and the status codes are the real ones.
/// </summary>
public class LifecycleMessageTests
{
    private static CdarReader Reader(params Profile[] registered) =>
        new(new EInvoicingOptions(), new ProfileResolver(new ProfileRegistry(registered)));

    private static string AFrenchStatusMessage(string profileId = "urn.cpro.gouv.fr:1p0:CDV:invoice") =>
        $"""
        <rsm:CrossDomainAcknowledgementAndResponse xmlns:rsm="{CdarNames.Rsm}" xmlns:ram="{CdarNames.Ram}"
                                                   xmlns:udt="{CdarNames.Udt}" xmlns:qdt="{CdarNames.Qdt}">
          <rsm:ExchangedDocumentContext>
            <ram:BusinessProcessSpecifiedDocumentContextParameter><ram:ID>REGULATED</ram:ID></ram:BusinessProcessSpecifiedDocumentContextParameter>
            <ram:GuidelineSpecifiedDocumentContextParameter><ram:ID>{profileId}</ram:ID></ram:GuidelineSpecifiedDocumentContextParameter>
          </rsm:ExchangedDocumentContext>
          <rsm:ExchangedDocument>
            <ram:ID>F202500003_200_20250701151000#380_20250701</ram:ID>
            <ram:Name>UC1_F202500003_01-CDV-200_Deposee</ram:Name>
            <ram:IssueDateTime><udt:DateTimeString format="204">20250701151500</udt:DateTimeString></ram:IssueDateTime>
            <ram:SenderTradeParty><ram:RoleCode>WK</ram:RoleCode></ram:SenderTradeParty>
            <ram:RecipientTradeParty>
              <ram:GlobalID schemeID="0002">100000009</ram:GlobalID>
              <ram:Name>VENDEUR</ram:Name>
              <ram:RoleCode>SE</ram:RoleCode>
              <ram:URIUniversalCommunication><ram:URIID schemeID="0225">100000009_STATUTS</ram:URIID></ram:URIUniversalCommunication>
            </ram:RecipientTradeParty>
            <ram:RecipientTradeParty>
              <ram:GlobalID schemeID="0238">9998</ram:GlobalID>
              <ram:Name>PPF</ram:Name>
              <ram:RoleCode>DFH</ram:RoleCode>
            </ram:RecipientTradeParty>
          </rsm:ExchangedDocument>
          <rsm:AcknowledgementDocument>
            <ram:MultipleReferencesIndicator><udt:Indicator>false</udt:Indicator></ram:MultipleReferencesIndicator>
            <ram:TypeCode>305</ram:TypeCode>
            <ram:IssueDateTime><udt:DateTimeString format="204">20250701151000</udt:DateTimeString></ram:IssueDateTime>
            <ram:ReferenceReferencedDocument>
              <ram:IssuerAssignedID>F202500003</ram:IssuerAssignedID>
              <ram:StatusCode>10</ram:StatusCode>
              <ram:TypeCode>380</ram:TypeCode>
              <ram:ReceiptDateTime><udt:DateTimeString format="204">20250701151000</udt:DateTimeString></ram:ReceiptDateTime>
              <ram:FormattedIssueDateTime><qdt:DateTimeString format="102">20250701</qdt:DateTimeString></ram:FormattedIssueDateTime>
              <ram:ProcessConditionCode>200</ram:ProcessConditionCode>
              <ram:ProcessCondition>Déposée</ram:ProcessCondition>
              <ram:IssuerTradeParty><ram:GlobalID schemeID="0002">100000009</ram:GlobalID></ram:IssuerTradeParty>
            </ram:ReferenceReferencedDocument>
          </rsm:AcknowledgementDocument>
        </rsm:CrossDomainAcknowledgementAndResponse>
        """;

    [Fact]
    public void AStatusMessageIsRead()
    {
        LifecycleStatusMessage message = Reader(CdarProfiles.FrenchLifecycleStatus)
            .Read(AFrenchStatusMessage()).Value!;

        message.Identifier.Value.ShouldBe("F202500003_200_20250701151000#380_20250701");
        message.TypeCode.Value.ShouldBe("305");
        message.CoversMultipleDocuments.Value.ShouldBe(false);
        message.Recipients.Count.ShouldBe(2);
        message.Recipients[1].Name.Value.ShouldBe("PPF");
    }

    [Fact]
    public void TheStatusItselfIsWhatTheMessageIsFor()
    {
        LifecycleStatusMessage message = Reader(CdarProfiles.FrenchLifecycleStatus)
            .Read(AFrenchStatusMessage()).Value!;

        ReferencedDocumentStatus status = message.References.ShouldHaveSingleItem();
        status.DocumentIdentifier.Value.ShouldBe("F202500003");
        status.ProcessConditionCode.Value.ShouldBe("200");
        status.ProcessCondition.Value.ShouldBe("Déposée");
        status.DocumentIssueDate.Value.ShouldBe(new DateOnly(2025, 7, 1));
    }

    [Fact]
    public void TimestampsKeepTheirFormatCodeAndTheirRawText()
    {
        LifecycleStatusMessage message = Reader(CdarProfiles.FrenchLifecycleStatus)
            .Read(AFrenchStatusMessage()).Value!;

        message.IssuedAt.Value.ShouldBe(new DateTimeOffset(2025, 7, 1, 15, 15, 0, TimeSpan.Zero));
        message.IssuedAt.FormatCode.ShouldBe("204");
        message.IssuedAt.Raw.ShouldBe("20250701151500");
    }

    [Fact]
    public void AnUnknownProfilingStillParsesAndTheDowngradeIsReported()
    {
        ParseResult<LifecycleStatusMessage> result = Reader(CdarProfiles.FrenchLifecycleStatus)
            .Read(AFrenchStatusMessage("urn:acme:lifecycle:2p0"));

        result.IsUsable.ShouldBeTrue();
        result.Value!.References.ShouldHaveSingleItem().ProcessConditionCode.Value.ShouldBe("200");
        result.Value.Profile!.IsExact.ShouldBeFalse();
        result.Value.Profile.AllowsCompleteValidation.ShouldBeFalse();

        Diagnostic downgrade = result.Diagnostics.Single(d => d.Code == "EIV1042");
        downgrade.BusinessTerm.ShouldBe("BT-24");
        downgrade.AppliedFallback!.ShouldContain("generic cdar reading");
    }

    [Fact]
    public void NothingIsLostOnTheWayBackOut()
    {
        string original = AFrenchStatusMessage();
        LifecycleStatusMessage message = Reader(CdarProfiles.FrenchLifecycleStatus).Read(original).Value!;

        string written = new CdarWriter().WriteToString(message);

        Dictionary<string, int> before = Count(XElement.Parse(original));
        Dictionary<string, int> after = Count(XElement.Parse(written));

        string[] lost = [.. before
            .Where(pair => after.GetValueOrDefault(pair.Key) < pair.Value)
            .Select(pair => pair.Key)
            .Order(StringComparer.Ordinal)];

        lost.ShouldBeEmpty($"lost: {string.Join(", ", lost)}");
    }

    [Fact]
    public void AMessageWrittenBackIsReadTheSameWay()
    {
        CdarReader reader = Reader(CdarProfiles.FrenchLifecycleStatus);
        LifecycleStatusMessage original = reader.Read(AFrenchStatusMessage()).Value!;

        LifecycleStatusMessage again = reader.Read(new CdarWriter().WriteToString(original)).Value!;

        again.Identifier.Value.ShouldBe(original.Identifier.Value);
        again.IssuedAt.Value.ShouldBe(original.IssuedAt.Value);
        again.References[0].ProcessConditionCode.Value.ShouldBe("200");
        again.Recipients.Count.ShouldBe(2);
    }

    private static Dictionary<string, int> Count(XElement root)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (XElement element in root.DescendantsAndSelf())
        {
            string key = element.Name.ToString();
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        return counts;
    }
}
