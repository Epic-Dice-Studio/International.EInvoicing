namespace International.EInvoicing.Countries.France.Lifecycle;

/// <summary>What a value attached to a status detail represents, as the DGFiP codes it.</summary>
public static class FrStatusValueType
{
    /// <summary>An amount collected, net of tax.</summary>
    public const string CollectedAmount = "MEN";

    /// <summary>An amount paid.</summary>
    public const string PaidAmount = "MPA";

    /// <summary>An amount not collected, net of tax.</summary>
    public const string UncollectedAmount = "MNA";

    /// <summary>An amount not collected, including tax.</summary>
    public const string UncollectedAmountIncludingTax = "MNATTC";

    /// <summary>An amount to be paid.</summary>
    public const string AmountToPay = "MAP";

    /// <summary>An amount to be paid, including tax.</summary>
    public const string AmountToPayIncludingTax = "MAPTTC";

    /// <summary>A late-payment charge.</summary>
    public const string LateCharge = "RAP";

    /// <summary>An early-payment discount.</summary>
    public const string EarlyPaymentDiscount = "ESC";

    /// <summary>A rebate.</summary>
    public const string Rebate = "RAB";

    /// <summary>A discount.</summary>
    public const string Discount = "REM";

    /// <summary>A surcharge.</summary>
    public const string Surcharge = "MAJ";

    /// <summary>Bank details.</summary>
    public const string BankDetails = "CBB";

    /// <summary>The value read from the document.</summary>
    public const string DocumentValue = "DIV";

    /// <summary>The value expected instead.</summary>
    public const string ExpectedValue = "DVA";
}
