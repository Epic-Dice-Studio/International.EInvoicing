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
/// <para>
/// Reading an element also marks it as mapped, so whatever is left at the end of the document is exactly what
/// nobody claimed, and can be kept as extension data.
/// </para>
/// <para>
/// Public because the Cross Industry Invoice is not the only message in its family: Order-X is the Cross
/// Industry Order, on a later version of the same data types, and reads its values with exactly this. Anyone
/// teaching the library another UN/CEFACT message needs the same, and should not have to write it again.
/// </para>
/// </remarks>
public sealed class CiiValueReader(DiagnosticCollector diagnostics, HashSet<XElement> mapped)
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

    /// <summary>
    /// Un-marks an element, so it travels as extension data after all.
    /// </summary>
    /// <remarks>
    /// Reading sometimes means looking at a value to decide what a thing is, and then deciding the model has
    /// no place for it. Looking is not keeping: an element the writer does not state itself has to go back
    /// where it came from, or it is lost.
    /// </remarks>
    public void Release(XElement? element)
    {
        if (element is not null)
        {
            mapped.Remove(element);
        }
    }

    /// <summary>Reads free text, keeping the language it was written in.</summary>
    public TextField ReadText(XElement? element) =>
        Consume(element)
            ? new TextField(element.Value, Attribute(element, "languageID"), Source(element))
            : TextField.Unset;

    /// <summary>Reads a code, keeping which list it was drawn from.</summary>
    public CodeField ReadCode(XElement? element) =>
        Consume(element)
            ? new CodeField(
                element.Value,
                Attribute(element, "listID"),
                Attribute(element, "listVersionID"),
                Attribute(element, "listAgencyID"),
                Source(element))
            : CodeField.Unset;

    /// <summary>Reads an identifier, keeping the scheme that gives it meaning.</summary>
    public IdentifierField ReadIdentifier(XElement? element) =>
        Consume(element)
            ? new IdentifierField(
                element.Value,
                Attribute(element, "schemeID"),
                Attribute(element, "schemeAgencyID"),
                Attribute(element, "schemeVersionID"),
                Source(element))
            : IdentifierField.Unset;

    /// <summary>Reads a monetary amount, keeping the currency stated on it.</summary>
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

    /// <summary>Reads a quantity, keeping the unit it is counted in.</summary>
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

    /// <summary>Reads a bare number — a percentage, a factor.</summary>
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

    /// <summary>Reads a true or false, whether written as a word or as a digit.</summary>
    public IndicatorField ReadIndicator(XElement? element)
    {
        XElement? indicator = ChildNamed(element, "Indicator") ?? element;
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

        // Most dates in CII are a DateTimeString; the tax point date inside the breakdown is a DateString,
        // which is the same value in the same format and a different element name. Reading only the first
        // meant BT-7 arrived unset and left as extension data.
        XElement? element = ChildNamed(parent, "DateTimeString") ?? ChildNamed(parent, "DateString");
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

    /// <summary>
    /// Reads a moment rather than a day.
    /// </summary>
    /// <remarks>
    /// An invoice's dates are days, so <see cref="ReadDate"/> is what reads most of CII. An order's issue
    /// time is a moment: Order-X writes it as <c>CCYYMMDDHHMM</c> (format 203), and lifecycle messages write
    /// <c>CCYYMMDDHHMMSS</c> (204). All three of those and a plain day are accepted, because a sender who
    /// states less precision than the format allows has still stated the moment they meant.
    /// </remarks>
    public DateTimeField ReadDateTime(XElement? parent, string? businessTerm = null)
    {
        if (!Consume(parent))
        {
            return DateTimeField.Unset;
        }

        XElement? element = ChildNamed(parent, "DateTimeString") ?? ChildNamed(parent, "DateString");
        if (!Consume(element))
        {
            return DateTimeField.Unset;
        }

        string? format = Attribute(element, "format");
        string text = element.Value.Trim();

        foreach (string pattern in (string[])["yyyyMMddHHmmss", "yyyyMMddHHmm", "yyyyMMdd"])
        {
            if (DateTime.TryParseExact(
                text,
                pattern,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime moment))
            {
                return new DateTimeField(new DateTimeOffset(moment, TimeSpan.Zero), format, Source(element));
            }
        }

        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset parsed)
            ? new DateTimeField(parsed, format, Source(element))
            : new DateTimeField(null, format, Source(element, Report(element, "a timestamp", businessTerm)));
    }

    /// <summary>Where an element sits, so a diagnostic can name the place rather than the value.</summary>
    public static SourceLocation LocationOf(XElement element)
    {
        var lineInfo = (IXmlLineInfo)element;
        return new SourceLocation(
            PathOf(element),
            lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
            lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0);
    }

    private static XElement? Child(XElement? parent, XName name) => parent?.Element(name);

    /// <summary>
    /// Finds a child by local name, whatever namespace it is in.
    /// </summary>
    /// <remarks>
    /// The data-type namespaces carry a version — <c>UnqualifiedDataType:100</c> for the Cross Industry
    /// Invoice, <c>:128</c> for the Order-X order — and either may be qualified or unqualified for the same
    /// element. A value's local name is unambiguous inside its parent, so matching on it reads both without
    /// a namespace to configure.
    /// </remarks>
    private static XElement? ChildNamed(XElement? parent, string localName) =>
        parent?.Elements().FirstOrDefault(child => child.Name.LocalName == localName);

    private static string PathOf(XElement element)
    {
        var segments = new Stack<string>();
        for (XElement? current = element; current is not null; current = current.Parent)
        {
            segments.Push(PrefixOf(current) + current.Name.LocalName);
        }

        return "/" + string.Join('/', segments);
    }

    // A diagnostic names the path a reader was at, so the prefix has to be recognised by what the namespace
    // is rather than by which version of it: the same reader sees ReusableAggregate...:100 in an invoice and
    // :128 in an Order-X order.
    private static string PrefixOf(XElement element)
    {
        string ns = element.Name.NamespaceName;

        if (ns.Contains("ReusableAggregateBusinessInformationEntity", StringComparison.Ordinal))
        {
            return CiiNames.RamPrefix + ":";
        }

        if (ns.Contains("UnqualifiedDataType", StringComparison.Ordinal))
        {
            return CiiNames.UdtPrefix + ":";
        }

        if (ns.Contains("QualifiedDataType", StringComparison.Ordinal))
        {
            return CiiNames.QdtPrefix + ":";
        }

        return ns.Contains("CrossIndustryInvoice", StringComparison.Ordinal)
            || ns.Contains("SCRDMCCBDACIOMessageStructure", StringComparison.Ordinal)
                ? CiiNames.RsmPrefix + ":"
                : string.Empty;
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
