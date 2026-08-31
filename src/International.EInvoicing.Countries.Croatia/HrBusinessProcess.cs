using System.Text.RegularExpressions;

namespace International.EInvoicing.Countries.Croatia;

/// <summary>
/// The business process (BT-23) a Croatian invoice must declare.
/// </summary>
/// <remarks>
/// <c>HR-BR-34</c> makes BT-23 mandatory and restricts it to <c>P1</c>…<c>P12</c>, or <c>P99:</c> followed by
/// the buyer's own designation for a process the twelve do not cover. What each of the twelve means is in the
/// CIUS-HR specification and in no artefact this repository carries, so this checks the shape and leaves the
/// choice to the caller rather than inventing labels for them.
/// </remarks>
public static partial class HrBusinessProcess
{
    /// <summary>The twelve published process codes, <c>P1</c> to <c>P12</c>.</summary>
    public static IReadOnlyList<string> All { get; } =
        [.. Enumerable.Range(1, 12).Select(number => $"P{number}")];

    /// <summary>Whether a value is one <c>HR-BR-34</c> accepts.</summary>
    public static bool IsValid(string? value) =>
        value is not null && Accepted().IsMatch(value);

    /// <summary>
    /// The process code for something the twelve do not cover, as the buyer designates it.
    /// </summary>
    /// <param name="designation">The buyer's own designation for the process.</param>
    /// <exception cref="ArgumentException"><paramref name="designation"/> is empty.</exception>
    public static string ForBuyer(string designation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(designation);

        return "P99:" + designation.Trim();
    }

    [GeneratedRegex("^(P([1-9]|1[0-2])|P99:.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex Accepted();
}
