using International.EInvoicing.Configuration;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.Validation.En16931;

/// <summary>Adds the EN 16931 rules to a library instance.</summary>
public static class En16931BuilderExtensions
{
    /// <summary>
    /// Checks every UBL and CII document that EN 16931 actually governs against EN 16931.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These are the rules a CIUS restricts and an extension builds on, so they apply whatever a document
    /// declares — unless the profile it declares brings rules of its own.
    /// </para>
    /// <para>
    /// They are registered as the <em>baseline</em>: when a profile's own rule set applies to the same
    /// document, these stand aside, because that rule set already carries whatever of EN 16931 still applies
    /// and has adapted what the profile adapts. Running both rejects invoices the publishers call valid —
    /// see <see cref="IDocumentRuleSet.IsBaseline"/> for the two families where that was measured. With no
    /// profile rule set registered they run as before.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddEn16931Rules(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddRules(DocumentSyntax.Ubl, En16931Rules.For(DocumentSyntax.Ubl), GovernedByEn16931, isBaseline: true)
            .AddRules(DocumentSyntax.Cii, En16931Rules.For(DocumentSyntax.Cii), GovernedByEn16931, isBaseline: true);
    }

    /// <summary>
    /// Whether EN 16931's rules have anything to say about a document declaring this profile.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Not every UBL or CII document is an EN 16931 invoice, and judging one that is not by rules that do not
    /// govern it produces failures that are not failures. Two families make this concrete: <b>Peppol PINT</b>
    /// is built for tax systems EN 16931 was never written for, and <b>Factur-X MINIMUM and BASIC WL</b> say
    /// in their own specification that they are not EN 16931 invoices.
    /// </para>
    /// <para>
    /// The test is the identifier itself: an EN 16931 profile names the standard and its edition at the
    /// front, and a CIUS keeps that prefix. A document declaring nothing is read as EN 16931 by the fallback
    /// chain, so it is judged by EN 16931 too.
    /// </para>
    /// </remarks>
    private static bool GovernedByEn16931(ProfileIdentifier specification) =>
        !specification.IsDeclared || En16931Edition.Of(specification) is not null;
}
