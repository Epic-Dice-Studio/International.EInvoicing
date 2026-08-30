using International.EInvoicing.Profiles;

namespace International.EInvoicing.Validation;

/// <summary>
/// A set of rules that can check a document, whatever it is written in.
/// </summary>
/// <remarks>
/// <para>
/// Rule sets are registered rather than hard-coded, which is what lets a caller add the ones this library
/// cannot ship — the Peppol and French artefacts declare no licence — and their own alongside. A validator
/// runs every registered set that applies and reports the ones that did not, so a document checked against
/// half of what governs it is never presented as conforming.
/// </para>
/// <para>
/// Implementing this is the supported way to add rules written in C# rather than in Schematron: a company
/// policy, a customer's quirk, a rule the norm leaves open.
/// </para>
/// </remarks>
public interface IDocumentRuleSet
{
    /// <summary>The rule set's name, as a report should print it.</summary>
    string Name { get; }

    /// <summary>Which version of it, so a report can be reproduced later.</summary>
    string Version { get; }

    /// <summary>
    /// Whether this rule set has anything to say about such a document.
    /// </summary>
    /// <param name="syntax">The syntax the document is written in.</param>
    /// <param name="specification">What the document claims to conform to, BT-24.</param>
    bool AppliesTo(DocumentSyntax syntax, ProfileIdentifier specification);

    /// <summary>Checks a document and says what it found.</summary>
    /// <param name="document">The document, as text.</param>
    ValidationReport Validate(string document);
}
