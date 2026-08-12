using System;
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Incoming.Poll;
using Vortex.Primitives.Messages.Outgoing.Poll;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Polls;
using Vortex.Primitives.Polls.Snapshots;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Polls;

/// <summary>
///     Locks the poll byte contract against the client's own parsers, re-derived from the AS3 for
///     WIN63-202607011411: <c>_SafeCls_3396</c> (contents), <c>_SafeCls_3698</c> (offer),
///     <c>_SafeCls_3271</c> (word-quiz question), <c>_SafeCls_3020</c> / <c>_SafeCls_2703</c> (the
///     two tally events) and <c>_SafeCls_3172</c> (the answer the client sends back).
///
///     Everything in this domain shipped empty: three parsers that read nothing, six composers with
///     no fields, and not one serializer registered in the map. The nesting is what makes the
///     layout worth pinning — a question's choice list is written only for the two choice types,
///     and a root question's follow-ups are written inline right after it, so one wrong count
///     desynchronises every field that follows.
/// </summary>
public sealed class PollWireLayoutTests
{
    private const int PollAnswerEvent = 3386;
    private const int PollStartEvent = 743;
    private const int PollRejectEvent = 1088;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    [Fact]
    public void PollOffer_WritesIdTypeHeadlineSummary()
    {
        ClientPacket packet = Serialize(
            new PollOfferEventMessageComposer
            {
                PollId = 7,
                PollType = "nps",
                Headline = "Got a minute?",
                Summary = "Three questions, no wrong answers.",
            }
        );

        packet.PopInt().Should().Be(7);
        packet.PopString().Should().Be("nps");
        packet.PopString().Should().Be("Got a minute?");
        packet.PopString().Should().Be("Three questions, no wrong answers.");
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void PollContents_WritesEachRootQuestionFollowedByItsOwnChildren()
    {
        ClientPacket packet = Serialize(
            new PollContentsEventMessageComposer
            {
                PollId = 7,
                StartMessage = "Here we go.",
                EndMessage = "Thanks!",
                NpsPoll = true,
                Questions =
                [
                    new PollQuestionSnapshot
                    {
                        Id = 1,
                        SortOrder = 0,
                        QuestionType = PollQuestionType.SingleChoice,
                        QuestionText = "Would you recommend us?",
                        QuestionCategory = 0,
                        QuestionAnswerType = 0,
                        Choices =
                        [
                            new PollChoiceSnapshot
                            {
                                Value = "10",
                                ChoiceText = "Definitely",
                                ChoiceType = 1,
                            },
                            new PollChoiceSnapshot
                            {
                                Value = "0",
                                ChoiceText = "No chance",
                                ChoiceType = 3,
                            },
                        ],
                        Children =
                        [
                            new PollQuestionSnapshot
                            {
                                Id = 2,
                                SortOrder = 0,
                                QuestionType = PollQuestionType.TextArea,
                                QuestionText = "What let you down?",
                                QuestionCategory = 3,
                                QuestionAnswerType = 0,
                            },
                        ],
                    },
                    new PollQuestionSnapshot
                    {
                        Id = 3,
                        SortOrder = 1,
                        QuestionType = PollQuestionType.TextLine,
                        QuestionText = "Anything else?",
                        QuestionCategory = 0,
                        QuestionAnswerType = 0,
                    },
                ],
            }
        );

        packet.PopInt().Should().Be(7);
        packet.PopString().Should().Be("Here we go.");
        packet.PopString().Should().Be("Thanks!");

        // The count is of ROOT questions only. Follow-ups are not counted here -- each root carries
        // its own child count immediately after its block.
        packet.PopInt().Should().Be(2);

        // Root 1: a choice question, so the choice list follows the answer count.
        ReadQuestionHeader(packet, id: 1, sortOrder: 0, type: 1, text: "Would you recommend us?");
        packet.PopInt().Should().Be(0); // category
        packet.PopInt().Should().Be(0); // answer type
        packet.PopInt().Should().Be(2); // answer count == choices written

        packet.PopString().Should().Be("10");
        packet.PopString().Should().Be("Definitely");
        packet.PopInt().Should().Be(1);
        packet.PopString().Should().Be("0");
        packet.PopString().Should().Be("No chance");
        packet.PopInt().Should().Be(3);

        // ...then this root's own follow-ups.
        packet.PopInt().Should().Be(1);
        ReadQuestionHeader(packet, id: 2, sortOrder: 0, type: 4, text: "What let you down?");
        packet.PopInt().Should().Be(3); // category -- matches the "No chance" choice type
        packet.PopInt().Should().Be(0);
        packet.PopInt().Should().Be(0); // a text question writes no choices

        // Root 2: a text question with no children.
        ReadQuestionHeader(packet, id: 3, sortOrder: 1, type: 3, text: "Anything else?");
        packet.PopInt().Should().Be(0);
        packet.PopInt().Should().Be(0);
        packet.PopInt().Should().Be(0);
        packet.PopInt().Should().Be(0); // child count

        packet.PopBoolean().Should().BeTrue();
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void PollContents_TextQuestionWritesNoChoicesEvenWhenSomeAreConfigured()
    {
        // The client reads the choice list only for types 1 and 2. If a text question ever carried
        // choices -- an operator switching a question's type after authoring it -- writing them
        // would shift every following field by three reads and corrupt the rest of the survey.
        ClientPacket packet = Serialize(
            new PollContentsEventMessageComposer
            {
                PollId = 1,
                StartMessage = string.Empty,
                EndMessage = string.Empty,
                NpsPoll = false,
                Questions =
                [
                    new PollQuestionSnapshot
                    {
                        Id = 5,
                        SortOrder = 0,
                        QuestionType = PollQuestionType.TextLine,
                        QuestionText = "Your name?",
                        QuestionCategory = 0,
                        QuestionAnswerType = 0,
                        Choices =
                        [
                            new PollChoiceSnapshot
                            {
                                Value = "leftover",
                                ChoiceText = "Leftover choice",
                                ChoiceType = 0,
                            },
                        ],
                    },
                ],
            }
        );

        packet.PopInt().Should().Be(1);
        packet.PopString().Should().BeEmpty();
        packet.PopString().Should().BeEmpty();
        packet.PopInt().Should().Be(1);

        ReadQuestionHeader(packet, id: 5, sortOrder: 0, type: 3, text: "Your name?");
        packet.PopInt().Should().Be(0);
        packet.PopInt().Should().Be(0);
        packet.PopInt().Should().Be(0); // NOT 1 -- the stray choice is dropped, not written

        packet.PopInt().Should().Be(0); // child count
        packet.PopBoolean().Should().BeFalse();
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void PollError_WritesNothingBeyondTheHeader()
    {
        // The client's parser returns false without reading a byte; anything written here would be
        // dead weight in the buffer.
        ClientPacket packet = Serialize(new PollErrorEventMessageComposer());

        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void Question_WritesTypeIdsDurationThenTheQuestionBlock()
    {
        ClientPacket packet = Serialize(
            new QuestionEventMessageComposer
            {
                PollType = "quiz",
                PollId = 4,
                QuestionId = 9,
                Duration = 30,
                Question = new PollQuestionSnapshot
                {
                    Id = 9,
                    SortOrder = 0,
                    QuestionType = PollQuestionType.SingleChoice,
                    QuestionText = "Pick one",
                    QuestionCategory = 0,
                    QuestionAnswerType = 0,
                    Choices =
                    [
                        new PollChoiceSnapshot
                        {
                            Value = "a",
                            ChoiceText = "Answer A",
                            ChoiceType = 0,
                        },
                    ],
                },
            }
        );

        packet.PopString().Should().Be("quiz");
        packet.PopInt().Should().Be(4);
        packet.PopInt().Should().Be(9);
        packet.PopInt().Should().Be(30);

        ReadQuestionHeader(packet, id: 9, sortOrder: 0, type: 1, text: "Pick one");
        packet.PopInt().Should().Be(0);
        packet.PopInt().Should().Be(0);
        packet.PopInt().Should().Be(1);
        packet.PopString().Should().Be("a");
        packet.PopString().Should().Be("Answer A");
        packet.PopInt().Should().Be(0);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void QuestionAnswered_WritesUserValueThenTheTally()
    {
        ClientPacket packet = Serialize(
            new QuestionAnsweredEventMessageComposer
            {
                UserId = 42,
                Value = "a",
                AnswerCounts =
                [
                    new PollAnswerCountSnapshot { Answer = "a", Count = 3 },
                    new PollAnswerCountSnapshot { Answer = "b", Count = 1 },
                ],
            }
        );

        packet.PopInt().Should().Be(42);
        packet.PopString().Should().Be("a");
        packet.PopInt().Should().Be(2);
        packet.PopString().Should().Be("a");
        packet.PopInt().Should().Be(3);
        packet.PopString().Should().Be("b");
        packet.PopInt().Should().Be(1);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void QuestionFinished_WritesQuestionIdThenTheTally()
    {
        ClientPacket packet = Serialize(
            new QuestionFinishedEventMessageComposer
            {
                QuestionId = 9,
                AnswerCounts = [new PollAnswerCountSnapshot { Answer = "a", Count = 5 }],
            }
        );

        packet.PopInt().Should().Be(9);
        packet.PopInt().Should().Be(1);
        packet.PopString().Should().Be("a");
        packet.PopInt().Should().Be(5);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void PollAnswerParser_ReadsPollQuestionThenTheCountedAnswers()
    {
        // The client pushes [pollId, questionId, length, ...answers]; a checkbox question legitimately
        // sends more than one. The parser used to read nothing at all and return an empty record.
        ClientPacket packet = BuildClientPacket(
            PollAnswerEvent,
            sp =>
            {
                sp.WriteInteger(7);
                sp.WriteInteger(1);
                sp.WriteInteger(2);
                sp.WriteString("rooms");
                sp.WriteString("groups");
            }
        );

        PollAnswerMessage message = Revision
            .Parsers[PollAnswerEvent]
            .Parse(packet)
            .Should()
            .BeOfType<PollAnswerMessage>()
            .Subject;

        message.PollId.Should().Be(7);
        message.QuestionId.Should().Be(1);
        message.Answers.Should().Equal("rooms", "groups");
    }

    [Fact]
    public void PollAnswerParser_AcceptsAnEmptyAnswerList()
    {
        // A skipped question sends a zero count. Reading a string anyway would throw and drop the
        // rest of the survey.
        ClientPacket packet = BuildClientPacket(
            PollAnswerEvent,
            sp =>
            {
                sp.WriteInteger(7);
                sp.WriteInteger(1);
                sp.WriteInteger(0);
            }
        );

        PollAnswerMessage message = Revision
            .Parsers[PollAnswerEvent]
            .Parse(packet)
            .Should()
            .BeOfType<PollAnswerMessage>()
            .Subject;

        message.Answers.Should().BeEmpty();
    }

    [Theory]
    [InlineData(PollStartEvent)]
    [InlineData(PollRejectEvent)]
    public void PollStartAndRejectParsers_ReadThePollId(int header)
    {
        ClientPacket packet = BuildClientPacket(header, sp => sp.WriteInteger(7));

        IMessageEvent message = Revision.Parsers[header].Parse(packet);

        int pollId = message switch
        {
            PollStartMessage start => start.PollId,
            PollRejectMessage reject => reject.PollId,
            _ => -1,
        };

        pollId.Should().Be(7);
    }

    /// <summary>Reads the four leading fields every question block starts with.</summary>
    private static void ReadQuestionHeader(
        ClientPacket packet,
        int id,
        int sortOrder,
        int type,
        string text
    )
    {
        packet.PopInt().Should().Be(id);
        packet.PopInt().Should().Be(sortOrder);
        packet.PopInt().Should().Be(type);
        packet.PopString().Should().Be(text);
    }

    private static ClientPacket Serialize<T>(T composer)
        where T : IComposer
    {
        byte[] bytes = Revision.Serializers[typeof(T)].Serialize(composer).ToArray();

        byte[] payload = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, payload, 0, payload.Length);

        return new ClientPacket(0, payload);
    }

    /// <summary>Writes packet fields the same way the client composer does, for parser input.</summary>
    private static ClientPacket BuildClientPacket(int header, Action<ServerPacket> write)
    {
        ServerPacket sp = new(header);
        write(sp);

        return new ClientPacket(header, sp.ToArray());
    }
}
