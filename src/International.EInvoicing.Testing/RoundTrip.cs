using System.Xml.Linq;
using International.EInvoicing.Model;

namespace International.EInvoicing.Testing;

/// <summary>What a round trip produced, and what it cost.</summary>
/// <param name="Original">The document that went in.</param>
/// <param name="Written">The document that came out.</param>
/// <param name="Reread">The invoice the written document reads back as, when it reads at all.</param>
/// <param name="Lost">Elements the original had and the result has fewer of, as <c>{namespace}name</c>.</param>
/// <param name="Gained">Elements the result has and the original did not.</param>
public sealed record RoundTripResult(
    string Original,
    string Written,
    EInvoice? Reread,
    IReadOnlyList<string> Lost,
    IReadOnlyList<string> Gained)
{
    /// <summary>Whether nothing the original carried went missing.</summary>
    public bool IsFaithful => Lost.Count == 0;

    /// <inheritdoc />
    public override string ToString() => IsFaithful
        ? "The round trip lost nothing."
        : $"The round trip lost: {string.Join(", ", Lost)}";
}

/// <summary>
/// Read a document, write it back, and see what survived.
/// </summary>
/// <remarks>
/// <para>
/// The check is by element census, not by text: <b>byte equality is not promised and should never be
/// asserted</b>. Namespace prefixes, insignificant whitespace and attribute order are not normative, and a
/// document may legally come back with any of them changed. What must hold is that no element the original
/// carried is missing from the result — including the ones the model has no field for, which travel in
/// extension data.
/// </para>
/// <para>
/// <see cref="RoundTripResult.Gained"/> is reported too, and it is not always a fault: a writer fills in what
/// a profile requires. It is worth looking at when a receiver rejects a document this library rewrote.
/// </para>
/// </remarks>
public static class RoundTrip
{
    /// <summary>Reads <paramref name="document"/>, writes it back in the syntax it arrived in, and compares.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static RoundTripResult Check(EInvoicing library, string document)
    {
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(document);

        DocumentResult read = library.Read(document);

        if (read.Invoice is not { } invoice)
        {
            return new RoundTripResult(document, string.Empty, null, Census(document).Keys.Order(StringComparer.Ordinal).ToArray(), []);
        }

        return Compare(document, library.Write(invoice, FormatOf(read.Kind)), library);
    }

    /// <summary>Writes <paramref name="invoice"/>, reads it back, writes it again, and compares the two.</summary>
    /// <remarks>
    /// Starting from a model rather than a document tests the other half: that what the writer emits, the
    /// reader understands. A term written in a way our own reader cannot read is a defect that a
    /// document-first round trip never reaches.
    /// </remarks>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static RoundTripResult Check(EInvoicing library, EInvoice invoice, DocumentFormat format)
    {
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(invoice);

        string written = library.Write(invoice, format);
        DocumentResult read = library.Read(written);

        return read.Invoice is { } again
            ? Compare(written, library.Write(again, format), library)
            : new RoundTripResult(written, string.Empty, null, Census(written).Keys.Order(StringComparer.Ordinal).ToArray(), []);
    }

    private static RoundTripResult Compare(string original, string written, EInvoicing library)
    {
        Dictionary<string, int> before = Census(original);
        Dictionary<string, int> after = Census(written);

        return new RoundTripResult(
            original,
            written,
            library.Read(written).Invoice,
            [.. Difference(before, after)],
            [.. Difference(after, before)]);
    }

    private static IEnumerable<string> Difference(Dictionary<string, int> left, Dictionary<string, int> right) =>
        left
            .Where(pair => right.GetValueOrDefault(pair.Key) < pair.Value)
            .Select(pair => $"{pair.Key} ({pair.Value} → {right.GetValueOrDefault(pair.Key)})")
            .Order(StringComparer.Ordinal);

    private static Dictionary<string, int> Census(string document)
    {
        Dictionary<string, int> counts = new(StringComparer.Ordinal);

        XElement root;

        try
        {
            root = XElement.Parse(document, LoadOptions.None);
        }
        catch (System.Xml.XmlException)
        {
            return counts;
        }

        foreach (XElement element in root.DescendantsAndSelf())
        {
            string name = element.Name.ToString();
            counts[name] = counts.GetValueOrDefault(name) + 1;
        }

        return counts;
    }

    private static DocumentFormat FormatOf(DocumentKind kind) =>
        kind == DocumentKind.Cii ? DocumentFormat.Cii : DocumentFormat.Ubl;
}
