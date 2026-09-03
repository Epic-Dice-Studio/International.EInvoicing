using System.Globalization;
using System.Xml.Linq;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Ubl.Reading;
using International.EInvoicing.Values;

namespace International.EInvoicing.Ubl;

/// <summary>
/// A moment UBL states in two elements and the model holds in one.
/// </summary>
/// <remarks>
/// Every UBL document splits a timestamp into a date and a time of day, and every one of them names the
/// pair differently — <c>IssueDate</c>/<c>IssueTime</c>, <c>StartDate</c>/<c>StartTime</c>,
/// <c>ActualDespatchDate</c>/<c>ActualDespatchTime</c>. The joining is the same each time, and so is the
/// answer when the time is absent: the date alone, at midnight, with the raw text saying which it was.
/// </remarks>
internal static class UblMoment
{
    public static DateTimeField Read(XElement? date, XElement? time)
    {
        if (date is null)
        {
            return DateTimeField.Unset;
        }

        string text = time is null ? date.Value.Trim() : $"{date.Value.Trim()}T{time.Value.Trim()}";
        var source = new FieldSource(text, UblValueReader.LocationOf(date));

        return DateTimeOffset.TryParse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTimeOffset moment)
            ? new DateTimeField(moment, null, source)
            : new DateTimeField(null, null, source);
    }

    /// <summary>Splits a moment back into the date and the time of day UBL states separately.</summary>
    /// <remarks>
    /// The raw text is preferred over the value, so a document that stated a date and no time is written
    /// back the same way rather than gaining a midnight nobody sent.
    /// </remarks>
    public static (string Date, string? Time) Split(DateTimeField field)
    {
        string text = field.Raw
            ?? field.Value?.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture)
            ?? string.Empty;

        int separator = text.IndexOf('T', StringComparison.Ordinal);

        return separator < 0 || separator + 1 >= text.Length
            ? (text, null)
            : (text[..separator], text[(separator + 1)..]);
    }
}
