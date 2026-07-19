using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players.Snapshots;
using Vortex.Primitives.Players.Wallet;

namespace Vortex.Primitives.Players.Providers;

public interface ICurrencyTypeProvider
{
    public CurrencyTypeSnapshot? GetCurrencyType(int typeId);

    public CurrencyTypeSnapshot? GetCurrencyTypeByKind(CurrencyKind kind);

    public Task ReloadAsync(CancellationToken ct);
}
