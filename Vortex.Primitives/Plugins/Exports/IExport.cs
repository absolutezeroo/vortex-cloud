using System;
using System.Threading.Tasks;

namespace Vortex.Primitives.Plugins.Exports;

public interface IExport<T>
    where T : class
{
    T Current { get; }
    IDisposable Subscribe(Action<T> onSwap);
    Task SwapAsync(T instance);
}
