using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteQuestRequest(int QuestId, string Reason) : IReasonedRequest;
