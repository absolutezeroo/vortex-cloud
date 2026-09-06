using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record UpdateTargetedOfferProductRequest(
    int ProductId,
    string ProductCode,
    int? FurnitureDefinitionId,
    int Quantity,
    string Reason
) : IReasonedRequest;
