using System;
using Orleans;

namespace Turbo.Primitives.Moderation;

[GenerateSerializer, Immutable]
public sealed record ChatlogRecordSnapshot
{
    [Id(0)]
    public required DateTime TimeStampUtc { get; init; }

    [Id(1)]
    public required int ChatterId { get; init; }

    [Id(2)]
    public required string ChatterName { get; init; }

    [Id(3)]
    public required string Message { get; init; }
}
