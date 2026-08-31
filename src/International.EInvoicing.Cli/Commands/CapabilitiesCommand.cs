using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Cli.Commands;

/// <summary>
/// <c>einvoice profiles</c> and <c>einvoice rules</c> — what this build actually knows.
/// </summary>
/// <remarks>
/// Read out of the registry at run time rather than from a table someone maintains, so it cannot drift from
/// the truth. It is also the fastest way to answer "why did my document come back unchecked": if the rule
/// set is not in this list, nothing was going to judge it, and <c>--rules</c> is the answer.
/// </remarks>
internal static class CapabilitiesCommand
{
    public static int Profiles(CommandLine command, TextWriter output, TextWriter errors)
    {
        EInvoicing library = Library.Build(command, errors);

        if (library.Profiles is not ProfileResolver { Registry: { } registry })
        {
            errors.WriteLine("error: this build resolves profiles with something that keeps no registry.");
            return Exit.CouldNotRun;
        }

        foreach (Profile profile in registry.All
            .OrderBy(profile => profile.Syntax.Name, StringComparer.Ordinal)
            .ThenBy(profile => profile.Id.Value, StringComparer.Ordinal))
        {
            output.WriteLine($"{profile.Syntax.Name,-5} {profile.Name}");
            output.WriteLine($"      {profile.Id}");
        }

        return Exit.Ok;
    }

    public static int Rules(CommandLine command, TextWriter output, TextWriter errors)
    {
        EInvoicing library = Library.Build(command, errors);

        foreach (IDocumentRuleSet ruleSet in library.RuleSets)
        {
            output.WriteLine($"{ruleSet.Name}  {ruleSet.Version}");
        }

        output.WriteLine();
        output.WriteLine($"{library.RuleSets.Count} rule set(s) registered.");
        output.WriteLine(
            "Artefacts that may not be redistributed are not in here until you point --rules at them; "
            + "see build/fetch-specs.sh.");

        return Exit.Ok;
    }
}
