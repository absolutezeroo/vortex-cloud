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
    /// <summary>
    /// The client's own entry points: <c>createHelpRequest(0)</c> and <c>(2)</c> are tour requests
    /// and go to guides, <c>(1)</c> is a help request and goes to helpers. Chat reviews never come
    /// through here — they have their own packet.
    /// </summary>
    private const int HelpRequestTypeHelper = 1;

    /// <summary>The client subtracts one before switching, so this is its "rejected" branch.</summary>
    private const int ErrorNoGuideAvailable = 1;

    private readonly Dictionary<int, DutyRoles> _onDuty = [];
    private readonly Dictionary<int, PendingRequest> _pendingByRequester = [];
    private readonly Dictionary<int, int> _requesterByOfferedGuide = [];
    private readonly Dictionary<int, GuideSessionSnapshot> _sessionsByPlayer = [];

    private readonly record struct DutyRoles(bool Guide, bool Helper, bool Guardian);

    private sealed record PendingRequest(int HelpRequestType, string Description)
    {
        /// <summary>Guides who have already turned this down, so it is never offered back to them.</summary>
        public HashSet<int> Declined { get; } = [];
    }

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

    public Task<GuideRequestOutcome> CreateRequestAsync(
        int requesterId,
        int helpRequestType,
        string description,
        CancellationToken ct
    )
    {
        if (requesterId <= 0)
        {
            return Task.FromResult(Failed(requesterId));
        }

        // One request per player. Without this a client that resends -- or a player who reopens the
        // dialog -- puts a second copy of the same request in front of a second guide, and the two
        // sessions then race to attach to one person.
        if (
            _pendingByRequester.ContainsKey(requesterId)
            || _sessionsByPlayer.ContainsKey(requesterId)
        )
        {
            return Task.FromResult(Failed(requesterId));
        }

        PendingRequest request = new(helpRequestType, description);

        int guideId = FindAvailableGuide(request, requesterId);

        if (guideId == 0)
        {
            return Task.FromResult(Failed(requesterId));
        }

        _pendingByRequester[requesterId] = request;
        _requesterByOfferedGuide[guideId] = requesterId;

        return Task.FromResult(Offered(requesterId, guideId, request));
    }

    public Task<GuideRequestOutcome> GuideDecidesAsync(
        int guideId,
        bool accepted,
        CancellationToken ct
    )
    {
        if (!_requesterByOfferedGuide.Remove(guideId, out int requesterId))
        {
            // Nothing was in front of them. A late answer to a request that has since gone
            // elsewhere lands here, and must not disturb whoever holds it now.
            return Task.FromResult(new GuideRequestOutcome());
        }

        if (!_pendingByRequester.TryGetValue(requesterId, out PendingRequest? request))
        {
            return Task.FromResult(new GuideRequestOutcome());
        }

        if (accepted)
        {
            _pendingByRequester.Remove(requesterId);

            GuideSessionSnapshot session = new()
            {
                RequesterId = requesterId,
                GuideId = guideId,
                HelpRequestType = request.HelpRequestType,
                Description = request.Description,
            };

            _sessionsByPlayer[requesterId] = session;
            _sessionsByPlayer[guideId] = session;

            return Task.FromResult(
                new GuideRequestOutcome { RequesterId = requesterId, Session = session }
            );
        }

        request.Declined.Add(guideId);

        int nextGuideId = FindAvailableGuide(request, requesterId);

        if (nextGuideId == 0)
        {
            _pendingByRequester.Remove(requesterId);

            return Task.FromResult(Failed(requesterId));
        }

        _requesterByOfferedGuide[nextGuideId] = requesterId;

        return Task.FromResult(Offered(requesterId, nextGuideId, request));
    }

    public Task<GuideSessionSnapshot?> GetSessionAsync(int playerId, CancellationToken ct) =>
        Task.FromResult(_sessionsByPlayer.GetValueOrDefault(playerId));

    public Task<int> GetPartnerAsync(int playerId, CancellationToken ct) =>
        Task.FromResult(PartnerOf(playerId));

    public Task<int> EndSessionAsync(int playerId, CancellationToken ct)
    {
        // A request that never found a guide is cleared too: a requester who walks away before
        // anyone accepted would otherwise leave their offer sitting in front of a guide.
        if (_pendingByRequester.Remove(playerId))
        {
            foreach ((int guideId, int requesterId) in _requesterByOfferedGuide)
            {
                if (requesterId == playerId)
                {
                    _requesterByOfferedGuide.Remove(guideId);
                    break;
                }
            }
        }

        int partnerId = PartnerOf(playerId);

        if (partnerId == 0)
        {
            return Task.FromResult(0);
        }

        _sessionsByPlayer.Remove(playerId);
        _sessionsByPlayer.Remove(partnerId);

        return Task.FromResult(partnerId);
    }

    private int PartnerOf(int playerId)
    {
        if (!_sessionsByPlayer.TryGetValue(playerId, out GuideSessionSnapshot? session))
        {
            return 0;
        }

        return session.RequesterId == playerId ? session.GuideId : session.RequesterId;
    }

    /// <summary>
    /// The first guide who covers this queue and is free to take it. Skips the requester -- nobody
    /// guides themselves -- anyone already in a session, anyone already holding an offer, and
    /// anyone who has turned this same request down.
    /// </summary>
    private int FindAvailableGuide(PendingRequest request, int requesterId)
    {
        bool wantsHelper = request.HelpRequestType == HelpRequestTypeHelper;

        foreach ((int playerId, DutyRoles roles) in _onDuty)
        {
            if (playerId == requesterId || request.Declined.Contains(playerId))
            {
                continue;
            }

            if (
                _sessionsByPlayer.ContainsKey(playerId)
                || _requesterByOfferedGuide.ContainsKey(playerId)
            )
            {
                continue;
            }

            if (wantsHelper ? roles.Helper : roles.Guide)
            {
                return playerId;
            }
        }

        return 0;
    }

    private static GuideRequestOutcome Offered(
        int requesterId,
        int guideId,
        PendingRequest request
    ) =>
        new()
        {
            RequesterId = requesterId,
            OfferedGuideId = guideId,
            HelpRequestType = request.HelpRequestType,
            Description = request.Description,
        };

    private static GuideRequestOutcome Failed(int requesterId) =>
        new() { RequesterId = requesterId, ErrorCode = ErrorNoGuideAvailable };

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
