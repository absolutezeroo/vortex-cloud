using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

/// <summary>
/// One furnidata entry, addressed by its position.
/// </summary>
/// <remarks>
/// Not by <c>id</c> and not by <c>classname</c>: 55 836 entries carry only 55 254 distinct ids and
/// 51 425 distinct classnames. 577 ids are shared between the floor and wall lists — two namespaces,
/// legitimate — but 5 are duplicated inside <c>roomitemtypes</c> itself, and nothing says which one
/// the client keeps. The position is the only thing that identifies a row, which is also why
/// deleting one is not offered: every index after it would shift.
/// </remarks>
public sealed record GamedataFurniRequest(
    string Kind,
    int Index,
    string Field,
    string Value,
    DateTime? ExpectedModifiedUtc,
    string Reason
) : IReasonedRequest;
