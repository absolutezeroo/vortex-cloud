using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record HandItemRequest(
    int HandItemId,
    string Name,
    int Nutrition,
    int Thirst,
    string Reason
) : IReasonedRequest;
