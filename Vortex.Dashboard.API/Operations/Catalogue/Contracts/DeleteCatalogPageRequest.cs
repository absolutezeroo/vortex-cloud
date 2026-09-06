using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations;

/// <summary>Blocked server-side if the page still has child pages or offers under it — delete those
/// first rather than cascading a silent mass-delete.</summary>
public sealed record DeleteCatalogPageRequest(int PageId, string Reason) : IReasonedRequest;
