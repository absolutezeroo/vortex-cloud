using Orleans;

namespace Vortex.Primitives.Moderation;

/// <summary>One labelled player in a <c>:uc</c> result. <paramref name="Label"/> is written to the
/// wire verbatim and shown as-is by the client, which does no interpretation of its own.</summary>
[GenerateSerializer, Immutable]
public readonly record struct UserClassificationEntry(int UserId, string UserName, string Label);
