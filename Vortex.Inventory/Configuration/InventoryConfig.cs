namespace Vortex.Inventory.Configuration;

public class InventoryConfig
{
    public const string SECTION_NAME = "Vortex:Inventory";

    public int FurniPerFragment { get; init; } = 100;
}
