using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record MintTokenOfferRequest(
    int OfferId,
    string ProductCode,
    int SilverPrice,
    int AmountTokens,
    bool Enabled,
    int SortOrder,
    string Reason
) : IReasonedRequest;
