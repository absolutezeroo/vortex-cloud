using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteClaimRequest(int ClaimId, string Reason) : IReasonedRequest;
