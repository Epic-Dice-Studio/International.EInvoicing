using International.EInvoicing.Building;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Belgium.Tests;

/// <summary>
/// Belgium mandates Peppol BIS Billing rather than a Belgian format, so the shortcut's job is to make that
/// fact easy to act on: the profile, the business process the network requires and EN 16931 does not, and the
/// enterprise number in the scheme Peppol reserves for it.
/// </summary>
public class BelgianEInvoicingTests
{
    private static readonly BelgianEInvoicing Belgium = BelgianEInvoicing.Create();

    [Fact]
    public void AnInvoiceBuiltHereDeclaresPeppolBillingAndItsBusinessProcess()
    {
        EInvoice invoice = AnInvoice();

        invoice.SpecificationIdentifier.Value.ShouldBe(PeppolProfiles.BillingUbl.Id.Value);
        invoice.BusinessProcessType.Value.ShouldBe(PeppolBusinessProcess.Billing);
    }

    [Fact]
    public void ACreditNoteIsAnInvoiceOfACreditNoteType()
    {
        EInvoice creditNote = Belgium.CreditNote().Build();

        InvoiceTypeCodes.IsCreditNote(creditNote.TypeCode.Value).ShouldBeTrue();
    }

    /// <summary>
    /// A Belgian party is addressed by its enterprise number, and a wrong one is a wrong invoice — so it is
    /// checked here rather than by the receiving access point.
    /// </summary>
    [Fact]
    public void AnEnterpriseNumberIsCheckedAndWrittenInTheSchemePeppolReservesForIt()
    {
        EInvoice invoice = AnInvoice();

        invoice.Seller!.ElectronicAddress.Value.ShouldBe("0776914174");
        invoice.Seller.ElectronicAddress.SchemeId.ShouldBe("0208");
        invoice.Seller.VatIdentifier.Value.ShouldBe("BE0776914174");

        Should.Throw<FormatException>(
            () => Belgium.Invoice().From(seller => Belgium.Describe(seller, "0776914151", "Fout BV")));
    }

    [Fact]
    public void TheStructuredCommunicationIsTheOneABelgianBankWillAccept()
    {
        string communication = Belgium.StructuredCommunication(123456789);

        communication.ShouldStartWith("+++");
        Identifiers.BeStructuredCommunication.IsValid(communication).ShouldBeTrue();
    }

    [Fact]
    public void WhatItWritesItReadsBack()
    {
        DocumentResult read = Belgium.Read(Belgium.Write(AnInvoice()));

        read.RequireInvoice().Number.Value.ShouldBe("2026-0001");
    }

    [Fact]
    public void TheWholeLibraryStaysReachable() => Belgium.Library.Ubl.Syntax.ShouldBe(DocumentSyntax.Ubl);

    private static EInvoice AnInvoice() => Belgium.Invoice()
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .From(seller => Belgium.Describe(seller, "0776914174", "Epic Dice Studio BV")
            .WithAddress(address =>
            {
                address.Line1 = "Grote Markt 1";
                address.City = "Brussel";
                address.PostCode = "1000";
                address.CountryCode = "BE";
            }))
        .To(buyer => buyer
            .Named("Klant NV")
            .WithVatIdentifier("BE0403170701")
            .WithElectronicAddress("0403170701", PeppolEndpointScheme.BelgianEnterprise)
            .WithAddress(address =>
            {
                address.Line1 = "Meir 2";
                address.City = "Antwerpen";
                address.PostCode = "2000";
                address.CountryCode = "BE";
            }))
        .AddLine(line => line.WithItem("Advies").WithNetAmount(1000m).WithVat("S", 21m))
        .WithComputedVatBreakdown()
        .WithComputedTotals()
        .Build();
}
