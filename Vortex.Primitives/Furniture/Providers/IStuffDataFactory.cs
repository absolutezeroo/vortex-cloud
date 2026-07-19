using Vortex.Primitives.Furniture.StuffData;

namespace Vortex.Primitives.Furniture.Providers;

public interface IStuffDataFactory
{
    public IStuffData CreateStuffData(StuffDataType type);
    public IStuffData CreateStuffDataFromJson(StuffDataType type, string? jsonData);
    public IStuffData CreateStuffDataFromExtraData(StuffDataType type, IExtraData extraData);
}
