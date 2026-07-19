namespace Vortex.Catalog.Configuration;

public class CatalogConfig
{
    public const string SECTION_NAME = "Turbo:Catalog";

    public LtdRaffleConfig LtdRaffle { get; set; } = new();

    public class LtdRaffleConfig
    {
        public int DefaultWindowSeconds { get; set; } = 30;
        public int MaxEntriesPerPlayer { get; set; } = 1;
    }
}
