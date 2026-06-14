using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Players.Snapshots;
using Turbo.Primitives.Players.Wallet;

namespace Turbo.Primitives.Players.Providers;

public interface ICurrencyTypeProvider
{
    public CurrencyTypeSnapshot? GetCurrencyType(int typeId);

    public CurrencyTypeSnapshot? GetCurrencyTypeByKind(CurrencyKind kind);

    public Task ReloadAsync(CancellationToken ct);
}
