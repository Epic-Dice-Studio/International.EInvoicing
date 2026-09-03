using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// What an ordered item is certified as — an eco-label, a standard it meets.
/// </summary>
/// <remarks>
/// It is a term of the order rather than a description of it: a buyer who ordered a certified product and
/// received an uncertified one did not get what they agreed to, so the certificate belongs on the agreement
/// beside the price.
/// </remarks>
public sealed class OrderItemCertificate : InvoiceNode
{
    /// <summary>What the certificate is called.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>What kind of certificate it is, as a code.</summary>
    public CodeField TypeCode { get; set; }

    /// <summary>What kind of certificate it is, in words.</summary>
    public TextField Type { get; set; }

    /// <summary>Anything the parties added about it.</summary>
    public TextField Remarks { get; set; }

    /// <summary>Who issued it, which is what makes a certificate worth anything.</summary>
    public Party? Issuer { get; set; }

    /// <summary>The certificate document itself, when the parties reference one.</summary>
    public IdentifierField DocumentReference { get; set; }
}
