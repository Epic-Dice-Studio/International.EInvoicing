namespace International.EInvoicing.Diagnostics;

/// <summary>Configures a <see cref="DiagnosticPolicy"/>: a preset, then overrides from broad to precise.</summary>
public sealed class DiagnosticPolicyBuilder
{
    private readonly Dictionary<string, DiagnosticAction> _byCode = new(StringComparer.Ordinal);
    private readonly Dictionary<DiagnosticCategory, DiagnosticAction> _byCategory = [];
    private readonly List<Func<Diagnostic, DiagnosticAction?>> _predicates = [];

    private DiagnosticPreset _preset = DiagnosticPreset.Balanced;

    /// <summary>Chooses the starting point. Defaults to <see cref="DiagnosticPreset.Balanced"/>.</summary>
    public DiagnosticPolicyBuilder UsePreset(DiagnosticPreset preset)
    {
        _preset = preset;
        return this;
    }

    /// <summary>Overrides every diagnostic of a category.</summary>
    public DiagnosticPolicyBuilder OnCategory(DiagnosticCategory category, DiagnosticAction action)
    {
        _byCategory[category] = action;
        return this;
    }

    /// <summary>Overrides one code, for example <c>EIV2001</c>.</summary>
    /// <exception cref="ArgumentException"><paramref name="code"/> is empty.</exception>
    public DiagnosticPolicyBuilder OnCode(string code, DiagnosticAction action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        _byCode[code] = action;
        return this;
    }

    /// <summary>
    /// Decides case by case. Return <c>null</c> to defer to the remaining rules. Predicates are consulted
    /// before code and category rules, in registration order.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="decide"/> is <c>null</c>.</exception>
    public DiagnosticPolicyBuilder OnDiagnostic(Func<Diagnostic, DiagnosticAction?> decide)
    {
        ArgumentNullException.ThrowIfNull(decide);
        _predicates.Add(decide);
        return this;
    }

    internal DiagnosticPolicy Build() => new(_preset, _byCode, _byCategory, _predicates);
}
