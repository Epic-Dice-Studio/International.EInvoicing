using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Testing;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Testing.Tests;

/// <summary>
/// The test kit, tested.
/// </summary>
/// <remarks>
/// A kit that reports a passing document as failing costs an integrator a day; one that reports a failing
/// document as passing costs them a rejected invoice. So both directions are pinned here, and the samples are
/// checked against the actual EN 16931 artefact rather than against our opinion of it.
/// </remarks>
public class TestKitTests
{
    [Fact]
    public void TheSampleInvoiceConformsToEn16931()
    {
        EInvoicing library = EInvoicing.CreateDefault();

        ValidationReport report = library.Validate(library.Write(SampleInvoices.Conforming(), DocumentFormat.Ubl));

        Expect.Conforming(report);
    }

    [Fact]
    public void AndSoDoesTheSampleCreditNote()
    {
        EInvoicing library = EInvoicing.CreateDefault();

        ValidationReport report =
            library.Validate(library.Write(SampleInvoices.ConformingCreditNote(), DocumentFormat.Ubl));

        Expect.Conforming(report);
    }

    [Fact]
    public void TheSampleConformsInCiiToo()
    {
        EInvoicing library = EInvoicing.CreateDefault();
        EInvoice invoice = SampleInvoices.Conforming(KnownProfiles.En16931Cii.Id);

        Expect.Conforming(library.Validate(library.Write(invoice, DocumentFormat.Cii)));
    }

    [Fact]
    public void ARoundTripOfTheSampleLosesNothing()
    {
        EInvoicing library = EInvoicing.CreateDefault();

        Expect.LostNothing(RoundTrip.Check(library, SampleInvoices.Conforming(), DocumentFormat.Ubl));
        Expect.LostNothing(RoundTrip.Check(
            library,
            SampleInvoices.Conforming(KnownProfiles.En16931Cii.Id),
            DocumentFormat.Cii));
    }

    /// <summary>The element with no business term is the one a naive round trip drops.</summary>
    [Fact]
    public void IncludingTheOneTheModelHasNoFieldFor()
    {
        EInvoicing library = EInvoicing.CreateDefault();
        string document = library.Write(SampleInvoices.WithSomethingUnmapped(), DocumentFormat.Ubl);

        RoundTripResult result = RoundTrip.Check(library, document);

        Expect.LostNothing(result);
        result.Written.ShouldContain("house:Approval");
    }

    /// <summary>The promise the whole corpus exists for.</summary>
    [Theory]
    [MemberData(nameof(Hostile))]
    public void NoDocumentInTheHostileCorpusThrows(string name)
    {
        HostileDocument document = HostileDocuments.All.Single(candidate => candidate.Name == name);
        EInvoicing library = EInvoicing.CreateDefault();

        DocumentResult result = library.Read(document.Bytes);

        result.IsUsable.ShouldBe(document.StaysUsable, $"{document.Name}: {document.What}");

        if (document.ExpectedDiagnostic is { } code)
        {
            Expect.Reported(result, code);
        }
    }

    /// <summary>A document that survives reading survives being written back, too.</summary>
    [Theory]
    [MemberData(nameof(SurvivableHostile))]
    public void AndTheSurvivableOnesCanBeWrittenBack(string name)
    {
        HostileDocument document = HostileDocuments.All.Single(candidate => candidate.Name == name);
        EInvoicing library = EInvoicing.CreateDefault();

        Expect.Usable(library.Read(document.Bytes));
    }

    [Fact]
    public void ExpectConformingFailsWhenARuleSetDidNotRun()
    {
        var report = new ValidationReport([], [new RuleSetOutcome("Something", "1.0", Ran: false, "not fetched")]);

        EInvoicingAssertionException failure =
            Should.Throw<EInvoicingAssertionException>(() => Expect.Conforming(report));

        failure.Message.ShouldContain("not fetched", Case.Sensitive);
    }

    [Fact]
    public void ExpectFailedSaysWhatDidFireWhenTheRuleDidNot()
    {
        var report = new ValidationReport(
            [new ValidationMessage("BR-01", RuleSeverity.Error, "something else broke")],
            []);

        Should.NotThrow(() => Expect.Failed(report, "BR-01"));

        EInvoicingAssertionException failure =
            Should.Throw<EInvoicingAssertionException>(() => Expect.Failed(report, "BR-99"));

        failure.Message.ShouldContain("BR-01");
    }

    [Fact]
    public void ExpectReportedNamesWhatWasReportedInstead()
    {
        EInvoicing library = EInvoicing.CreateDefault();
        DocumentResult result = library.Read(HostileDocuments.All.Single(d => d.Name == "profile-nobody-registered").Xml);

        Should.NotThrow(() => Expect.Reported(result, "EIV1042"));

        EInvoicingAssertionException failure =
            Should.Throw<EInvoicingAssertionException>(() => Expect.Reported(result, "EIV9999"));

        failure.Message.ShouldContain("EIV1042");
    }

    /// <summary>The raw text is the promise; the typed value is a convenience over it.</summary>
    /// <summary>
    /// The one a naive reader gets wrong in the only field a human looks at.
    /// </summary>
    /// <remarks>
    /// A sender whose database is Latin-1 and whose template says UTF-8 is not a rare event. Decoding as
    /// UTF-8 regardless turns Müller into M?ller: the document validates, arrives, and is wrong.
    /// </remarks>
    [Fact]
    public void AMisDeclaredEncodingIsNoticedRatherThanMangled()
    {
        HostileDocument document = HostileDocuments.All.Single(d => d.Name == "declares-utf8-and-sends-latin1");
        EInvoicing library = EInvoicing.CreateDefault();

        DocumentResult result = library.Read(document.Bytes);

        Expect.Reported(result, "EIV5002");
        result.RequireInvoice().Buyer!.Name.Value.ShouldBe("Müller und Söhne");
    }

    /// <summary>Text handed over as text is not second-guessed: the caller already decoded it.</summary>
    [Fact]
    public void AndTextHandedOverAsTextIsLeftAlone()
    {
        EInvoicing library = EInvoicing.CreateDefault();
        HostileDocument document = HostileDocuments.All.Single(d => d.Name == "declares-utf8-and-sends-latin1");

        DocumentResult result = library.Read(document.Xml);

        result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "EIV5002");
        result.RequireInvoice().Buyer!.Name.Value.ShouldBe("Müller und Söhne");
    }

    [Fact]
    public void ExpectRawChecksTheTextTheDocumentActuallyCarried()
    {
        EInvoicing library = EInvoicing.CreateDefault();
        HostileDocument document = HostileDocuments.All.Single(d => d.Name == "date-in-a-format-nobody-agreed-to");

        EInvoice invoice = library.Read(document.Xml).RequireInvoice();

        Expect.Raw(invoice.IssueDate, "le 1er septembre");
        invoice.IssueDate.IsRawOnly.ShouldBeTrue();
    }

    public static TheoryData<string> Hostile()
    {
        TheoryData<string> data = [];

        foreach (HostileDocument document in HostileDocuments.All)
        {
            data.Add(document.Name);
        }

        return data;
    }

    public static TheoryData<string> SurvivableHostile()
    {
        TheoryData<string> data = [];

        foreach (HostileDocument document in HostileDocuments.Survivable)
        {
            data.Add(document.Name);
        }

        return data;
    }
}
