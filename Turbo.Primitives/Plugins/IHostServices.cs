namespace Turbo.Primitives.Plugins;

public interface IHostServices
{
    T GetRequiredService<T>()
        where T : notnull;
}
