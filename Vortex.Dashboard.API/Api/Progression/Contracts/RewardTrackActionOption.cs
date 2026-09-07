using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One action code.
/// </summary>
/// <param name="Wired">Whether anything actually raises it. Read from the loaded translators, so it
/// is a fact rather than a hand-kept list -- and false is the flag that a task on this action will
/// never advance.</param>
/// <param name="Facts">What a step on this action can filter on, and what a later step can point
/// back at. The shapes come from the translators themselves, so a fact the action never emits
/// cannot be offered.</param>
public sealed record RewardTrackActionOption(
    string Name,
    bool Wired,
    IReadOnlyList<FactOption> Facts
);
