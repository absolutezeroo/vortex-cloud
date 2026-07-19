namespace Vortex.Primitives.Rooms.Object.Logic.Furniture;

/// <summary>
/// Sentinel <c>param</c> values for <see cref="IFurnitureLogic.OnUseAsync"/> shared between
/// <c>ThrowDiceMessageHandler</c>/<c>DiceOffMessageHandler</c> (which message arrived selects the
/// action) and <c>FurnitureDiceLogic</c> (which owns the actual state change) — never serialized
/// on the wire, purely an internal call-site convention.
/// </summary>
public static class FurnitureDiceAction
{
    public const int TurnOff = 0;
    public const int Roll = 1;
}
