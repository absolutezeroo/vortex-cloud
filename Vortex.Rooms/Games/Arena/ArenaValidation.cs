using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Vortex.Rooms.Games.Arena;

/// <summary>One thing a match needs from the room, and whether the room has it.</summary>
/// <param name="What">Human-readable, for the report: "Banzai tiles", "Team 2 goal".</param>
/// <param name="Found">How many are present.</param>
/// <param name="Required">How many are needed.</param>
/// <param name="Fatal">Whether falling short blocks the match rather than merely degrading it.</param>
public readonly record struct ArenaRequirement(string What, int Found, int Required, bool Fatal)
{
    public bool IsMet => Found >= Required;
}

/// <summary>
/// The structured answer to "can this room start a match of this game, and if not, what is missing".
/// It replaces the scattered boolean checks each game used to make at kick-off, where a missing goal
/// or an empty arena simply produced a match that did nothing and said nothing.
/// <para>
/// A fatal unmet requirement blocks the match. A non-fatal one is reported and the match proceeds —
/// a Freeze arena with no scoreboards is playable, it is just quieter.
/// </para>
/// </summary>
public sealed record ArenaValidation
{
    public static readonly ArenaValidation Valid = new() { Requirements = [] };

    public required ImmutableArray<ArenaRequirement> Requirements { get; init; }

    /// <summary>True when nothing fatal is missing.</summary>
    public bool CanStart
    {
        get
        {
            foreach (ArenaRequirement requirement in Requirements)
            {
                if (requirement.Fatal && !requirement.IsMet)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>A one-line summary of what is missing, for the log that explains a refusal. Empty
    /// when nothing is.</summary>
    public string DescribeShortfall()
    {
        StringBuilder builder = new();

        foreach (ArenaRequirement requirement in Requirements)
        {
            if (requirement.IsMet)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append("; ");
            }

            builder
                .Append(requirement.What)
                .Append(": ")
                .Append(requirement.Found)
                .Append('/')
                .Append(requirement.Required);
        }

        return builder.ToString();
    }

    public static ArenaValidationBuilder Builder() => new();
}

/// <summary>Collects the requirements of one game's arena check.</summary>
public sealed class ArenaValidationBuilder
{
    private readonly List<ArenaRequirement> _requirements = [];

    /// <summary>Without this, no match. Reports as ✗ and blocks the start.</summary>
    public ArenaValidationBuilder Require(string what, int found, int required = 1)
    {
        _requirements.Add(new ArenaRequirement(what, found, required, Fatal: true));

        return this;
    }

    /// <summary>Wanted but not essential — the match still starts without it.</summary>
    public ArenaValidationBuilder Prefer(string what, int found, int required = 1)
    {
        _requirements.Add(new ArenaRequirement(what, found, required, Fatal: false));

        return this;
    }

    public ArenaValidation Build() => new() { Requirements = [.. _requirements] };
}
