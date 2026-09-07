using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>The question types an operator may pick from.</summary>
public sealed record PollQuestionTypeOptions(
    int Count,
    IReadOnlyList<PollQuestionTypeOption> Items
);
