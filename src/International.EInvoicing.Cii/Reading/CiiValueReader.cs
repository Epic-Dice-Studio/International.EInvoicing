using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Values;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Cii.Reading;

/// <summary>
/// Turns CII elements into fields. A value that cannot be converted is never dropped and never throws: the
/// field keeps the raw text, carries the diagnostic explaining why, and the document goes on being read.
/// </summary>
/// <remarks>
/// Reading an element also marks it as mapped, so whatever is left at the end of the document is exactly what
/// nobody claimed, and can be kept as extension data.
/// </remarks>
internal sealed class CiiValueReader(DiagnosticCollector diagnostics, HashSet<XElement> mapped)
{
    /// <summary>Where anything this reader could not do is reported.</summary>
    public DiagnosticCollector Diagnostics => diagnostics;

    /// <summary>The limits in force while reading, so the guards can be applied where the values are.</summary>
    public DocumentLimits Limits { get; init; } = DocumentLimits.Default;

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

    public IndicatorField ReadIndicator(XElement? element)
    {
        XElement? indicator = Child(element, CiiNames.Udt + "Indicator") ?? element;
        if (!Consume(indicator))
        {
            return IndicatorField.Unset;
        }

        return indicator.Value.Trim().ToUpperInvariant() switch
        {
            "TRUE" or "1" => new IndicatorField(true, Source(indicator)),
            "FALSE" or "0" => new IndicatorField(false, Source(indicator)),
            _ => new IndicatorField(null, Source(indicator, Report(indicator, "an indicator", null))),
        };
    }

    /// <summary>
    /// Reads a CII date. The value sits in a <c>udt:DateTimeString</c> whose <c>format</c> attribute says how
    /// to read it — normally <c>102</c> for <c>CCYYMMDD</c>. Partial formats such as <c>610</c> (a month) are
    /// legal but carry less than a day, so they are reported and left as raw text rather than invented into a
    /// date the sender never wrote.
    /// </summary>
    public DateField ReadDate(XElement? parent, string? businessTerm = null)
    {
        if (!Consume(parent))
        {
            return DateField.Unset;
        }

        XElement? element = Child(parent, CiiNames.Udt + "DateTimeString")
            ?? Child(parent, CiiNames.Qdt + "DateTimeString");
        if (!Consume(element))
        {
            return DateField.Unset;
        }

        string? format = Attribute(element, "format");
        string text = element.Value.Trim();

        if (format is null or DateField.FormatCcyyMmDd
            && DateOnly.TryParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
        {
            return new DateField(date, format ?? DateField.FormatCcyyMmDd, Source(element));
        }

        if (DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly isoDate))
        {
            return new DateField(isoDate, format, Source(element));
        }

        Diagnostic? diagnostic = format is null or DateField.FormatCcyyMmDd
            ? Report(element, "a date", businessTerm)
            : ReportUnsupportedFormat(element, format, businessTerm);

        return new DateField(null, format, Source(element, diagnostic));
    }

    public static SourceLocation LocationOf(XElement element)
    {
        var lineInfo = (IXmlLineInfo)element;
        return new SourceLocation(
            PathOf(element),
            lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
            lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0);
    }

    private static XElement? Child(XElement? parent, XName name) => parent?.Element(name);

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
        if (ns == CiiNames.Ram)
        {
            return CiiNames.RamPrefix + ":";
        }

        if (ns == CiiNames.Udt)
        {
            return CiiNames.UdtPrefix + ":";
        }

        if (ns == CiiNames.Qdt)
        {
            return CiiNames.QdtPrefix + ":";
        }

        return ns == CiiNames.Rsm ? CiiNames.RsmPrefix + ":" : string.Empty;
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

    private Diagnostic? ReportUnsupportedFormat(XElement element, string format, string? businessTerm)
    {
        Diagnostic diagnostic = Diagnostic.Create(DiagnosticCodes.UnsupportedDateFormat, format) with
        {
            Location = LocationOf(element),
            BusinessTerm = businessTerm,
            Expected = $"format {DateField.FormatCcyyMmDd}",
            Found = format,
            AppliedFallback = "raw text and format code preserved; typed value is null",
        };

        return diagnostics.Add(diagnostic);
    }
}
