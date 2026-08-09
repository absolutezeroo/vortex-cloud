using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace Vortex.Primitives.Bots;

/// <summary>
/// What the client's chatter dialog packs into one string:
/// <c>chat text;#;auto chat;#;delay;#;mix sentences</c>. The dialog's own parser accepts a legacy
/// three-field form separated by plain semicolons, so both are read here.
/// <para>
/// Splitting the whole blob on <c>;</c> — which is what the room used to do — leaves a bot reciting
/// its own settings: "true", "10" and "false" become phrases like any other.
/// </para>
/// </summary>
public sealed record BotChatterConfiguration
{
    /// <summary>The client's own field separator, chosen so a phrase may contain a semicolon.</summary>
    public const string FieldSeparator = ";#;";

    /// <summary>
    /// Slower than the dialog's floor of one second. A bot on a one-second timer fills the room's
    /// chat log on its own, and the client's own scrollback is what suffers.
    /// </summary>
    public const int MinimumDelaySeconds = 4;

    /// <summary>Long enough for a doorman that greets rarely; past this the value is a mistake.</summary>
    public const int MaximumDelaySeconds = 600;

    public const int DefaultDelaySeconds = 10;

    private static readonly char[] PhraseSeparators = ['\r', '\n'];

    public ImmutableArray<string> Phrases { get; init; } = [];

    /// <summary>Off means the bot only ever speaks when something else makes it.</summary>
    public bool AutoChat { get; init; }

    public int DelaySeconds { get; init; } = DefaultDelaySeconds;

    /// <summary>
    /// The dialog's "mix sentences" box. Read so the setting round-trips, but nothing recombines
    /// phrases yet — a bot with it ticked chats its lines whole rather than not at all.
    /// </summary>
    public bool Markov { get; init; }

    public static BotChatterConfiguration Empty { get; } = new();

    public static BotChatterConfiguration Parse(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return Empty;
        }

        // A blob with no marker is the older three-field form. Anything else is one field, which
        // reads as a phrase list on its own rather than as a malformed configuration.
        string[] fields = data.Contains(FieldSeparator, StringComparison.Ordinal)
            ? data.Split(FieldSeparator, StringSplitOptions.None)
            : data.Split(';', StringSplitOptions.None);

        string text = fields[0];

        return new BotChatterConfiguration
        {
            Phrases =
            [
                .. text.Split(PhraseSeparators, StringSplitOptions.RemoveEmptyEntries)
                    .Select(phrase => phrase.Trim())
                    .Where(phrase => phrase.Length > 0),
            ],
            AutoChat = fields.Length > 1 && ReadFlag(fields[1]),
            DelaySeconds = fields.Length > 2 ? ClampDelay(fields[2]) : DefaultDelaySeconds,
            Markov = fields.Length > 3 && ReadFlag(fields[3]),
        };
    }

    /// <summary>The dialog writes a bool, but the legacy form writes 1 and 0; both mean the same.</summary>
    private static bool ReadFlag(string value)
    {
        string trimmed = value.Trim();

        return trimmed.Equals("true", StringComparison.OrdinalIgnoreCase) || trimmed == "1";
    }

    private static int ClampDelay(string value)
    {
        if (!int.TryParse(value.Trim(), CultureInfo.InvariantCulture, out int seconds))
        {
            return DefaultDelaySeconds;
        }

        return Math.Clamp(seconds, MinimumDelaySeconds, MaximumDelaySeconds);
    }
}
