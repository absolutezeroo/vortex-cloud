using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

public sealed record DeleteClaimRequest(int ClaimId, string Reason) : IReasonedRequest;
