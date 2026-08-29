using International.EInvoicing.BuildTools;

// Repository gates, run locally and in CI:
//   dotnet run --project build/Tools -- coverage [--check]
//   dotnet run --project build/Tools -- diagnostics

if (args.Length == 0)
{
    Console.Error.WriteLine("usage: coverage [--check] | diagnostics");
    return 2;
}

string repositoryRoot = Repository.FindRoot();

return args[0] switch
{
    "coverage" => CoverageTable.Run(repositoryRoot, checkOnly: args.Contains("--check")),
    "diagnostics" => DiagnosticCatalogue.Run(repositoryRoot),
    _ => Unknown(args[0]),
};

static int Unknown(string command)
{
    Console.Error.WriteLine($"unknown command '{command}' (coverage | diagnostics)");
    return 2;
}
