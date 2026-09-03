using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol.TaxData;
using International.EInvoicing.Peppol.TaxData.Model;
using International.EInvoicing.Peppol.TaxData.Reading;
using International.EInvoicing.Peppol.TaxData.Writing;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Peppol.Tests;

/// <summary>
/// The receiver's side of tax reporting, which is the tax authority's side.
/// </summary>
/// <remarks>
/// This library reads back everything it writes — that parity is what lets an integrator test their own
/// output, and what lets a receiver use the same library as the sender. The tax data document was the one
/// place it did not hold: written and judged, never read. It holds now.
/// </remarks>
public class PeppolTaxDataReadingTests
{
    [Theory]
    [InlineData("SK")]
    [InlineData("ViDA")]
    public void ADocumentThisLibraryWritesComesBackAsTheOneThatWentIn(string jurisdiction)
    {
        PeppolTaxData written = ATaxDataDocument(Of(jurisdiction));

        PeppolTaxData read = Read(new PeppolTaxDataWriter().WriteToString(written));

        read.Jurisdiction.ShouldBe(written.Jurisdiction);
        read.Uuid.ShouldBe(written.Uuid);
        read.IssuedAt.ShouldBe(written.IssuedAt);
        read.TaxDataTypeCode.ShouldBe(written.TaxDataTypeCode);
        read.DocumentScope.ShouldBe(written.DocumentScope);
        read.ReporterRole.ShouldBe(written.ReporterRole);
        read.Authority.Id.ShouldBe(written.Authority.Id);
        read.ReportingParty.Id.ShouldBe(written.ReportingParty.Id);
        read.ReportingParty.SchemeId.ShouldBe(written.ReportingParty.SchemeId);
        read.ReceivingParty.Id.ShouldBe(written.ReceivingParty.Id);
        read.ReportedDocumentUuid.ShouldBe(written.ReportedDocumentUuid);
    }

    /// <summary>
    /// The reported document is a projection of an invoice, and comes back as one.
    /// </summary>
    /// <remarks>
    /// It is read by the UBL invoice reader, not by a second mapping written beside it — the projection
    /// renames three elements and is otherwise UBL as published. So a term the invoice reader maps is a term
    /// a tax authority gets back.
    /// </remarks>
    [Fact]
    public void AndTheReportedDocumentComesBackAsAnInvoice()
    {
        PeppolTaxData written = ATaxDataDocument(PeppolTaxDataJurisdiction.Slovakia);
        EInvoice sent = written.ReportedDocument.ShouldNotBeNull();

        EInvoice read = Read(new PeppolTaxDataWriter().WriteToString(written))
            .ReportedDocument.ShouldNotBeNull();

        read.Number.Value.ShouldBe(sent.Number.Value);
        read.IssueDate.Value.ShouldBe(sent.IssueDate.Value);
        read.TypeCode.Value.ShouldBe(sent.TypeCode.Value);
        read.CurrencyCode.Value.ShouldBe(sent.CurrencyCode.Value);
        // The projection identifies the seller by VAT number and country, and carries no name for them:
        // the rules define no cac:PartyLegalEntity under the supplier, so writing one makes the document
        // fail. The buyer does have one. Reading gives back exactly what was reported, not what was invoiced.
        read.Seller.ShouldNotBeNull().VatIdentifier.Value.ShouldBe(sent.Seller!.VatIdentifier.Value);
        read.Seller.Name.Value.ShouldBeNull("the tax data projection reports no supplier name");
        read.Buyer.ShouldNotBeNull().Name.Value.ShouldBe(sent.Buyer!.Name.Value);
        read.Totals.DuePayableAmount.Value.ShouldBe(sent.Totals.DuePayableAmount.Value);
        read.Lines.Count.ShouldBe(sent.Lines.Count);
        read.Lines[0].Item.ShouldNotBeNull().Name.Value.ShouldBe(sent.Lines[0].Item!.Name.Value);
    }

    /// <summary>A jurisdiction this library does not carry is read, and the downgrade is said out loud.</summary>
    [Fact]
    public void AJurisdictionThisLibraryDoesNotKnowIsStillReadAndReported()
    {
        string xml = new PeppolTaxDataWriter()
            .WriteToString(ATaxDataDocument(PeppolTaxDataJurisdiction.Slovakia))
            .Replace("urn:peppol:taxdata:sk-1", "urn:peppol:taxdata:xx-1", StringComparison.Ordinal);

        ParseResult<PeppolTaxData> result = new PeppolTaxDataReader(new(), Resolver()).Read(xml);

        result.Value.ShouldNotBeNull().Jurisdiction.CustomizationId.ShouldBe("urn:peppol:taxdata:xx-1");
        result.Value.ReportedDocument.ShouldNotBeNull();
        result.Diagnostics.Select(diagnostic => diagnostic.Code)
            .ShouldContain(DiagnosticCodes.UnknownProfile.Code);
    }

    /// <summary>Reading never throws on the document, whatever arrives.</summary>
    [Fact]
    public void ADocumentThatIsNotWellFormedIsReportedRatherThanRaised()
    {
        ParseResult<PeppolTaxData> result = new PeppolTaxDataReader(new(), Resolver())
            .Read("""<pxs:TaxData xmlns:pxs="urn:peppol:schema:sk-taxdata:1.0">""");

        result.Value.ShouldBeNull();
        result.Diagnostics.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("""<pxs:TaxData xmlns:pxs="urn:peppol:schema:sk-taxdata:1.0"/>""", true)]
    [InlineData("""<Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"/>""", false)]
    [InlineData("not xml at all", false)]
    public void ATaxDataDocumentIsToldFromEverythingElseByItsRoot(string xml, bool expected) =>
        PeppolTaxDataReader.LooksLikeTaxData(xml).ShouldBe(expected);

    private static PeppolTaxData Read(string xml) =>
        new PeppolTaxDataReader(new(), Resolver()).Read(xml).Value.ShouldNotBeNull();

    private static Profiles.ProfileResolver Resolver() =>
        new(new Profiles.ProfileRegistry(Profiles.KnownProfiles.All));

    private static PeppolTaxDataJurisdiction Of(string name) =>
        name == "SK" ? PeppolTaxDataJurisdiction.Slovakia : PeppolTaxDataJurisdiction.ViDA;

    private static PeppolTaxData ATaxDataDocument(PeppolTaxDataJurisdiction jurisdiction) => new()
    {
        Jurisdiction = jurisdiction,
        Uuid = "8f1d1c26-0f27-4c6f-9c7c-5f9f7b1f0f01",
        IssuedAt = new DateTimeOffset(2027, 1, 15, 9, 30, 0, TimeSpan.Zero),
        TaxDataTypeCode = "S",
        DocumentScope = "D",
        ReporterRole = "C2",
        Authority = new PeppolTaxAuthority { Id = "9915:sk-fs", Name = "Finančná správa" },
        ReportingParty = new PeppolTaxDataEndpoint { Id = "0242:sp-1", SchemeId = PeppolTaxDataEndpoint.ServiceProviderScheme },
        ReceivingParty = new PeppolTaxDataEndpoint { Id = "0242:sp-2", SchemeId = PeppolTaxDataEndpoint.ServiceProviderScheme },
        ReportedDocumentUuid = "3d0a3a58-6f5a-4d0f-9c67-2f6f7a5f9a10",
        ReportedDocument = AnInvoice(),
    };

    private static EInvoice AnInvoice() => Building.EInvoiceBuilder
        .Create(Profiles.KnownProfiles.PeppolBisBilling3Ubl)
        .WithNumber("2027-0001")
        .IssuedOn(new DateOnly(2027, 1, 14))
        .OfType(InvoiceTypeCodes.CommercialInvoice)
        .InCurrency("EUR")
        .From(seller => seller.Named("Vendeur SK").WithVatIdentifier("SK2020317068"))
        .To(buyer => buyer.Named("Odberateľ s.r.o."))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Consulting")
            .WithQuantity(2m, "HUR")
            .WithNetPrice(500m)
            .WithNetAmount(1000m)
            .WithVat(VatCategoryCodes.Standard, 20m))
        .Extend(invoice => invoice.Totals.DuePayableAmount = 1200m)
        .Build();
}
