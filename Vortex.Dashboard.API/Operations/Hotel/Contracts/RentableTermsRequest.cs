using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record RentableTermsRequest(
    int FurnitureId,
    int Price,
    int CurrencyTypeId,
    int RentDurationSeconds,
    bool RequiresHc,
    string Reason
) : IReasonedRequest;
