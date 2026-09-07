using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

public sealed record DeleteMintTokenOfferRequest(int OfferId, string Reason) : IReasonedRequest;
