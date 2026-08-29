using System.Globalization;
using System.Xml.Linq;

namespace International.EInvoicing.Validation.Schematron.XPath;

/// <summary>
/// The result of evaluating an expression: a sequence of items, each a node, a number, a string or a boolean.
/// </summary>
/// <remarks>
/// Numbers are <see cref="decimal"/>. The rules this engine runs compare invoice totals against sums of
/// lines, and an engine that does that in binary floating point reports correct invoices as wrong — which is
/// the single most expensive way a validator can be subtly broken.
/// </remarks>
internal readonly struct XPathValue
{
    private readonly object? _single;
    private readonly IReadOnlyList<object>? _many;

    private XPathValue(object? single, IReadOnlyList<object>? many)
    {
        _single = single;
        _many = many;
    }

    public static XPathValue Empty { get; } = new(null, []);

    public static XPathValue Number(decimal value) => new(value, null);

    public static XPathValue Text(string value) => new(value, null);

    public static XPathValue Boolean(bool value) => new(value, null);

    public static XPathValue Nodes(IReadOnlyList<object> nodes) =>
        nodes.Count == 1 ? new XPathValue(nodes[0], null) : new XPathValue(null, nodes);

    public IReadOnlyList<object> Items => _many ?? (_single is null ? [] : [_single]);

    public bool IsEmpty => _many is { Count: 0 } || (_many is null && _single is null);

    /// <summary>The effective boolean value, as XPath defines it.</summary>
    public bool AsBoolean() => _single switch
    {
        bool value => value,
        decimal number => number != 0,
        string text => text.Length > 0,
        null => _many is { Count: > 0 },
        _ => true,
    };

    /// <summary>
    /// The value as a number. An item that is not numeric text yields <c>null</c> rather than NaN, so a
    /// comparison against it is false rather than accidentally true.
    /// </summary>
    public decimal? AsNumber()
    {
        object? item = _single ?? (_many is { Count: > 0 } ? _many[0] : null);

        return item switch
        {
            decimal number => number,
            bool value => value ? 1m : 0m,
            null => null,
            _ => ParseNumber(StringOf(item)),
        };
    }

    /// <summary>The value as a string: the first item's string value, or empty for an empty sequence.</summary>
    public string AsText()
    {
        object? item = _single ?? (_many is { Count: > 0 } ? _many[0] : null);
        return item is null ? string.Empty : StringOf(item);
    }

    /// <summary>Every item's string value, which comparisons need because XPath compares sequences pairwise.</summary>
    public IEnumerable<string> AllText() => Items.Select(StringOf);

    /// <summary>Every item's numeric value, skipping those that are not numbers.</summary>
    public IEnumerable<decimal> AllNumbers()
    {
        foreach (object item in Items)
        {
            decimal? number = item switch
            {
                decimal value => value,
                bool value => value ? 1m : 0m,
                _ => ParseNumber(StringOf(item)),
            };

            if (number is { } parsed)
            {
                yield return parsed;
            }
        }
    }

    public static string StringOf(object item) => item switch
    {
        XElement element => element.Value,
        XAttribute attribute => attribute.Value,
        XDocument document => document.Root?.Value ?? string.Empty,
        decimal number => Format(number),
        bool value => value ? "true" : "false",
        string text => text,
        _ => item?.ToString() ?? string.Empty,
    };

    public static string Format(decimal number) =>
        number == decimal.Truncate(number) && Math.Abs(number) < 1_000_000_000_000m
            ? decimal.Truncate(number).ToString(CultureInfo.InvariantCulture)
            : number.ToString(CultureInfo.InvariantCulture);

    private static decimal? ParseNumber(string text) =>
        decimal.TryParse(
            text.Trim(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out decimal value)
            ? value
            : null;
}
