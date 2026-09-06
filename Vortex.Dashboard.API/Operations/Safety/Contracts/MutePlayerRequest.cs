using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations;

/// <summary>Room-scoped mute. Only works while the target is currently present in a room — there is
/// no account-wide chat mute in this codebase.</summary>
public sealed record MutePlayerRequest(int PlayerId, int DurationSeconds, string Reason)
    : IReasonedRequest;
