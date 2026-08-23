using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents;

[GenerateSerializer, Immutable]
public sealed record WiredValidationErrorEventMessageComposer : IComposer
{
    [Id(0)]
    public required string LocalizationKey { get; init; }

    [Id(1)]
    public required List<(string, string)> Parameters { get; init; }
}
