using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

public sealed record DeleteMintableItemTypeRequest(int TypeId, string Reason) : IReasonedRequest;
