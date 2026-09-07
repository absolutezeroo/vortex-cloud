namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One objective type.
/// </summary>
/// <param name="Wired">Whether anything in the emulator actually advances it. A quest on an unwired
/// type can be accepted and never completed.</param>
public sealed record QuestTypeOption(string Name, bool Wired);
