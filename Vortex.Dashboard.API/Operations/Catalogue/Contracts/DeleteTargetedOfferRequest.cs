using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Catalogue.Contracts;

public sealed record DeleteTargetedOfferRequest(int OfferId, string Reason) : IReasonedRequest;
