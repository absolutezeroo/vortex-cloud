using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// The objective types a quest may be written on.
/// </summary>
/// <remarks>
/// Read by reflection off the progression code's own constants, so the picker cannot offer a type
/// the engine does not know.
/// </remarks>
public sealed record QuestTypeOptions(int Count, IReadOnlyList<QuestTypeOption> Items);
