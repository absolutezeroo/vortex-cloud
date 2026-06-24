using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Room.Furniture;

public record ConfigureRentableSpaceMessage : IMessageEvent
{
    public required int FurnitureId { get; init; }
    public required int Price { get; init; }
    public required int CurrencyTypeId { get; init; }
    public required int RentDurationSeconds { get; init; }
    public required bool RequiresHc { get; init; }
}
