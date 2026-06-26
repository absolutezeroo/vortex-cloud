using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Messages.Outgoing.Users;

namespace Turbo.PacketHandlers.Users;

public partial class ApproveNameMessageHandler : IMessageHandler<ApproveNameMessage>
{
    private const int MinLength = 1;
    private const int MaxLength = 15;

    public async ValueTask HandleAsync(
        ApproveNameMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        int result = Validate(message.Name);

        await ctx.SendComposerAsync(
                new ApproveNameMessageComposer { Result = result, ValidationInfo = string.Empty },
                ct
            )
            .ConfigureAwait(false);
    }

    private static int Validate(string name)
    {
        if (name.Length > MaxLength)
        {
            return 1;
        }

        if (name.Length < MinLength)
        {
            return 2;
        }

        if (!ValidCharsRegex().IsMatch(name))
        {
            return 3;
        }

        return 0;
    }

    [GeneratedRegex(@"^[\p{L}\p{N} \-]+$")]
    private static partial Regex ValidCharsRegex();
}
