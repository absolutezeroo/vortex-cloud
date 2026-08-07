using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Help;
using Vortex.Primitives.Help.Grains;
using Vortex.Runtime;

namespace Vortex.Players.Grains;

/// <summary>
/// The live roster of guides on duty. One grain for the hotel, so the counts every guide tool shows
/// are the same counts — a per-player tally would drift the moment two people went on duty at once.
/// </summary>
/// <remarks>
/// Nothing here is persisted, and that is the design rather than a shortcut: on duty means
/// "available right now". A guide who closes their client while on duty must not still be counted
/// tomorrow, and a restart must begin from an empty roster instead of one describing a hotel that
/// no longer exists. The cost of being wrong the other way is a queue that routes requests to
/// somebody who is not there.
/// </remarks>
[KeepAlive]
internal sealed class GuideDirectoryGrain : Grain, IGuideDirectoryGrain
{
    private readonly Dictionary<int, DutyRoles> _onDuty = [];

    private readonly record struct DutyRoles(bool Guide, bool Helper, bool Guardian);

    public Task<GuideDutySnapshot> SetDutyAsync(
        int playerId,
        bool onDuty,
        bool handlesGuideRequests,
        bool handlesHelperRequests,
        bool handlesGuardianRequests,
        CancellationToken ct
    )
    {
        if (playerId <= 0)
        {
            return Task.FromResult(Describe(onDuty: false));
        }

        // Off duty clears the roles outright. The client sends its three checkboxes on every change
        // including the one that turns duty off, and keeping them would leave someone counted as
        // covering a queue they have just stepped away from.
        if (!onDuty)
        {
            _onDuty.Remove(playerId);

            return Task.FromResult(Describe(onDuty: false));
        }

        _onDuty[playerId] = new DutyRoles(
            handlesGuideRequests,
            handlesHelperRequests,
            handlesGuardianRequests
        );

        return Task.FromResult(Describe(onDuty: true));
    }

    public Task<GuideDutySnapshot> GetStatusAsync(int playerId, CancellationToken ct) =>
        Task.FromResult(Describe(_onDuty.ContainsKey(playerId)));

    public Task ClearDutyAsync(int playerId, CancellationToken ct)
    {
        _onDuty.Remove(playerId);

        return Task.CompletedTask;
    }

    /// <summary>
    /// The counts overlap on purpose: one person covering all three queues is counted in all three,
    /// because what the tool asks is "is anybody watching this queue", not "how many people are
    /// here".
    /// </summary>
    private GuideDutySnapshot Describe(bool onDuty)
    {
        int guides = 0;
        int helpers = 0;
        int guardians = 0;

        foreach (DutyRoles roles in _onDuty.Values)
        {
            if (roles.Guide)
            {
                guides++;
            }

            if (roles.Helper)
            {
                helpers++;
            }

            if (roles.Guardian)
            {
                guardians++;
            }
        }

        return new GuideDutySnapshot
        {
            OnDuty = onDuty,
            GuidesOnDuty = guides,
            HelpersOnDuty = helpers,
            GuardiansOnDuty = guardians,
        };
    }
}
