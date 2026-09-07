using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations.Catalogue.Contracts;

/// <summary>Grant a furniture definition (optionally with extra data) to a player's inventory.</summary>
public sealed record GiveFurnitureRequest(
    int PlayerId,
    int DefinitionId,
    string? ExtraData,
    string Reason
) : IReasonedRequest;
