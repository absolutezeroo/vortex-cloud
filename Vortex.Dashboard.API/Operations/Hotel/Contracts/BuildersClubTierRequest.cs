using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record BuildersClubTierRequest(int Level, int FurniLimit, string Reason)
    : IReasonedRequest;
