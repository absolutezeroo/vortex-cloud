using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Catalog;

public record GetClubOffersMessage : IMessageEvent
{
    public ClubOfferRequestSourceType RequestSource { get; init; }
}
