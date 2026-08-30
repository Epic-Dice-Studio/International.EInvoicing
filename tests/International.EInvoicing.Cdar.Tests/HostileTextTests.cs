using System.Xml.Linq;
using International.EInvoicing.Cdar.Model;
using International.EInvoicing.Cdar.Writing;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Cdar.Tests;

/// <summary>
/// What a caller passes in comes from their system, not from a specification.
/// </summary>
/// <remarks>
/// Every mature library in this space has had the same report: a description with a stray control character
/// in it, and a writer that fails with "hexadecimal value 0x07, is an invalid character" — which names
/// neither the field nor anything the caller can act on. XML cannot carry those characters at all, so they
/// are dropped and everything else is written as it was.
/// </remarks>
public class HostileTextTests
{
    private const char Bell = '\u0007';

    [Fact]
    public void ACharacterXmlCannotCarryDoesNotStopTheDocumentBeingWritten()
    {
        var message = new LifecycleStatusMessage
        {
            Identifier = "F202500003",
            Name = $"Facture{Bell} n°1",
        };

        message.References.Add(new ReferencedDocumentStatus { DocumentIdentifier = "F202500003" });

        XElement written = XElement.Parse(new CdarWriter().WriteToString(message));

        written.Descendants(CdarNames.Ram + "Name").ShouldHaveSingleItem().Value.ShouldBe("Facture n°1");
    }

    [Fact]
    public void AnAttributeIsCleanedToo()
    {
        var message = new LifecycleStatusMessage
        {
            Identifier = new Values.IdentifierField("F202500003", $"0002{Bell}"),
        };

        XElement written = XElement.Parse(new CdarWriter().WriteToString(message));

        written.Descendants(CdarNames.Ram + "ID").ShouldHaveSingleItem()
            .Attribute("schemeID")!.Value.ShouldBe("0002");
    }

    [Fact]
    public void AccentsAndSymbolsAreLeftAlone()
    {
        var message = new LifecycleStatusMessage { Name = "Prestation — 3 × 4 m², à l'unité 😀" };

        XElement written = XElement.Parse(new CdarWriter().WriteToString(message));

        written.Descendants(CdarNames.Ram + "Name").ShouldHaveSingleItem()
            .Value.ShouldBe("Prestation — 3 × 4 m², à l'unité 😀");
    }
}
