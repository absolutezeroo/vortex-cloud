using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Bots;

/// <summary>
/// The bot feature (BotEntity/grain/ownership) doesn't exist in this codebase yet -- no player can
/// own a bot, so an empty inventory is the correct, truthful response rather than a stub.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record BotInventoryEventMessageComposer : IComposer { }
