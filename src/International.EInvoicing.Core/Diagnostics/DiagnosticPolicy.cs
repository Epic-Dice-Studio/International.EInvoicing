using System.Collections.Concurrent;

namespace International.EInvoicing.Diagnostics;

/// <summary>
/// Decides what happens to each diagnostic a reader produces. The library never decides on the caller's
/// behalf whether a document is acceptable; this is where the caller says so.
/// </summary>
/// <remarks>
/// Rules are resolved most specific first: caller predicates in registration order, then a rule for the exact
/// code, then a rule for the category, then the preset.
/// </remarks>
public sealed class DiagnosticPolicy
{
    private static readonly ConcurrentDictionary<DiagnosticPreset, DiagnosticPolicy> Presets = new();

    private readonly DiagnosticPreset _preset;
    private readonly IReadOnlyDictionary<string, DiagnosticAction> _byCode;
    private readonly IReadOnlyDictionary<DiagnosticCategory, DiagnosticAction> _byCategory;
    private readonly IReadOnlyList<Func<Diagnostic, DiagnosticAction?>> _predicates;

    internal DiagnosticPolicy(
        DiagnosticPreset preset,
        IReadOnlyDictionary<string, DiagnosticAction> byCode,
        IReadOnlyDictionary<DiagnosticCategory, DiagnosticAction> byCategory,
        IReadOnlyList<Func<Diagnostic, DiagnosticAction?>> predicates)
    {
        _preset = preset;
        _byCode = byCode;
        _byCategory = byCategory;
        _predicates = predicates;
    }

    /// <summary>Descriptor defaults, unchanged. Used when the caller configures nothing.</summary>
    public static DiagnosticPolicy Balanced => ForPreset(DiagnosticPreset.Balanced);

    /// <summary>Drops what a caller reading only the EN 16931 core cannot act on.</summary>
    public static DiagnosticPolicy Lenient => ForPreset(DiagnosticPreset.Lenient);

    /// <summary>Anything not fully interpreted makes the document unusable.</summary>
    public static DiagnosticPolicy Strict => ForPreset(DiagnosticPreset.Strict);

    /// <summary>Builds a policy from a preset plus overrides.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static DiagnosticPolicy Create(Action<DiagnosticPolicyBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new DiagnosticPolicyBuilder();
        configure(builder);
        return builder.Build();
    }

    /// <summary>The action this policy takes for <paramref name="diagnostic"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostic"/> is <c>null</c>.</exception>
    public DiagnosticAction Resolve(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        foreach (Func<Diagnostic, DiagnosticAction?> predicate in _predicates)
        {
            if (predicate(diagnostic) is { } action)
            {
                return action;
            }
        }

        if (_byCode.TryGetValue(diagnostic.Code, out DiagnosticAction byCode))
        {
            return byCode;
        }

        return _byCategory.TryGetValue(diagnostic.Category, out DiagnosticAction byCategory)
            ? byCategory
            : PresetAction(_preset, diagnostic.Category);
    }

    /// <summary>
    /// Applies this policy, returning the diagnostic to report or <c>null</c> when it is suppressed.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostic"/> is <c>null</c>.</exception>
    public Diagnostic? Apply(Diagnostic diagnostic) => Resolve(diagnostic) switch
    {
        DiagnosticAction.Suppress => null,
        DiagnosticAction.Escalate => Raise(diagnostic, DiagnosticSeverity.Error),
        DiagnosticAction.Fail => Raise(diagnostic, DiagnosticSeverity.Fatal),
        _ => diagnostic,
    };

    private static DiagnosticPolicy ForPreset(DiagnosticPreset preset) =>
        Presets.GetOrAdd(preset, key => new DiagnosticPolicy(key, new Dictionary<string, DiagnosticAction>(),
            new Dictionary<DiagnosticCategory, DiagnosticAction>(), []));

    private static Diagnostic Raise(Diagnostic diagnostic, DiagnosticSeverity floor) =>
        diagnostic.Severity >= floor ? diagnostic : diagnostic.WithSeverity(floor);

    private static DiagnosticAction PresetAction(DiagnosticPreset preset, DiagnosticCategory category) =>
        preset switch
        {
            DiagnosticPreset.Lenient => category switch
            {
                DiagnosticCategory.UnmappedElement or DiagnosticCategory.UnknownCode => DiagnosticAction.Suppress,
                _ => DiagnosticAction.Keep,
            },
            DiagnosticPreset.Strict => category switch
            {
                DiagnosticCategory.Safety or DiagnosticCategory.Configuration => DiagnosticAction.Keep,
                _ => DiagnosticAction.Fail,
            },
            _ => DiagnosticAction.Keep,
        };
}
