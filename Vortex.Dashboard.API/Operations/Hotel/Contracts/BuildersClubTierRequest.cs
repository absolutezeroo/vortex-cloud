using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

public sealed record BuildersClubTierRequest(int Level, int FurniLimit, string Reason)
    : IReasonedRequest;
