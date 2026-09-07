using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>What players actually answered, question by question.</summary>
public sealed record PollResults(
    int Id,
    string Code,
    string Headline,
    bool NpsPoll,
    PollFunnel Funnel,
    IReadOnlyList<PollQuestionResult> Questions
);
