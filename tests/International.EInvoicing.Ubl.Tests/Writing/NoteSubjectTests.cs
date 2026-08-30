using System.Xml.Linq;
using International.EInvoicing.Configuration;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl.Reading;
using International.EInvoicing.Ubl.Writing;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Ubl.Tests.Writing;

/// <summary>
/// UBL has no element for the note subject code (BT-21): it is a prefix on the note text.
/// </summary>
/// <remarks>
/// CII gives it an element of its own, so the model does too — and a reader that took a UBL note at face
/// value both lost the code and kept a <c>#AAB#</c> nobody wants to display. France depends on this: three
/// of its mandatory mentions are identified by nothing but their code.
/// </remarks>
public class NoteSubjectTests
{
    private static UblInvoiceReader Reader() =>
        new(new EInvoicingOptions(), new ProfileResolver(new ProfileRegistry(KnownProfiles.All)));

    [Fact]
    public void ACodedNoteIsWrittenTheWayUblCarriesIt()
    {
        var invoice = new EInvoice { Number = "FA-1" };
        invoice.Notes.Add(new InvoiceNote { SubjectCode = "AAB", Text = "Escompte : néant." });

        XElement written = XElement.Parse(new UblInvoiceWriter().WriteToString(invoice));

        written.Elements(UblNames.Cbc + "Note").ShouldHaveSingleItem()
            .Value.ShouldBe("#AAB#Escompte : néant.");
    }

    [Fact]
    public void AnUncodedNoteIsWrittenAsItIs()
    {
        var invoice = new EInvoice { Number = "FA-1" };
        invoice.Notes.Add(new InvoiceNote { Text = "Merci de votre confiance." });

        XElement written = XElement.Parse(new UblInvoiceWriter().WriteToString(invoice));

        written.Elements(UblNames.Cbc + "Note").ShouldHaveSingleItem()
            .Value.ShouldBe("Merci de votre confiance.");
    }

    [Theory]
    [InlineData("#AAB#Escompte : néant.", "AAB", "Escompte : néant.")]
    [InlineData("#ADU#Nos conditions générales s'appliquent.", "ADU", "Nos conditions générales s'appliquent.")]
    [InlineData("Merci de votre confiance.", null, "Merci de votre confiance.")]
    [InlineData("#12#Not a subject code", null, "#12#Not a subject code")]
    public void ACodedNoteIsReadBackIntoItsTwoParts(string note, string? code, string text)
    {
        EInvoice invoice = Reader().Read(AnInvoiceWith(note)).Value!;
        InvoiceNote read = invoice.Notes.ShouldHaveSingleItem();

        read.SubjectCode.Value.ShouldBe(code);
        read.Text.Value.ShouldBe(text);
    }

    [Fact]
    public void ACodedNoteSurvivesTheRoundTrip()
    {
        EInvoice read = Reader().Read(AnInvoiceWith("#PMD#Pénalités de retard : taux légal.")).Value!;
        EInvoice again = Reader().Read(new UblInvoiceWriter().WriteToString(read)).Value!;

        again.Notes.ShouldHaveSingleItem().SubjectCode.Value.ShouldBe("PMD");
        again.Notes[0].Text.Value.ShouldBe("Pénalités de retard : taux légal.");
    }

    private static string AnInvoiceWith(string note) =>
        $"""
        <ubl:Invoice xmlns:ubl="{UblNames.Invoice}" xmlns:cbc="{UblNames.Cbc}">
          <cbc:CustomizationID>urn:cen.eu:en16931:2017</cbc:CustomizationID>
          <cbc:ID>FA-1</cbc:ID>
          <cbc:Note>{System.Security.SecurityElement.Escape(note)}</cbc:Note>
        </ubl:Invoice>
        """;
}
