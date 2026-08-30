using International.EInvoicing.Building;

namespace International.EInvoicing.Peppol;

/// <summary>
/// The Peppol business process (BT-23), which says which exchange an invoice belongs to.
/// </summary>
/// <remarks>
/// Peppol requires it, in a shape of its own: <c>urn:fdc:peppol.eu:2017:poacc:billing:NN:1.0</c>. An invoice
/// without it fails <c>PEPPOL-EN16931-R001</c>, and one that improvises the shape fails <c>R007</c> — neither
/// of which EN 16931 would have complained about, which is how an invoice passes the base rules and is
/// rejected by the network.
/// </remarks>
public static class PeppolBusinessProcess
{
    /// <summary>The billing process — an invoice or credit note, which is process 01.</summary>
    public const string Billing = "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0";

    /// <summary>
    /// The billing process a <b>PINT</b> document declares, which is not the same string.
    /// </summary>
    /// <remarks>
    /// BIS Billing numbers its processes and PINT does not, so a PINT invoice carrying the BIS process
    /// identifier is wrong in a way that looks right. See <see cref="PeppolPintProfiles"/>.
    /// </remarks>
    public const string PintBilling = "urn:peppol:bis:billing";

    /// <summary>The PINT self-billing process, where the buyer issues the invoice.</summary>
    public const string PintSelfBilling = "urn:peppol:bis:selfbilling";

    /// <summary>The process with a number of your own, for an exchange other than plain billing.</summary>
    /// <param name="number">The two-digit process number.</param>
    /// <exception cref="ArgumentException"><paramref name="number"/> is not two digits.</exception>
    public static string Numbered(string number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        return number.Length == 2 && number.All(char.IsAsciiDigit)
            ? $"urn:fdc:peppol.eu:2017:poacc:billing:{number}:1.0"
            : throw new ArgumentException(
                $"'{number}' is not a Peppol process number: two digits, as in 01. PEPPOL-EN16931-R007 "
                + "rejects anything else.",
                nameof(number));
    }
}

/// <summary>What a Peppol invoice needs beyond EN 16931.</summary>
public static class PeppolInvoiceBuilderExtensions
{
    /// <summary>
    /// Declares the Peppol business process (BT-23), which the network requires and EN 16931 does not.
    /// </summary>
    /// <param name="builder">The invoice being built.</param>
    /// <param name="businessProcess">The process. The billing one unless said otherwise.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoiceBuilder ForPeppol(
        this EInvoiceBuilder builder,
        string businessProcess = PeppolBusinessProcess.Billing)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(businessProcess);

        return builder.Extend(invoice => invoice.BusinessProcessType = businessProcess);
    }

    /// <summary>
    /// Declares the <b>PINT</b> business process (BT-23), which is a different string from the BIS one.
    /// </summary>
    /// <remarks>
    /// Using <see cref="ForPeppol"/> on a PINT invoice writes the BIS Billing process identifier, which the
    /// jurisdiction rules reject — the two families look alike and are not interchangeable.
    /// </remarks>
    /// <param name="builder">The invoice being built.</param>
    /// <param name="businessProcess">The process. PINT billing unless said otherwise.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoiceBuilder ForPeppolPint(
        this EInvoiceBuilder builder,
        string businessProcess = PeppolBusinessProcess.PintBilling)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(businessProcess);

        return builder.Extend(invoice => invoice.BusinessProcessType = businessProcess);
    }
}
