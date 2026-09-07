using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// How many rooms the account owns, and the most recently active few.
/// </summary>
/// <remarks>
/// The total is counted separately so the panel can say "8 of 214" rather than implying the
/// account owns exactly what is listed.
/// </remarks>
public sealed record ProfileRooms(int Total, IReadOnlyList<ProfileRoomRow> Latest);
