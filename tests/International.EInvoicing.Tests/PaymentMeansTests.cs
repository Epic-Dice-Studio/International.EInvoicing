using System.Xml.Linq;
using International.EInvoicing.Building;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Tests;

/// <summary>
/// An invoice that offers two accounts to pay into.
/// </summary>
/// <remarks>
/// <para>
/// Both syntaxes allow <b>one account per payment means</b> and repeat the whole block when there are two —
/// which is what EN 16931's own examples do: <c>ubl-tc434-example1</c> and <c>guide-example1</c> each carry
/// two <c>cac:PaymentMeans</c> and two <c>cac:PayeeFinancialAccount</c>.
/// </para>
/// <para>
/// This library used to write both accounts into one block, and to read only the first block back. The first
/// produces a document no schema accepts and no Schematron rule complains about; the second silently drops an
/// account the sender meant you to be able to pay into. Neither showed up until the neighbours' test corpora
/// were read — mustangproject keeps a <c>multiple-payment-means</c> case for exactly this.
/// </para>
/// </remarks>
public class PaymentMeansTests
{
    private static readonly EInvoicing Library = EInvoicing.Create(builder => builder.AddDefaults());

    [Theory]
    [InlineData("UBL")]
    [InlineData("CII")]
    public void TwoAccountsAreTwoPaymentMeansBlocks(string syntax)
    {
        // BT-24 is the same string in both syntaxes for EN 16931, so the format is named rather than guessed.
        DocumentFormat format = syntax == "UBL" ? DocumentFormat.Ubl : DocumentFormat.Cii;
        XElement root = XElement.Parse(Library.Write(AnInvoiceWithTwoAccounts(format), format));

        (string block, string account) = syntax == "UBL"
            ? ("PaymentMeans", "PayeeFinancialAccount")
            : ("SpecifiedTradeSettlementPaymentMeans", "PayeePartyCreditorFinancialAccount");

        List<XElement> blocks = [.. root.Descendants().Where(e => e.Name.LocalName == block)];

        blocks.Count.ShouldBe(2);

        foreach (XElement one in blocks)
        {
            one.Descendants().Count(e => e.Name.LocalName == account).ShouldBe(1);
        }
    }

    [Theory]
    [InlineData("UBL")]
    [InlineData("CII")]
    public void AndBothComeBackWhenItIsReadAgain(string syntax)
    {
        DocumentFormat format = syntax == "UBL" ? DocumentFormat.Ubl : DocumentFormat.Cii;
        EInvoice invoice = AnInvoiceWithTwoAccounts(format);

        EInvoice read = Library.Read(Library.Write(invoice, format)).RequireInvoice();

        read.Payment!.CreditTransfers.Count.ShouldBe(2);
        read.Payment.CreditTransfers[0].AccountIdentifier.Value.ShouldBe("FR7630006000011234567890189");
        read.Payment.CreditTransfers[1].AccountIdentifier.Value.ShouldBe("DE02120300000000202051");
        read.Payment.MeansTypeCode.Value.ShouldBe("30");
    }

    /// <summary>The proof it is the norm's shape and not ours: its own example, read here.</summary>
    [Fact]
    public void TheOfficialExampleWithTwoAccountsReadsAsTwo()
    {
        string path = Path.Combine(
            RepositoryRoot(), "specs", "en16931", "ubl", "examples", "ubl-tc434-example1.xml");

        Assert.SkipWhen(!File.Exists(path), "run build/fetch-specs.sh en16931");

        EInvoice invoice = Library.Read(File.ReadAllText(path)).RequireInvoice();

        invoice.Payment!.CreditTransfers.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData("UBL")]
    [InlineData("CII")]
    public void AndAnInvoiceWithNoAccountStillStatesHowItIsPaid(string syntax)
    {
        DocumentFormat format = syntax == "UBL" ? DocumentFormat.Ubl : DocumentFormat.Cii;
        EInvoice invoice = AnInvoiceWithTwoAccounts(format);
        invoice.Payment!.CreditTransfers.Clear();

        EInvoice read = Library.Read(Library.Write(invoice, format)).RequireInvoice();

        read.Payment!.MeansTypeCode.Value.ShouldBe("30");
        read.Payment.CreditTransfers.ShouldBeEmpty();
    }

    private static EInvoice AnInvoiceWithTwoAccounts(DocumentFormat format) => EInvoiceBuilder
        .Create(format == DocumentFormat.Ubl ? KnownProfiles.En16931Ubl : KnownProfiles.En16931Cii)
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType(InvoiceTypeCodes.CommercialInvoice)
        .InCurrency("EUR")
        .WithBuyerReference("REF-2026-0001")
        .From(seller => seller
            .Named("Vendeur SAS")
            .WithVatIdentifier("FR40303265045")
            .WithElectronicAddress("seller@example.fr", "EM")
            .WithAddress(address =>
            {
                address.Line1 = "1 rue de la Paix";
                address.City = "Paris";
                address.PostCode = "75002";
                address.CountryCode = "FR";
            }))
        .To(buyer => buyer
            .Named("Acheteur GmbH")
            .WithElectronicAddress("buyer@example.de", "EM")
            .WithAddress(address =>
            {
                address.Line1 = "Musterstraße 1";
                address.City = "Berlin";
                address.PostCode = "10115";
                address.CountryCode = "DE";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Prestation")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m)
            .WithVat("S", 20m))
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",
            CreditTransfers =
            {
                new CreditTransfer { AccountIdentifier = "FR7630006000011234567890189" },
                new CreditTransfer { AccountIdentifier = "DE02120300000000202051" },
            },
        })
        .WithComputedVatBreakdown()
        .WithComputedTotals()
        .Build();

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "International.EInvoicing.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
    }
}
