using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteTargetedOfferProductRequest(int ProductId, string Reason)
    : IReasonedRequest;
