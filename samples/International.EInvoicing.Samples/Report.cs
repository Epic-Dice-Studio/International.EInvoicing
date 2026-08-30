using System.Globalization;

namespace International.EInvoicing.Samples;

/// <summary>Printing, so the chapters can be about the library rather than about the console.</summary>
internal static class Report
{
    private static int _chapter;

    public static void Chapter(string title)
    {
        _chapter++;
        Console.WriteLine();
        Console.WriteLine($"── {_chapter}. {title} ".PadRight(96, '─'));
    }

    public static void Say(string line) => Console.WriteLine($"   {line}");

    public static void Fact(string label, object? value) =>
        Console.WriteLine($"   {label,-42} {Format(value)}");

    public static void Note(string line) => Console.WriteLine($"   · {line}");

    public static void Snippet(string xml, int lines = 6)
    {
        foreach (string line in xml.Split('\n').Take(lines))
        {
            Console.WriteLine($"     {line.TrimEnd()}");
        }

        Console.WriteLine("     …");
    }

    private static string Format(object? value) => value switch
    {
        null => "(none)",
        bool flag => flag ? "yes" : "no",
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTimeOffset moment => moment.ToString("u", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "(none)",
    };
}
