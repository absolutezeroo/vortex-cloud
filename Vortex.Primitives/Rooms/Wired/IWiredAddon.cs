using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Rooms.Wired;

public interface IWiredAddon : IWiredBox
{
    public Task<bool> MutatePolicyAsync(IWiredProcessingContext ctx, CancellationToken ct);
    public Task BeforeEffectsAsync(IWiredProcessingContext ctx, CancellationToken ct);
    public Task AfterEffectsAsync(IWiredProcessingContext ctx, CancellationToken ct);

    /// <summary>
    /// The add-on's chance to rewrite a text an action is about to say, for the placeholder boxes.
    /// Returns the text untouched by default, so an add-on that is not about text costs a call and
    /// nothing else.
    /// </summary>
    public Task<string> ApplyToTextAsync(
        string text,
        IWiredExecutionContext ctx,
        CancellationToken ct
    );
}
