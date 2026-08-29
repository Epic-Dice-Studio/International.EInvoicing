using International.EInvoicing.Diagnostics;
using International.EInvoicing.Profiles;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Configuration;

/// <summary>
/// Assembles an instance of the library: which profiles it implements, what it does with diagnostics, and
/// what it refuses to read. Format and country packages extend this with their own methods.
/// </summary>
public sealed class EInvoicingBuilder
{
    private readonly List<Profile> _profiles = [];
    private readonly List<Action<DiagnosticPolicyBuilder>> _diagnosticConfigurations = [];

    private DocumentLimits _limits = DocumentLimits.Default;

    /// <summary>Registers a profile. A profile registered here wins over one the library ships.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="profile"/> is <c>null</c>.</exception>
    public EInvoicingBuilder AddProfile(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _profiles.Add(profile);
        return this;
    }

    /// <summary>Registers several profiles.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="profiles"/> is <c>null</c>.</exception>
    public EInvoicingBuilder AddProfiles(IEnumerable<Profile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        foreach (Profile profile in profiles)
        {
            AddProfile(profile);
        }

        return this;
    }

    /// <summary>Chooses the diagnostic preset to start from.</summary>
    public EInvoicingBuilder UseDiagnosticPreset(DiagnosticPreset preset) =>
        ConfigureDiagnostics(o => o.UsePreset(preset));

    /// <summary>Overrides every diagnostic of a category.</summary>
    public EInvoicingBuilder OnCategory(DiagnosticCategory category, DiagnosticAction action) =>
        ConfigureDiagnostics(o => o.OnCategory(category, action));

    /// <summary>Overrides one diagnostic code, for example <c>EIV1043</c>.</summary>
    public EInvoicingBuilder OnCode(string code, DiagnosticAction action) =>
        ConfigureDiagnostics(o => o.OnCode(code, action));

    /// <summary>Decides case by case. Return <c>null</c> to defer to the remaining rules.</summary>
    public EInvoicingBuilder OnDiagnostic(Func<Diagnostic, DiagnosticAction?> decide) =>
        ConfigureDiagnostics(o => o.OnDiagnostic(decide));

    /// <summary>Replaces the resource limits applied to incoming documents.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="limits"/> is <c>null</c>.</exception>
    public EInvoicingBuilder UseLimits(DocumentLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        _limits = limits;
        return this;
    }

    internal EInvoicingBuilder ConfigureDiagnostics(Action<DiagnosticPolicyBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _diagnosticConfigurations.Add(configure);
        return this;
    }

    internal ProfileRegistry BuildRegistry() => new(_profiles);

    internal EInvoicingOptions BuildOptions() => new()
    {
        Limits = _limits,
        DiagnosticPolicy = DiagnosticPolicy.Create(policy =>
        {
            foreach (Action<DiagnosticPolicyBuilder> configure in _diagnosticConfigurations)
            {
                configure(policy);
            }
        }),
    };
}
