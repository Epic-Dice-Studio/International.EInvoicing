using System.Collections.Concurrent;
using System.Reflection;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.Validation.En16931;

/// <summary>
/// The EN 16931 rule sets, loaded from the artefacts embedded in this package.
/// </summary>
/// <remarks>
/// Loading a rule set parses close to a thousand expressions, so each is loaded once and reused. They are
/// immutable, and safe to share.
/// </remarks>
public static class En16931Rules
{
    /// <summary>The version of the published artefacts this package carries.</summary>
    public const string ArtefactVersion = "1.3.16";

    /// <summary>
    /// The edition of the standard these artefacts encode.
    /// </summary>
    /// <remarks>
    /// The artefacts are published against one edition at a time. CEN published EN 16931-1:2026 in May 2026;
    /// no validation artefacts for it exist yet, so a document declaring that edition is validated against
    /// these rules and the report says so rather than claiming a clean pass.
    /// </remarks>
    public static En16931Edition Edition => En16931Edition.Original;

    private static readonly ConcurrentDictionary<string, SchematronRuleSet> Loaded = new(StringComparer.Ordinal);

    /// <summary>The rule set for a syntax.</summary>
    /// <exception cref="NotSupportedException">EN 16931 has no artefacts for that syntax.</exception>
    public static SchematronRuleSet For(DocumentSyntax syntax)
    {
        if (syntax == DocumentSyntax.Ubl)
        {
            return Load("EN16931-UBL.sch", $"{Edition} (UBL)");
        }

        if (syntax == DocumentSyntax.Cii)
        {
            return Load("EN16931-CII.sch", $"{Edition} (CII)");
        }

        throw new NotSupportedException(
            $"EN 16931 publishes validation artefacts for UBL and CII only, not for {syntax}.");
    }

    /// <summary>Whether EN 16931 publishes artefacts for a syntax.</summary>
    public static bool Covers(DocumentSyntax syntax) => syntax == DocumentSyntax.Ubl || syntax == DocumentSyntax.Cii;

    private static SchematronRuleSet Load(string resource, string name) =>
        Loaded.GetOrAdd(resource, key => SchematronRuleSet.Load(ReadResource(key), name, ArtefactVersion));

    private static string ReadResource(string name)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"The embedded artefact '{name}' is missing from this package.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
