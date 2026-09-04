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

    /// <summary>
    /// Whether this rule set is the base a family of profiles is built on, rather than the rules of one
    /// profile.
    /// </summary>
    /// <remarks>
    /// EN 16931 is the base: a CIUS restricts it and an extension builds on it. A base stands aside for a
    /// rule set that declares <see cref="SupersedesBaseline"/>, and runs otherwise.
    /// </remarks>
    bool IsBaseline => false;

    /// <summary>
    /// Whether this rule set already carries the rules of the base it derives from, so that running the base
    /// as well would judge the document twice by rules the profile has adapted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Publishers differ, and the difference is not cosmetic. Factur-X ships one artefact per profile which
    /// carries the EN 16931 rules that still apply, <em>adapted</em>: EXTENDED permits grouped lines, where
    /// a heading's amount is the sum of its children, and its BR-CO-10 knows that. Belgium's
    /// <c>GLOBALUBL.BE</c> does the same for 94% of the EN 16931 rules. Running the unmodified originals
    /// beside either rejects invoices the publishers call valid — eight of Factur-X's own 58 documents and
    /// seventeen of Belgium's 36.
    /// </para>
    /// <para>
    /// XRechnung is the opposite and shows why this cannot be assumed: its artefact carries <em>none</em> of
    /// the EN 16931 rules — the KoSIT validator runs the two as separate steps — so a rule set that
    /// superseded the base there would quietly stop checking almost everything. That is not hypothetical
    /// either: it is what the cross-check against KoSIT caught when this was first written the other way
    /// round.
    /// </para>
    /// <para>
    /// Defaults to <c>false</c>, which is the safe answer: a rule set that says nothing is run alongside the
    /// base, and nothing stops being checked.
    /// </para>
    /// </remarks>
    bool SupersedesBaseline => false;

    /// <summary>Checks a document and says what it found.</summary>
    /// <param name="document">The document, as text.</param>
    ValidationReport Validate(string document);
}
