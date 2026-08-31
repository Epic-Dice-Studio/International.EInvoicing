using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Ubl.Reading;

/// <summary>
/// Turns UBL elements into fields. A value that cannot be converted is never dropped and never throws: the
/// field keeps the raw text, carries the diagnostic explaining why, and the document goes on being read.
/// </summary>
/// <remarks>
/// Reading an element also marks it as mapped, so whatever is left at the end of the document is exactly what
/// nobody claimed, and can be kept as extension data.
/// </remarks>
internal sealed class UblValueReader(DiagnosticCollector diagnostics, HashSet<XElement> mapped)
{
    /// <summary>Where anything this reader could not do is reported.</summary>
    public DiagnosticCollector Diagnostics => diagnostics;

    /// <summary>Marks an element as mapped. Returns false when there is no element to read.</summary>
    public bool Consume([NotNullWhen(true)] XElement? element)
    {
        if (element is null)
        {
            return false;
        }

        mapped.Add(element);
        return true;
    }

    /// <summary>
    /// A note whose subject code UBL folded into the text, kept apart without losing where it came from.
    /// </summary>
    public InvoiceNote ReadNote(XElement element)
    {
        Consume(element);
        (string? subjectCode, string text) = UblNoteSubject.Split(element.Value);

        return new InvoiceNote
        {
            SubjectCode = subjectCode is null ? CodeField.Unset : new CodeField(subjectCode, null, null, null, Source(element)),
            Text = new TextField(text, Attribute(element, "languageID"), Source(element)),
        };
    }

    public TextField ReadText(XElement? element) =>
        Consume(element)
            ? new TextField(element.Value, Attribute(element, "languageID"), Source(element))
            : TextField.Unset;

    public CodeField ReadCode(XElement? element) =>
        Consume(element)
            ? new CodeField(
                element.Value,
                Attribute(element, "listID"),
                Attribute(element, "listVersionID"),
                Attribute(element, "listAgencyID"),
                Source(element))
            : CodeField.Unset;

    public IdentifierField ReadIdentifier(XElement? element) =>
        Consume(element)
            ? new IdentifierField(
                element.Value,
                Attribute(element, "schemeID"),
                Attribute(element, "schemeAgencyID"),
                Attribute(element, "schemeVersionID"),
                Source(element))
            : IdentifierField.Unset;

    public AmountField ReadAmount(XElement? element, string? businessTerm = null)
    {
        if (!Consume(element))
        {
            return AmountField.Unset;
        }

        string? currency = Attribute(element, "currencyID");

        return TryReadDecimal(element, out decimal amount)
            ? new AmountField(amount, currency, Source(element))
            : new AmountField(null, currency, Source(element, Report(element, "an amount", businessTerm)));
    }

    public QuantityField ReadQuantity(XElement? element, string? businessTerm = null)
    {
        if (!Consume(element))
        {
            return QuantityField.Unset;
        }

        string? unit = Attribute(element, "unitCode");

        return TryReadDecimal(element, out decimal quantity)
            ? new QuantityField(quantity, unit, Attribute(element, "unitCodeListVersionID"), Source(element))
            : new QuantityField(null, unit, null, Source(element, Report(element, "a quantity", businessTerm)));
    }

    public Field<decimal> ReadDecimal(XElement? element, string? businessTerm = null)
    {
        if (!Consume(element))
        {
            return Field<decimal>.Unset;
        }

        return TryReadDecimal(element, out decimal value)
            ? new Field<decimal>(value, Source(element))
            : new Field<decimal>(null, Source(element, Report(element, "a number", businessTerm)));
    }

    /// <summary>Reads an <c>xs:date</c>. A trailing time zone offset is accepted and kept in the raw text.</summary>
    public DateField ReadDate(XElement? element, string? businessTerm = null)
    {
        if (!Consume(element))
        {
            return DateField.Unset;
        }

        string text = element.Value.Trim();
        ReadOnlySpan<char> datePart = text.Length >= 10 ? text.AsSpan(0, 10) : text;

        return DateOnly.TryParseExact(
            datePart,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateOnly date)
            ? new DateField(date, null, Source(element))
            : new DateField(null, null, Source(element, Report(element, "a date", businessTerm)));
    }

    public IndicatorField ReadIndicator(XElement? element)
    {
        if (!Consume(element))
        {
            return IndicatorField.Unset;
        }

        return element.Value.Trim().ToUpperInvariant() switch
        {
            "TRUE" or "1" => new IndicatorField(true, Source(element)),
            "FALSE" or "0" => new IndicatorField(false, Source(element)),
            _ => new IndicatorField(null, Source(element, Report(element, "an indicator", null))),
        };
    }

    public static SourceLocation LocationOf(XElement element)
    {
        var lineInfo = (IXmlLineInfo)element;
        return new SourceLocation(
            PathOf(element),
            lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
            lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0);
    }

    private static string PathOf(XElement element)
    {
        var segments = new Stack<string>();
        for (XElement? current = element; current is not null; current = current.Parent)
        {
            segments.Push(PrefixOf(current) + current.Name.LocalName);
        }

        return "/" + string.Join('/', segments);
    }

    private static string PrefixOf(XElement element)
    {
        XNamespace ns = element.Name.Namespace;
        if (ns == UblNames.Cbc)
        {
            return UblNames.CbcPrefix + ":";
        }

        return ns == UblNames.Cac ? UblNames.CacPrefix + ":" : string.Empty;
    }

    private static string? Attribute(XElement element, string name) => element.Attribute(name)?.Value;

    private static bool TryReadDecimal(XElement element, out decimal value) =>
        decimal.TryParse(
            element.Value.Trim(),
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out value);

    private static FieldSource Source(XElement element, Diagnostic? diagnostic = null) =>
        new(element.Value, LocationOf(element), diagnostic);

    private Diagnostic? Report(XElement element, string expected, string? businessTerm)
    {
        Diagnostic diagnostic = Diagnostic.Create(DiagnosticCodes.InvalidValue, element.Value.Trim(), expected) with
        {
            Location = LocationOf(element),
            BusinessTerm = businessTerm,
            Expected = expected,
            Found = element.Value.Trim(),
            AppliedFallback = "raw text preserved; typed value is null",
        };

        return diagnostics.Add(diagnostic);
    }
}
