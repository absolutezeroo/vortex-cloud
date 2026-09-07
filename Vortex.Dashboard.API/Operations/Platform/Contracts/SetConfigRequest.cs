using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations.Platform.Contracts;

/// <summary>Set a runtime server-config value. <paramref name="Key"/> must be a known
/// <c>ConfigKeyCatalog</c> key and <paramref name="Value"/> must parse for that key's kind.</summary>
public sealed record SetConfigRequest(string Key, string Value, string Reason) : IReasonedRequest;
