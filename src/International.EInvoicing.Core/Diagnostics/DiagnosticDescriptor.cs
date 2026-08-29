using System.Globalization;

namespace International.EInvoicing.Diagnostics;

/// <summary>
/// The immutable definition of a diagnostic: its stable code, its category, its default severity and the
/// shape of its message. Descriptors are declared once in <see cref="DiagnosticCodes"/> so that codes,
/// severities and documentation stay in one place.
/// </summary>
/// <param name="Code">Stable identifier, for example <c>EIV2001</c>. Changing it is a breaking change.</param>
/// <param name="Category">The category used to configure policy.</param>
/// <param name="DefaultSeverity">Severity applied unless policy overrides it.</param>
/// <param name="MessageFormat">Composite format string filled by <see cref="Diagnostic.Create"/>.</param>
public sealed record DiagnosticDescriptor(
    string Code,
    DiagnosticCategory Category,
    DiagnosticSeverity DefaultSeverity,
    string MessageFormat)
{
    /// <summary>Link to the catalogue page documenting this code.</summary>
    public string HelpLink { get; } =
        $"https://github.com/Epic-Dice-Studio/International.EInvoicing/blob/main/docs/diagnostics/{Code}.md";

    internal string FormatMessage(params object?[] arguments) =>
        arguments.Length == 0
            ? MessageFormat
            : string.Format(CultureInfo.InvariantCulture, MessageFormat, arguments);
}
