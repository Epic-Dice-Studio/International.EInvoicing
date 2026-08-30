using System.Text.RegularExpressions;

namespace International.EInvoicing.Ubl;

/// <summary>
/// The subject code of a note (BT-21), which UBL carries inside the note text.
/// </summary>
/// <remarks>
/// CII gives BT-21 its own element. UBL does not, so the EN 16931 binding writes it as a prefix —
/// <c>#AAB#Escompte pour paiement anticipé : néant</c> — and a reader that takes the note at face value both
/// loses the code and keeps a prefix nobody wants to display. France depends on this: three of its mandatory
/// mentions are identified by nothing but their code.
/// </remarks>
internal static partial class UblNoteSubject
{
    /// <summary>Splits <c>#CODE#text</c> into its two parts. Text without a prefix comes back unchanged.</summary>
    public static (string? SubjectCode, string Text) Split(string note)
    {
        Match match = Prefixed().Match(note);

        return match.Success
            ? (match.Groups["code"].Value, match.Groups["text"].Value)
            : (null, note);
    }

    /// <summary>Puts the two back together the way UBL carries them.</summary>
    public static string Join(string? subjectCode, string text) =>
        string.IsNullOrEmpty(subjectCode) ? text : $"#{subjectCode}#{text}";

    [GeneratedRegex(@"^#(?<code>[A-Z]{2,3})#(?<text>.*)$", RegexOptions.Singleline)]
    private static partial Regex Prefixed();
}
