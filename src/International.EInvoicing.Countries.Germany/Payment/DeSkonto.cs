using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using International.EInvoicing.Model;

namespace International.EInvoicing.Countries.Germany.Payment;

/// <summary>
/// One early-payment discount: pay within so many days, deduct so many percent.
/// </summary>
/// <param name="Days">Days from the invoice date within which the discount applies.</param>
/// <param name="Percentage">The percentage deducted, written with exactly two decimals.</param>
/// <param name="BaseAmount">
/// The amount the percentage applies to, when it is not the amount due (BT-115) — a partial amount, say.
/// <c>null</c> when the discount applies to the whole.
/// </param>
public sealed record DeSkonto(int Days, decimal Percentage, decimal? BaseAmount = null)
{
    /// <summary>This one term, as BR-DE-18 requires it to be written, without its line break.</summary>
    public override string ToString()
    {
        var text = new StringBuilder("#SKONTO#TAGE=")
            .Append(Days.ToString(CultureInfo.InvariantCulture))
            .Append("#PROZENT=")
            .Append(Percentage.ToString("F2", CultureInfo.InvariantCulture));

        if (BaseAmount is { } amount)
        {
            text.Append("#BASISBETRAG=").Append(amount.ToString("F2", CultureInfo.InvariantCulture));
        }

        return text.Append('#').ToString();
    }
}

/// <summary>
/// Reading and writing German early-payment discounts, which live inside free text.
/// </summary>
/// <remarks>
/// <para>
/// EN 16931 has no business term for <em>Skonto</em>. Germany needed one anyway, so XRechnung defined a
/// structured syntax <b>inside</b> BT-20, the payment terms note — and <c>BR-DE-18</c> validates it with a
/// regular expression. It looks like this, one term per line, and the last line must be followed by a line
/// break:
/// </para>
/// <code>
/// #SKONTO#TAGE=7#PROZENT=2.00#
/// #SKONTO#TAGE=14#PROZENT=1.00#
/// </code>
/// <para>
/// So a German invoice carries a number your accounting system needs, in a string. Getting it out with a
/// hand-rolled split is where the money goes wrong: the percentage must have exactly two decimals, the
/// keywords must be capitals, and a stray space fails the rule. That is what this is for.
/// </para>
/// <para>
/// The shape here is the one <c>BR-DE-18</c> tests, taken from the artefact in
/// <c>specs/xrechnung/schematron/</c> rather than from prose. Peppol's <c>DE-R-018</c> and PINT's German
/// rules judge the same statements with the same expression, anchored one notch more loosely at the start.
/// </para>
/// </remarks>
public static partial class DeSkontoTerms
{
    /// <summary>
    /// Every discount stated in an invoice's payment terms, in the order they appear.
    /// </summary>
    /// <remarks>
    /// Lines that are not discount statements are ignored rather than rejected: BT-20 is free text, and a
    /// German invoice routinely carries a sentence for a human alongside the machine-readable lines.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    public static IReadOnlyList<DeSkonto> SkontoTerms(this EInvoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return Parse(invoice.PaymentTerms.Value ?? invoice.PaymentTerms.Raw);
    }

    /// <summary>Every discount stated in a payment-terms note.</summary>
    public static IReadOnlyList<DeSkonto> Parse(string? paymentTerms)
    {
        if (string.IsNullOrEmpty(paymentTerms))
        {
            return [];
        }

        List<DeSkonto> terms = [];

        foreach (string line in paymentTerms.Split('\n'))
        {
            Match match = Statement().Match(line.Trim('\r', ' ', '\t'));

            if (!match.Success)
            {
                continue;
            }

            terms.Add(new DeSkonto(
                int.Parse(match.Groups["days"].Value, CultureInfo.InvariantCulture),
                decimal.Parse(match.Groups["percent"].Value, CultureInfo.InvariantCulture),
                match.Groups["base"].Success
                    ? decimal.Parse(match.Groups["base"].Value, CultureInfo.InvariantCulture)
                    : null));
        }

        return terms;
    }

    /// <summary>
    /// The payment-terms note for a set of discounts, with anything else you want to say after them.
    /// </summary>
    /// <remarks>
    /// The trailing line break is not decoration: <c>BR-DE-18</c> requires a complete statement to end with
    /// one, and an invoice whose last line is <c>…PROZENT=1.00#</c> with nothing after it fails.
    /// </remarks>
    /// <param name="terms">The discounts, in the order they should appear.</param>
    /// <param name="freeText">Anything else the note should say, after the discounts.</param>
    /// <exception cref="ArgumentNullException"><paramref name="terms"/> is <c>null</c>.</exception>
    public static string Write(IEnumerable<DeSkonto> terms, string? freeText = null)
    {
        ArgumentNullException.ThrowIfNull(terms);

        var note = new StringBuilder();

        foreach (DeSkonto term in terms)
        {
            note.Append(term).Append('\n');
        }

        if (!string.IsNullOrEmpty(freeText))
        {
            note.Append(freeText);
        }

        return note.ToString();
    }

    /// <summary>
    /// Puts the discounts into the invoice's payment terms, keeping whatever free text was already there.
    /// </summary>
    /// <remarks>
    /// Replaces any discount lines already in the note. A German invoice that states its discounts twice,
    /// once structurally and once in a sentence, is one where the two disagree by the second revision.
    /// </remarks>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static EInvoice WithSkonto(this EInvoice invoice, params DeSkonto[] terms)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(terms);

        string? existing = invoice.PaymentTerms.Value ?? invoice.PaymentTerms.Raw;
        string kept = string.Join(
            '\n',
            (existing ?? string.Empty)
                .Split('\n')
                .Where(line => !Statement().IsMatch(line.Trim('\r', ' ', '\t')))
                .Where(line => !string.IsNullOrWhiteSpace(line)));

        invoice.PaymentTerms = Write(terms, kept.Length == 0 ? null : kept);

        return invoice;
    }

    /// <summary>
    /// The expression <c>BR-DE-18</c> uses, named groups aside.
    /// </summary>
    /// <remarks>
    /// Taken from <c>common.sch</c> under <c>specs/xrechnung/schematron/</c>, and compared against that file
    /// on every test run so the two cannot drift: a statement this reads is one <c>BR-DE-18</c> accepts, and a
    /// statement it refuses is one that rule refuses too. What the two do with a refused line differs — the
    /// rule fails the invoice, this skips the line — because a reader that throws on arrival is no reader.
    /// </remarks>
    [GeneratedRegex(
        @"^#SKONTO#TAGE=(?<days>[0-9]+)#PROZENT=(?<percent>[0-9]+\.[0-9]{2})(#BASISBETRAG=(?<base>-?[0-9]+\.[0-9]{2}))?#$",
        RegexOptions.CultureInvariant)]
    private static partial Regex Statement();
}
