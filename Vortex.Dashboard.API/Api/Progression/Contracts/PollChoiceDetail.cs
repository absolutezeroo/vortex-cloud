namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One configured answer to a choice question.</summary>
public sealed record PollChoiceDetail(
    int Id,
    string Value,
    string ChoiceText,
    int ChoiceType,
    int SortOrder
);
