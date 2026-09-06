using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record GamedataLanguageRemoveRequest(string Code, string Reason) : IReasonedRequest;
