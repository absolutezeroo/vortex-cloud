using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Help;
using Vortex.Primitives.Messages.Outgoing.Callforhelp;
using Vortex.Primitives.Moderation;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// "My sanctions", opened from the help window and again whenever the topic flow needs to know
/// whether the player is already on probation.
/// </summary>
/// <remarks>
/// Answered even when the history is empty. The client has no timeout on this: it opens the screen
/// on the reply, so no reply is a screen that never appears rather than one that says "nothing".
/// </remarks>
public class GetCfhStatusMessageHandler(ICfhTicketService tickets)
    : IMessageHandler<GetCfhStatusMessage>
{
    public async ValueTask HandleAsync(
        GetCfhStatusMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        ImmutableArray<PlayerSanctionSnapshot> sanctions = await tickets
            .GetSanctionHistoryAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new SanctionStatusEventMessageComposer
                {
                    Sanctions =
                    [
                        .. sanctions.Select(s => new SanctionRecord
                        {
                            SanctionType = new SanctionType
                            {
                                Name = s.TypeName,
                                DurationHours = s.DurationHours,
                            },
                            Description = s.Reason,
                            ShowsProbationDetails = s.IsActive,
                            ProbationHoursLeft = s.HoursLeft,
                            // The hotel has no escalation ladder to read a "next time" from, so the
                            // follow-up type is written empty rather than invented. It still has to
                            // be written: dropping it puts every later record four bytes out.
                            NextSanctionType = new SanctionType
                            {
                                Name = string.Empty,
                                DurationHours = 0,
                            },
                        }),
                    ],
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
