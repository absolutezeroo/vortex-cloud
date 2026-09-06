using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteStoreOfferRequest(int OfferId, string Reason) : IReasonedRequest;
