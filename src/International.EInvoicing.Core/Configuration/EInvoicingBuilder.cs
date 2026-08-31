using International.EInvoicing.Diagnostics;
using International.EInvoicing.Documents;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Xml;
using Microsoft.Extensions.DependencyInjection;

namespace International.EInvoicing.Configuration;

/// <summary>
/// Assembles an instance of the library: which formats and countries it handles, which rules it checks
/// against, what it does with diagnostics, and what it refuses to read.
/// </summary>
/// <remarks>
/// One vocabulary, whether or not there is a container. <c>services.AddEInvoicing(e =&gt; e.AddUbl())</c> and
/// <c>EInvoicing.Create(e =&gt; e.AddUbl())</c> take the same calls and mean the same thing; the difference is
/// only where the result ends up. Format and country packages extend this with their own methods.
/// </remarks>
public sealed class EInvoicingBuilder
{
    private readonly List<Profile> _profiles = [];
    private readonly List<Action<DiagnosticPolicyBuilder>> _diagnosticConfigurations = [];
    private readonly List<IDocumentRuleSet> _ruleSets = [];
    private readonly List<IWritePipelineStep> _writeSteps = [];
    private readonly IServiceCollection? _services;

    private DocumentLimits _limits = DocumentLimits.Default;

    /// <summary>Assembles a library instance that is not going into a container.</summary>
    public EInvoicingBuilder()
    {
    }

    internal EInvoicingBuilder(IServiceCollection services) => _services = services;

    /// <summary>Whether this builder is filling a dependency injection container.</summary>
    /// <remarks>
    /// A package method can use this to decide whether registering its services is worth doing. It rarely
    /// needs to: <see cref="ConfigureServices"/> does nothing when there is no container.
    /// </remarks>
    public bool HasServices => _services is not null;

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

    /// <summary>
    /// Adds a rule set every validation should run when it applies.
    /// </summary>
    /// <remarks>
    /// This is how the artefacts this library cannot ship are brought in — the Peppol and French ones — and
    /// how rules of your own join them. A validator runs each of these that applies to the document in front
    /// of it, and names the ones that did not.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="ruleSet"/> is <c>null</c>.</exception>
    public EInvoicingBuilder AddRules(IDocumentRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);

        _ruleSets.Add(ruleSet);

        if (_services is not null)
        {
            _services.AddSingleton(ruleSet);
        }

        return this;
    }

    /// <summary>Adds several rule sets.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="ruleSets"/> is <c>null</c>.</exception>
    public EInvoicingBuilder AddRules(IEnumerable<IDocumentRuleSet> ruleSets)
    {
        ArgumentNullException.ThrowIfNull(ruleSets);

        foreach (IDocumentRuleSet ruleSet in ruleSets)
        {
            AddRules(ruleSet);
        }

        return this;
    }

    /// <summary>
    /// Adds a step that runs whenever the library writes a document.
    /// </summary>
    /// <remarks>
    /// Numbering, house rounding, a signature, an element your ERP insists on: the answer to "run my own
    /// logic during generation", without a fork and without hoping every call site remembers. Steps run in
    /// the order they are added, wrapped around the writer — so a writer used directly runs them too.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="step"/> is <c>null</c>.</exception>
    public EInvoicingBuilder AddWriteStep(IWritePipelineStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        _writeSteps.Add(step);

        if (_services is not null)
        {
            _services.AddSingleton(step);
        }

        return this;
    }

    /// <summary>Adds a step written inline, for the ones too small to deserve a type.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="step"/> is <c>null</c>.</exception>
    public EInvoicingBuilder AddWriteStep(Action<WriteContext, Action<WriteContext>> step)
    {
        ArgumentNullException.ThrowIfNull(step);

        return AddWriteStep(new InlineWriteStep(step));
    }

    /// <summary>Adds several steps.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="steps"/> is <c>null</c>.</exception>
    public EInvoicingBuilder AddWriteSteps(IEnumerable<IWritePipelineStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        foreach (IWritePipelineStep step in steps)
        {
            AddWriteStep(step);
        }

        return this;
    }

    /// <summary>
    /// Registers services, when this builder is filling a container. Does nothing when it is not, so a
    /// package method can call it unconditionally.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public EInvoicingBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        if (_services is not null)
        {
            configure(_services);
        }

        return this;
    }

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

    /// <summary>The profiles assembled so far, as a registry a resolver can use.</summary>
    /// <remarks>Public because assembling the library by hand is a supported thing to do.</remarks>
    public ProfileRegistry BuildRegistry() => new(_profiles);

    /// <summary>The rule sets assembled so far, in the order they were added.</summary>
    public IReadOnlyList<IDocumentRuleSet> BuildRuleSets() => _ruleSets;

    /// <summary>The write pipeline steps assembled so far, in the order they run.</summary>
    public IReadOnlyList<IWritePipelineStep> BuildWriteSteps() => _writeSteps;

    /// <summary>The options assembled so far.</summary>
    public EInvoicingOptions BuildOptions() => new()
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

    private sealed class InlineWriteStep(Action<WriteContext, Action<WriteContext>> step) : IWritePipelineStep
    {
        public void Write(WriteContext context, Action<WriteContext> next) => step(context, next);
    }
}
