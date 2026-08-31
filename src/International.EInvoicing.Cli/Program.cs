using System.Reflection;
using International.EInvoicing.Cli;
using International.EInvoicing.Cli.Commands;

return Cli.Run(args, Console.Out, Console.Error);

namespace International.EInvoicing.Cli
{
    /// <summary>
    /// <c>einvoice</c> — the validator this ecosystem did not have in .NET.
    /// </summary>
    /// <remarks>
    /// Everything is a plain static call taking its writers, so the whole tool is testable without a process:
    /// the tests run <see cref="Run"/> against a <see cref="StringWriter"/> and read what came out. A CLI
    /// nobody can test is a CLI that breaks quietly.
    /// </remarks>
    internal static class Cli
    {
        public static int Run(IReadOnlyList<string> arguments, TextWriter output, TextWriter errors)
        {
            CommandLine command = CommandLine.Parse(arguments);

            if (command.Has("version"))
            {
                output.WriteLine(Version());
                return Exit.Ok;
            }

            if (command.Command.Length == 0 || command.Has("help", "h"))
            {
                WriteHelp(output);
                return command.Command.Length == 0 && !command.Has("help", "h") ? Exit.CouldNotRun : Exit.Ok;
            }

            switch (command.Command)
            {
                case "validate":
                    return ValidateCommand.Run(command, output, errors);
                case "inspect":
                    return InspectCommand.Run(command, output, errors);
                case "convert":
                    return ConvertCommand.Run(command, output, errors);
                case "profiles":
                    return CapabilitiesCommand.Profiles(command, output, errors);
                case "rules":
                    return CapabilitiesCommand.Rules(command, output, errors);
                default:
                    errors.WriteLine($"error: '{command.Command}' is not a command.");
                    WriteHelp(errors);
                    return Exit.CouldNotRun;
            }
        }

        public static string Version() =>
            typeof(Cli).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(Cli).Assembly.GetName().Version?.ToString()
            ?? "unknown";

        private static void WriteHelp(TextWriter output)
        {
            output.WriteLine($"einvoice {Version()} — electronic invoices, from the command line.");
            output.WriteLine();
            output.WriteLine("  einvoice validate <file|directory>...   check against every rule set that applies");
            output.WriteLine("  einvoice inspect  <file|directory>...   what is it, and what did reading it report");
            output.WriteLine("  einvoice convert  <file> --to ubl|cii   carry it across, and say what that cost");
            output.WriteLine("  einvoice profiles                       the profiles this build knows");
            output.WriteLine("  einvoice rules                          the rule sets it can judge with");
            output.WriteLine();
            output.WriteLine("Options");
            output.WriteLine("  --rules <file|directory>   add Schematron, source or compiled, that we may not ship");
            output.WriteLine("  --strict | --lenient       how hard reading should be on a document");
            output.WriteLine("  --json                     machine-readable report (validate)");
            output.WriteLine("  --quiet                    only what failed (validate)");
            output.WriteLine("  --out <file>               write there instead of standard output (convert)");
            output.WriteLine();
            output.WriteLine("Exit codes: 0 conforming, 1 rejected or unchecked, 2 could not run.");
            output.WriteLine();
            output.WriteLine("UBL, CII, Factur-X PDFs and lifecycle messages. EN 16931, XRechnung, France,");
            output.WriteLine("Germany and Belgium are built in; artefacts that may not be redistributed are");
            output.WriteLine("fetched — see build/fetch-specs.sh — and pointed at with --rules.");
        }
    }
}
