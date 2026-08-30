using System.Collections.Concurrent;
using System.Reflection;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.Validation.XRechnung;

/// <summary>
/// The XRechnung rule sets, loaded from the artefacts embedded in this package.
/// </summary>
/// <remarks>
/// XRechnung restricts EN 16931 rather than replacing it, so these rules are meant to run alongside the
/// EN 16931 ones, not instead of them. Running only these would leave the base rules unchecked.
/// </remarks>
public static class XRechnungRules
{
    /// <summary>The version of the published artefacts this package carries.</summary>
    public const string ArtefactVersion = "3.0";

    private static readonly ConcurrentDictionary<string, SchematronRuleSet> Loaded = new(StringComparer.Ordinal);

    /// <summary>The rule set for a syntax. Germany uses both, so both are here.</summary>
    /// <exception cref="NotSupportedException">XRechnung has no artefacts for that syntax.</exception>
    public static SchematronRuleSet For(DocumentSyntax syntax)
    {
        if (syntax == DocumentSyntax.Ubl)
        {
            return Load("XRechnung-UBL.sch", "XRechnung (UBL)");
        }

        if (syntax == DocumentSyntax.Cii)
        {
            return Load("XRechnung-CII.sch", "XRechnung (CII)");
        }

        throw new NotSupportedException($"XRechnung publishes artefacts for UBL and CII only, not for {syntax}.");
    }

    /// <summary>Whether XRechnung publishes artefacts for a syntax.</summary>
    public static bool Covers(DocumentSyntax syntax) => syntax == DocumentSyntax.Ubl || syntax == DocumentSyntax.Cii;

    private static SchematronRuleSet Load(string resource, string name) =>
        Loaded.GetOrAdd(
            resource,
            key => SchematronRuleSet.Load(Read(key), name, ArtefactVersion, include: Include));

    /// <summary>
    /// The German rule sets keep their global variables in a separate file, pulled in by an include.
    /// </summary>
    private static string? Include(string href) =>
        href.EndsWith("common.sch", StringComparison.OrdinalIgnoreCase) ? Read("XRechnung-common.sch") : null;

    private static string Read(string name)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"The embedded artefact '{name}' is missing from this package.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
