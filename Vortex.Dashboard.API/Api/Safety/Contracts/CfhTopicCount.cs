namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>What players report. The name falls back to the id for a topic no longer configured.</summary>
public sealed record CfhTopicCount(int TopicId, string TopicName, int Count);
