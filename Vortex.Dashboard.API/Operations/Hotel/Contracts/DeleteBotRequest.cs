using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteBotRequest(int BotId, string Reason) : IReasonedRequest;
