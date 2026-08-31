using International.EInvoicing.Profiles;

namespace International.EInvoicing.Xml;

/// <summary>
/// Which syntax an XML namespace belongs to.
/// </summary>
/// <remarks>
/// Asked by the writers, which keep extension data so that a document written back in the syntax it came
/// from loses nothing — not so that one syntax's elements can be smuggled into another. An invoice read from
/// UBL carries UBL elements the model had no field for; writing those into a CII document produces something
/// no receiver will accept. <c>EInvoicing.Convert</c> reports them as the cost of the conversion instead.
/// </remarks>
public static class SyntaxNamespaces
{
    private const string UblPrefix = "urn:oasis:names:specification:ubl:";
    private const string CiiPrefix = "urn:un:unece:uncefact:data:standard:";

    /// <summary>
    /// Whether a namespace is one of the syntax's own.
    /// </summary>
    /// <remarks>
    /// Matched by prefix because both bodies number their namespaces by version — UBL 2.1 and a later UBL
    /// are equally UBL, and a rule written against one exact URI would quietly stop holding.
    /// </remarks>
    /// <param name="namespaceUri">The namespace to judge. An empty string belongs to nothing.</param>
    /// <param name="syntax">The syntax to judge it against.</param>
    /// <exception cref="ArgumentNullException"><paramref name="namespaceUri"/> is <c>null</c>.</exception>
    public static bool BelongsTo(string namespaceUri, DocumentSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(namespaceUri);

        if (syntax == DocumentSyntax.Ubl)
        {
            return namespaceUri.StartsWith(UblPrefix, StringComparison.Ordinal);
        }

        return syntax == DocumentSyntax.Cii
            && namespaceUri.StartsWith(CiiPrefix, StringComparison.Ordinal);
    }
}
