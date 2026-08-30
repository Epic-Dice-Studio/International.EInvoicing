using International.EInvoicing.Building;
using International.EInvoicing.Countries.France;
using International.EInvoicing.Countries.France.Invoicing;
using International.EInvoicing.Countries.Germany;
using International.EInvoicing.Countries.Germany.Identifiers;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Samples.Chapters;

/// <summary>
/// The same invoice, for three countries — and what each of them asks for that EN 16931 does not.
/// </summary>
/// <remarks>
/// This is where a library either helps or gets in the way. Every one of these requirements is a rule
/// somebody's invoice was rejected by, and none of them is discoverable from the norm alone.
/// </remarks>
internal static class CountryInvoices
{
    public static void Run(EInvoicing einvoicing)
    {
        Report.Chapter("The same invoice, for three countries");

        France(einvoicing);
        Germany(einvoicing);
        Belgium(einvoicing);
    }

    /// <summary>France: an invoicing case, three mandatory mentions, and a SIREN on both sides.</summary>
    private static void France(EInvoicing einvoicing)
    {
        EInvoice invoice = Common(FrProfiles.ExtendedCtcFrUbl, "FA-2026-001", 20m)
            .ForFrance()                                                   // BT-23, and the three mentions
            .FromFrenchSeller("Fournisseur SARL", "732829320", "FR32732829320", Seller("Angers", "49000", "FR"))
            .ToFrenchBuyer("Client SA", "552081317", "FR89552081317", Buyer("Nantes", "44000", "FR"))
            .WithComputedVatBreakdown()
            .WithComputedTotals()
            .Build();

        Report.Say("France");
        Report.Fact("  invoicing case (BT-23)", invoice.BusinessProcessType.Value);

        foreach (InvoiceNote note in invoice.Notes)
        {
            Report.Fact($"  mention {note.SubjectCode.Value}", note.Text.Value?[..Math.Min(52, note.Text.Value.Length)]);
        }

        Report.Fact("  seller SIREN, check digit verified", invoice.Seller!.LegalRegistrationIdentifier.Value);
        Verdict(einvoicing, invoice, "  against every rule registered");
        Report.Note("The three mentions are required whatever else the invoice gets right: BR-FR-05.");
    }

    /// <summary>Germany: the routing identifier that decides which desk the invoice reaches.</summary>
    private static void Germany(EInvoicing einvoicing)
    {
        DeLeitwegId routing = DeLeitwegId.Create("04011000", "1234512345");

        EInvoice invoice = Common(DeProfiles.XRechnungUbl, "RE-2026-001", 19m)
            .WithBuyerReference(routing.ToString())                        // BT-10
            .From(seller => seller
                .Named("Epic Dice Studio GmbH")
                .WithVatIdentifier("DE123456789")
                .WithElectronicAddress("rechnung@example.de", "EM")
                .WithContact(contact =>
                {
                    contact.Name = "Rechnungsstelle";
                    contact.Telephone = "+49 30 123456";
                    contact.Email = "rechnung@example.de";
                })
                .WithAddress(address => Where(address, "Musterstraße 1", "Berlin", "10115", "DE")))
            .To(buyer => buyer
                .Named("Behörde")
                .WithElectronicAddress("buyer@example.de", "EM")
                .WithAddress(address => Where(address, "Amtsweg 2", "Bonn", "53113", "DE")))
            .Extend(document => document.Payment = new PaymentInstructions      // BR-DE-1 requires it
            {
                MeansTypeCode = "58",
                CreditTransfers = { new CreditTransfer { AccountIdentifier = "DE02120300000000202051" } },
            })
            .WithComputedVatBreakdown()
            .WithComputedTotals()
            .Build();

        Report.Say("Germany");
        Report.Fact("  Leitweg-ID (BT-10)", invoice.BuyerReference.Value);
        Report.Fact("  its check digits hold", DeLeitwegId.IsValid(invoice.BuyerReference.Value));
        Report.Fact("  payment instructions (BG-16)", invoice.Payment?.MeansTypeCode.Value);
        Verdict(einvoicing, invoice, "  against every rule registered");
        Report.Note("Wrong, an invoice is not rejected — it is delivered to another authority.");
    }

    /// <summary>Belgium: Peppol, which means a business process EN 16931 never asks for.</summary>
    private static void Belgium(EInvoicing einvoicing)
    {
        PeppolParticipant seller = PeppolParticipant.Create(PeppolEndpointScheme.BelgianEnterprise, "0203201340");

        EInvoice invoice = Common(PeppolProfiles.BillingUbl, "FA-2026-001", 21m)
            .ForPeppol()                                                   // BT-23, in Peppol's own shape
            .From(party => party
                .Named("Verkoper BV")
                .WithVatIdentifier("BE0203201340")
                .WithElectronicAddress(seller.Value, seller.Scheme)
                .WithAddress(address => Where(address, "Nijverheidsstraat 1", "Antwerpen", "2000", "BE")))
            .To(party => party
                .Named("Koper NV")
                .WithVatIdentifier("BE0776914174")
                .WithElectronicAddress("0776914174", PeppolEndpointScheme.BelgianEnterprise)
                .WithAddress(address => Where(address, "Havenlaan 2", "Brussel", "1000", "BE")))
            .WithComputedVatBreakdown()
            .WithComputedTotals()
            .Build();

        Report.Say("Belgium");
        Report.Fact("  business process (BT-23)", invoice.BusinessProcessType.Value);
        Report.Fact("  seller, as the network addresses it", seller.ToQualifiedString());
        Verdict(einvoicing, invoice, "  against every rule registered");
        Report.Note("Without the business process the invoice passes EN 16931 and Peppol rejects it.");
    }

    private static EInvoiceBuilder Common(Profile profile, string number, decimal vatRate) => EInvoiceBuilder
        .Create(profile)
        .WithNumber(number)
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .InCurrency("EUR")
        .WithBuyerReference("PO-4417")
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Conseil")
            .WithQuantity(3m, "HUR")
            .WithNetPrice(150m)
            .WithNetAmount(450m)
            .WithVat("S", vatRate));

    private static Action<PartyBuilder> Seller(string city, string postCode, string country) =>
        party => party.WithAddress(address => Where(address, "1 rue de la Facture", city, postCode, country));

    private static Action<PartyBuilder> Buyer(string city, string postCode, string country) =>
        party => party.WithAddress(address => Where(address, "8 avenue des Clients", city, postCode, country));

    private static void Where(PostalAddress address, string line, string city, string postCode, string country)
    {
        address.Line1 = line;
        address.City = city;
        address.PostCode = postCode;
        address.CountryCode = country;
    }

    private static void Verdict(EInvoicing einvoicing, EInvoice invoice, string label)
    {
        ValidationReport report = einvoicing.Validate(einvoicing.Write(invoice));

        Report.Fact(label, report.IsValid ? "accepted" : "rejected");

        foreach (ValidationMessage message in report.Errors.Take(3))
        {
            Report.Note($"{message.RuleIdentifier}: {message.Message}");
        }
    }
}
