using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

public sealed record CurrencyRequest(
    int CurrencyId,
    string Name,
    int CurrencyType,
    int? ActivityPointType,
    bool Enabled,
    int StartingAmount,
    string Reason
) : IReasonedRequest;
