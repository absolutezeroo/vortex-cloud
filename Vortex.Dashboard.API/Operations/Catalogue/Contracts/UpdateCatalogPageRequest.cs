using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations;

public sealed record UpdateCatalogPageRequest(
    int PageId,
    int? ParentId,
    string Localization,
    string? Name,
    int Icon,
    string Layout,
    List<string>? ImageData,
    List<string>? TextData,
    int SortOrder,
    bool Visible,
    string Reason
) : IReasonedRequest;
