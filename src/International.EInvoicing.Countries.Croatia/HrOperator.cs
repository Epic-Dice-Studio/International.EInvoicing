using International.EInvoicing.Countries.Croatia.Identifiers;

namespace International.EInvoicing.Countries.Croatia;

/// <summary>
/// The person who issued the invoice — HR-BT-4 and HR-BT-5, which EN 16931 has no business term for.
/// </summary>
/// <remarks>
/// <c>HR-BR-37</c> and <c>HR-BR-9</c> require both, and the OIB is checked here rather than at the receiver:
/// a wrong one is a rejected invoice, and the digits are checkable before anything is written.
/// </remarks>
/// <param name="Name">What the operator is called (HR-BT-4).</param>
/// <param name="Oib">The operator's own OIB, not the seller's (HR-BT-5).</param>
public sealed record HrOperator(string Name, HrOib Oib)
{
    /// <summary>The same, from an OIB still in string form.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="FormatException"><paramref name="oib"/> is not an OIB.</exception>
    public HrOperator(string name, string oib)
        : this(name, HrOib.Parse(oib))
    {
    }

    /// <summary>What the operator is called (HR-BT-4).</summary>
    /// <exception cref="ArgumentException">The value is empty.</exception>
    public string Name { get; } = Validated(Name);

    private static string Validated(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return name;
    }
}
