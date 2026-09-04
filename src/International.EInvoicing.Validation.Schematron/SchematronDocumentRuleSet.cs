using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Validation.Schematron;

/// <summary>
/// A Schematron rule set, registered so that validation runs it whenever it applies.
/// </summary>
/// <remarks>
/// This is the bridge between an artefact and the rest of the library: load the <c>.sch</c> once, say which
/// syntax it governs and — optionally — which profiles it is meant for, and every validation afterwards
/// takes it into account.
/// </remarks>
public sealed class SchematronDocumentRuleSet : IDocumentRuleSet
{
    private readonly SchematronRuleSet _rules;
    private readonly DocumentSyntax _syntax;
    private readonly Func<ProfileIdentifier, bool>? _appliesTo;
    private readonly bool _isBaseline;
    private readonly bool _supersedesBaseline;
    private readonly SchematronValidator _validator = new();

    /// <summary>Registers a loaded rule set.</summary>
    /// <param name="rules">The rules themselves.</param>
    /// <param name="syntax">The syntax they are written against.</param>
    /// <param name="appliesTo">
    /// Which declared profiles they govern. Omit it for rules that apply to every document in that syntax.
    /// </param>
    /// <param name="isBaseline">
    /// Whether these are the rules a family of profiles is built on rather than one profile's own — see
    /// <see cref="IDocumentRuleSet.IsBaseline"/>. Only EN 16931 sets this.
    /// </param>
    /// <param name="supersedesBaseline">
    /// Whether these already carry the base's rules, adapted — see
    /// <see cref="IDocumentRuleSet.SupersedesBaseline"/>. True for Factur-X and UBL.BE, false for XRechnung.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="rules"/> is <c>null</c>.</exception>
    public SchematronDocumentRuleSet(
        SchematronRuleSet rules,
        DocumentSyntax syntax,
        Func<ProfileIdentifier, bool>? appliesTo = null,
        bool isBaseline = false,
        bool supersedesBaseline = false)
    {
        ArgumentNullException.ThrowIfNull(rules);

        _rules = rules;
        _syntax = syntax;
        _appliesTo = appliesTo;
        _isBaseline = isBaseline;
        _supersedesBaseline = supersedesBaseline;
    }

    /// <inheritdoc />
    public string Name => _rules.Name;

    /// <inheritdoc />
    public string Version => _rules.Version;

    /// <inheritdoc />
    public bool AppliesTo(DocumentSyntax syntax, ProfileIdentifier specification) =>
        syntax == _syntax && (_appliesTo is null || _appliesTo(specification));

    /// <inheritdoc />
    public bool IsBaseline => _isBaseline;

    /// <inheritdoc />
    public bool SupersedesBaseline => _supersedesBaseline;

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public ValidationReport Validate(string document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return _validator.Validate(document, _rules);
    }
}
