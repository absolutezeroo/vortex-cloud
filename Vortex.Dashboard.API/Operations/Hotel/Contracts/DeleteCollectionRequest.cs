using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

public sealed record DeleteCollectionRequest(int CollectionId, string Reason) : IReasonedRequest;
