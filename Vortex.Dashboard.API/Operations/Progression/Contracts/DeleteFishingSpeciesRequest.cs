using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

public sealed record DeleteFishingSpeciesRequest(int SpeciesId, string Reason) : IReasonedRequest;
