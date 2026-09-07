namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// How many players picked one value.
/// </summary>
/// <param name="Retired">
/// True for an answer that no longer matches any configured choice — what editing a question leaves
/// behind. Those are counted and shown rather than dropped, because silently missing votes read as
/// a bug in the tally.
/// </param>
public sealed record PollTallyEntry(
    string Value,
    string Text,
    int ChoiceType,
    int Count,
    double Share,
    bool Retired
);
