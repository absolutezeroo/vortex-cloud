using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Pets.Providers;

public interface IPetVocalProvider
{
    /// <summary>
    /// One of the lines this pet type says in this situation, chosen at random, or null when the
    /// type has none. Null means the pet stays quiet -- a pet with no dialogue written for it says
    /// nothing rather than something made up.
    /// </summary>
    string? TryGetLine(int petType, string vocalType);

    Task ReloadAsync(CancellationToken ct);
}
