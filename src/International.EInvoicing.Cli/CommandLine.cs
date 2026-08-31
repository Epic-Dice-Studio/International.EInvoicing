namespace International.EInvoicing.Cli;

/// <summary>
/// The arguments, taken apart.
/// </summary>
/// <remarks>
/// Hand-written rather than brought in: this library adds no dependency without a reason, and the reason
/// would have to outweigh a hundred lines. What it accepts is deliberately small — <c>--name value</c>,
/// <c>--name=value</c>, <c>--flag</c>, and everything else is an operand.
/// </remarks>
internal sealed class CommandLine
{
    private readonly Dictionary<string, string?> _options = new(StringComparer.Ordinal);

    private CommandLine(string command, IReadOnlyList<string> operands, Dictionary<string, string?> options)
    {
        Command = command;
        Operands = operands;
        _options = options;
    }

    public string Command { get; }

    public IReadOnlyList<string> Operands { get; }

    public static CommandLine Parse(IReadOnlyList<string> arguments)
    {
        string command = arguments.Count > 0 && !arguments[0].StartsWith('-') ? arguments[0] : string.Empty;

        List<string> operands = [];
        Dictionary<string, string?> options = new(StringComparer.Ordinal);

        for (int index = command.Length > 0 ? 1 : 0; index < arguments.Count; index++)
        {
            string argument = arguments[index];

            if (!argument.StartsWith('-'))
            {
                operands.Add(argument);
                continue;
            }

            string name = argument.TrimStart('-');
            int equals = name.IndexOf('=', StringComparison.Ordinal);

            if (equals >= 0)
            {
                options[name[..equals]] = name[(equals + 1)..];
                continue;
            }

            bool takesAValue = index + 1 < arguments.Count && !arguments[index + 1].StartsWith('-');
            options[name] = takesAValue ? arguments[++index] : null;
        }

        return new CommandLine(command, operands, options);
    }

    public bool Has(params string[] names) => names.Any(_options.ContainsKey);

    public string? Value(params string[] names)
    {
        foreach (string name in names)
        {
            if (_options.TryGetValue(name, out string? value) && value is not null)
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>Every option given, so a command can complain about one it does not know.</summary>
    public IEnumerable<string> OptionNames => _options.Keys;
}
