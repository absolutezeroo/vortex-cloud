using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteBuildersClubTierRequest(int TierId, string Reason) : IReasonedRequest;
