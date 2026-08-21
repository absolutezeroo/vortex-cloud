using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Vortex.Specs.Cli;

/// <summary>
/// The parsed command line: a verb, positional arguments, and flags.
/// </summary>
/// <remarks>
/// Hand-rolled rather than taking a parser dependency. The surface is a verb and a handful of flags,
/// and this repository's contract asks for new dependencies to be justified by need; forty lines is
/// cheaper than a package for that.
/// </remarks>
public sealed class CommandLine
{
    private readonly Dictionary<string, string?> _options;

    private CommandLine(
        string verb,
        IReadOnlyList<string> positional,
        Dictionary<string, string?> options
    )
    {
        Verb = verb;
        Positional = positional;
        _options = options;
    }

    public string Verb { get; }

    public IReadOnlyList<string> Positional { get; }

    public static CommandLine Parse(string[] args)
    {
        string verb = args.Length > 0 && !args[0].StartsWith('-') ? args[0] : "help";
        List<string> positional = [];
        Dictionary<string, string?> options = new(StringComparer.Ordinal);

        for (
            int i = verb == "help" && args.Length > 0 && args[0] == "help" ? 1 : 1;
            i < args.Length;
            i++
        )
        {
            string argument = args[i];

            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                positional.Add(argument);
                continue;
            }

            string name = argument[2..];
            int equals = name.IndexOf('=', StringComparison.Ordinal);

            if (equals >= 0)
            {
                options[name[..equals]] = name[(equals + 1)..];
                continue;
            }

            // A flag takes the next token as its value only when that token is not itself a flag,
            // so `--force` and `--out path` both work without a schema.
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                options[name] = args[i + 1];
                i++;
            }
            else
            {
                options[name] = null;
            }
        }

        return new CommandLine(verb, positional, options);
    }

    public bool Has(string name) => _options.ContainsKey(name);

    public string? Value(string name) =>
        _options.TryGetValue(name, out string? value) ? value : null;

    public int ValueOrDefault(string name, int fallback) =>
        int.TryParse(
            Value(name),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int parsed
        )
            ? parsed
            : fallback;
}
