using International.EInvoicing.Countries.France.Lifecycle;
using International.EInvoicing.Model;

namespace International.EInvoicing.Samples.Chapters;

/// <summary>
/// French lifecycle statuses — the CDAR messages the 2026 reform makes mandatory between platforms.
/// </summary>
/// <remarks>
/// A message has three parties and it is easy to fill in the wrong one, so the builder reads as the
/// sentence: who reports the status, through which platform, to whom. Where you start fixes the roles, the
/// destination fixes the profile, and the status fixes the codes behind it.
/// </remarks>
internal static class FrenchLifecycle
{
    private const string SellerSiren = "100000009";
    private const string BuyerSiren = "200000008";

    public static void Run(EInvoicing einvoicing)
    {
        Report.Chapter("French lifecycle statuses");

        APlatformFilesAnInvoice(einvoicing);
        ABuyerRefusesOne(einvoicing);
        ASellerReportsCollection(einvoicing);
        TheWrongWayRound();
    }

    /// <summary>A platform event: the platform reports on its own behalf, so it is issuer and sender both.</summary>
    private static void APlatformFilesAnInvoice(EInvoicing einvoicing)
    {
        LifecycleStatusMessage filed = FrCdar
            .FromPlatform("0003", "PA-E Vendeur")
            .ToSeller(SellerSiren, "VENDEUR", "100000009_STATUTS")
            .About("F202500003", new DateOnly(2025, 7, 1))
            .Filed(new DateTimeOffset(2025, 7, 1, 15, 10, 0, TimeSpan.Zero));

        Report.Fact("status", filed.References[0].ProcessConditionCode.Value);
        Report.Fact("acknowledgement type it implies", filed.TypeCode.Value);
        Report.Fact("referenced document status", filed.References[0].StatusCode.Value);
        Report.Fact("identifier derived for you", filed.Identifier.Value);
        Report.Fact("recipients", filed.Recipients.Count);
        Report.Note("Sending to a partner addresses the public portal too; that profile expects it.");
        Report.Snippet(einvoicing.Write(filed), lines: 7);
    }

    /// <summary>A business event: a trading party reports it, and its platform transmits it.</summary>
    private static void ABuyerRefusesOne(EInvoicing einvoicing)
    {
        LifecycleStatusMessage refused = FrCdar
            .FromBuyer(BuyerSiren, "ACHETEUR")
            .SentBy("0003", "PA-E Acheteur")
            .ToSeller(SellerSiren, "VENDEUR", "100000009_STATUTS")
            .About("F202500003", new DateOnly(2025, 7, 1))
            .Refused(
                FrStatusReason.VatRateWrong,
                "Taux de TVA erroné",
                requestedActionCode: FrRequestedAction.CorrectiveInvoice,
                requestedAction: "Créer une facture rectificative");

        DocumentStatusDetail detail = refused.References[0].StatusDetails[0];

        Report.Fact("status", refused.References[0].ProcessConditionCode.Value);
        Report.Fact("reason code", detail.ReasonCode.Value);
        Report.Fact("action requested", detail.RequestedActionCode.Value);
        Report.Fact("detail numbered", detail.SequenceNumber.Value);
        Report.Fact("reasons a refusal accepts", FrStatusReason.AllowedFor(FrLifecycleStatus.Refused).Count);
        Report.Note("Public-sector refusals accept seven more: AllowedFor(status, publicSector: true).");

        DocumentResult read = einvoicing.Read(einvoicing.Write(refused));
        Report.Fact("read back as a lifecycle message", read.Kind);
        Report.Fact("its profile resolves", read.Profile?.IsExact);
    }

    /// <summary>A collection is reported by the seller, and must say how much was collected, at which rate.</summary>
    private static void ASellerReportsCollection(EInvoicing einvoicing)
    {
        LifecycleStatusMessage collected = FrCdar
            .FromSeller(SellerSiren, "VENDEUR")
            .SentBy("0003", "PA-E Vendeur")
            .ToPublicPortal()
            .About("F202500003", new DateOnly(2025, 7, 1))
            .Collected(new FrCollectedAmount(12000m, 20m));

        DocumentStatusCharacteristic amount = collected.References[0].StatusDetails[0].Characteristics[0];

        Report.Fact("profile", collected.SpecificationIdentifier.Value);
        Report.Fact("collected", amount.ValueAmount.Value);
        Report.Fact("at", $"{amount.ValuePercent.Value}%");
        Report.Fact("written", $"{einvoicing.Write(collected).Length} characters");
    }

    /// <summary>Getting the direction wrong is refused before anything is written.</summary>
    private static void TheWrongWayRound()
    {
        try
        {
            FrCdar.FromPlatform("0003", "PA-E Vendeur")
                .ToSeller(SellerSiren, "VENDEUR")
                .About("F202500003", new DateOnly(2025, 7, 1))
                .Approved();
        }
        catch (InvalidOperationException refused)
        {
            Report.Say("Asking a platform to approve an invoice:");
            Report.Note(refused.Message);
        }
    }
}
