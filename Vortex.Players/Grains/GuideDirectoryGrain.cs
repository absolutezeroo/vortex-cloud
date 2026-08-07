using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
internal sealed class GuideDirectoryGrain(IGrainFactory grainFactory) : Grain, IGuideDirectoryGrain
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    /// <summary>
    /// The client's own entry points: <c>createHelpRequest(0)</c> and <c>(2)</c> are tour requests
    /// and go to guides, <c>(1)</c> is a help request and goes to helpers. Chat reviews never come
    /// through here — they have their own packet.
    /// </summary>
    private const int HelpRequestTypeHelper = 1;

    /// <summary>The client subtracts one before switching, so this is its "rejected" branch.</summary>
    private const int ErrorNoGuideAvailable = 1;

    /// <summary>How long a guardian has to answer an offer, and how long to vote once they have
    /// opened the excerpt. Both match the countdowns their client is told to run.</summary>
    private const long AcceptanceTimeoutMs = 30_000;
    private const long VotingTimeoutMs = 120_000;

    private readonly Dictionary<int, DutyRoles> _onDuty = [];
    private readonly Dictionary<int, PendingRequest> _pendingByRequester = [];
    private readonly Dictionary<int, int> _requesterByOfferedGuide = [];
    private readonly Dictionary<int, GuideSessionSnapshot> _sessionsByPlayer = [];
    private readonly Dictionary<int, ChatReview> _reviewsByReporter = [];
    private readonly Dictionary<int, int> _reporterByGuardian = [];

    private readonly record struct DutyRoles(bool Guide, bool Helper, bool Guardian);

    private sealed record PendingRequest(int HelpRequestType, string Description)
    {
        /// <summary>Guides who have already turned this down, so it is never offered back to them.</summary>
        public HashSet<int> Declined { get; } = [];
    }

    private sealed record ChatReview(int ReporterId, string ChatRecord, long OfferedAtMs)
    {
        /// <summary>Offered to them; they have not answered yet.</summary>
        public HashSet<int> Offered { get; } = [];

        /// <summary>Took it, and owes a vote. The value is when they took it, so a guardian who
        /// accepts late still gets their full time to read the excerpt.</summary>
        public Dictionary<int, long> Accepted { get; } = [];

        public Dictionary<int, int> Votes { get; } = [];
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

    public async Task<ChatReviewOutcome> CreateChatReviewAsync(
        int reporterId,
        string chatRecord,
        CancellationToken ct
    )
    {
        long nowMs = NowMs();

        if (reporterId <= 0 || _reviewsByReporter.ContainsKey(reporterId))
        {
            return new ChatReviewOutcome();
        }

        ChatReview review = new(reporterId, chatRecord, nowMs);

        foreach ((int playerId, DutyRoles roles) in _onDuty)
        {
            // The reporter is skipped even if they are a guardian on duty: nobody judges the chat
            // they themselves reported.
            if (
                roles.Guardian
                && playerId != reporterId
                && !_reporterByGuardian.ContainsKey(playerId)
            )
            {
                review.Offered.Add(playerId);
                _reporterByGuardian[playerId] = reporterId;
            }
        }

        if (review.Offered.Count == 0)
        {
            return new ChatReviewOutcome();
        }

        _reviewsByReporter[reporterId] = review;

        return await SendAsync(
            new ChatReviewOutcome { OfferedTo = [.. review.Offered], ChatRecord = chatRecord }
        );
    }

    public async Task<ChatReviewOutcome> ChatReviewDecideAsync(
        int guardianId,
        bool accepted,
        CancellationToken ct
    )
    {
        long nowMs = NowMs();

        if (
            !TryFindReview(guardianId, out ChatReview? review) || !review.Offered.Remove(guardianId)
        )
        {
            return new ChatReviewOutcome();
        }

        if (!accepted)
        {
            _reporterByGuardian.Remove(guardianId);

            // Everyone else may already have voted, so a decline can be what completes it.
            return await SendAsync(TryResolve(review));
        }

        review.Accepted[guardianId] = nowMs;

        // The excerpt is theirs only now that they have taken it.
        await ChatReviewDispatch
            .SendRecordAsync(_grainFactory, guardianId, review.ChatRecord)
            .ConfigureAwait(true);

        return await SendAsync(
            new ChatReviewOutcome
            {
                Participants = [.. review.Accepted.Keys],
                ChatRecord = review.ChatRecord,
            }
        );
    }

    public async Task<ChatReviewOutcome> ChatReviewVoteAsync(
        int guardianId,
        int vote,
        CancellationToken ct
    )
    {
        if (
            !TryFindReview(guardianId, out ChatReview? review)
            || !review.Accepted.ContainsKey(guardianId)
        )
        {
            return new ChatReviewOutcome();
        }

        // Last vote wins rather than first: the client lets a guardian change their mind before the
        // others finish, and rejecting the change would show them a verdict they no longer hold.
        review.Votes[guardianId] = vote;

        return await SendAsync(TryResolve(review));
    }

    public async Task<ChatReviewOutcome> ChatReviewDetachAsync(int guardianId, CancellationToken ct)
    {
        if (!TryFindReview(guardianId, out ChatReview? review))
        {
            return new ChatReviewOutcome();
        }

        review.Offered.Remove(guardianId);
        review.Accepted.Remove(guardianId);
        review.Votes.Remove(guardianId);
        _reporterByGuardian.Remove(guardianId);

        return await SendAsync(TryResolve(review));
    }

    /// <summary>
    /// Closes the review once nobody still owes a vote, and hands back the verdict.
    /// </summary>
    /// <remarks>
    /// "Owes a vote" is everyone who accepted and has not voted, plus everyone still deciding. A
    /// guardian who accepts and then goes quiet therefore holds the review open — which is what the
    /// acceptance and voting timeouts the client counts down are for, and they are not enforced
    /// here yet.
    /// </remarks>
    private ChatReviewOutcome TryResolve(ChatReview review)
    {
        bool outstanding = review.Offered.Count > 0 || review.Accepted.Count > review.Votes.Count;

        if (outstanding)
        {
            return new ChatReviewOutcome
            {
                Participants = [.. review.Accepted.Keys],
                ChatRecord = review.ChatRecord,
            };
        }

        _reviewsByReporter.Remove(review.ReporterId);

        foreach (int guardianId in review.Accepted.Keys)
        {
            _reporterByGuardian.Remove(guardianId);
        }

        if (review.Votes.Count == 0)
        {
            // Everyone walked away. There is no verdict to report and nothing to send.
            return new ChatReviewOutcome();
        }

        int abusive = review.Votes.Values.Count(v => v != 0);
        int acceptable = review.Votes.Count - abusive;

        return new ChatReviewOutcome
        {
            Participants = [.. review.Votes.Keys],
            Result = new ChatReviewResultSnapshot
            {
                // A tie reads as "not abusive": condemning a chat needs a majority, not the absence
                // of one.
                WinningVote = abusive > acceptable ? 1 : 0,
                Votes = [.. review.Votes.Values],
                VotesByGuardian = review.Votes.ToImmutableDictionary(),
            },
        };
    }

    /// <summary>
    /// Gives up on guardians who stopped answering, then closes any review that no longer has
    /// anybody to wait for.
    /// </summary>
    /// <remarks>
    /// Takes the time rather than reading it, so a test can move the clock instead of sleeping. Two
    /// separate deadlines: one to answer the offer at all, and a longer one to vote once the excerpt
    /// has been opened -- a guardian who has taken it is reading, and deserves more than one who
    /// never responded.
    /// </remarks>
    /// <summary>
    /// Sends an outcome and hands it back, so every caller both acts and reports through one line.
    /// </summary>
    private async Task<ChatReviewOutcome> SendAsync(ChatReviewOutcome outcome)
    {
        await ChatReviewDispatch
            .DeliverAsync(_grainFactory, outcome, CancellationToken.None)
            .ConfigureAwait(true);

        return outcome;
    }

    internal List<ChatReviewOutcome> SweepChatReviewTimeouts(long nowMs)
    {
        List<ChatReviewOutcome> resolved = [];

        foreach (ChatReview review in _reviewsByReporter.Values.ToList())
        {
            bool changed = false;

            foreach (int guardianId in review.Offered.ToList())
            {
                if (nowMs - review.OfferedAtMs >= AcceptanceTimeoutMs)
                {
                    review.Offered.Remove(guardianId);
                    _reporterByGuardian.Remove(guardianId);
                    changed = true;
                }
            }

            foreach ((int guardianId, long acceptedAtMs) in review.Accepted.ToList())
            {
                if (review.Votes.ContainsKey(guardianId) || nowMs - acceptedAtMs < VotingTimeoutMs)
                {
                    continue;
                }

                // Silence is not a vote. They are dropped rather than counted, so a guardian who
                // wandered off cannot tip a verdict by doing nothing.
                review.Accepted.Remove(guardianId);
                _reporterByGuardian.Remove(guardianId);
                changed = true;
            }

            if (!changed)
            {
                continue;
            }

            ChatReviewOutcome outcome = TryResolve(review);

            if (!outcome.Nothing)
            {
                resolved.Add(outcome);
            }
        }

        return resolved;
    }

    private static long NowMs() => Environment.TickCount64;

    public override Task OnActivateAsync(CancellationToken ct)
    {
        // Slow on purpose. Nothing here is time-critical -- the deadlines are tens of seconds -- and
        // a hotel with no review open must not pay for a tick that finds nothing.
        this.RegisterGrainTimer<object?>(
            async (_, timerCt) =>
            {
                foreach (ChatReviewOutcome outcome in SweepChatReviewTimeouts(NowMs()))
                {
                    await ChatReviewDispatch
                        .DeliverAsync(_grainFactory, outcome, timerCt)
                        .ConfigureAwait(true);
                }
            },
            null,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5)
        );

        return base.OnActivateAsync(ct);
    }

    private bool TryFindReview(int guardianId, out ChatReview review)
    {
        review = null!;

        return _reporterByGuardian.TryGetValue(guardianId, out int reporterId)
            && _reviewsByReporter.TryGetValue(reporterId, out review!);
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
