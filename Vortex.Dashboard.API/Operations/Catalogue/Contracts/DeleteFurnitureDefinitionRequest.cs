using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations.Catalogue.Contracts;

/// <summary>Blocked server-side if the definition is still referenced by placed/owned furniture
/// instances or by a catalog product.</summary>
public sealed record DeleteFurnitureDefinitionRequest(int DefinitionId, string Reason)
    : IReasonedRequest;
