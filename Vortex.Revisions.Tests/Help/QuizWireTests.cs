using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Incoming.Help;
using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Help;

/// <summary>
///     The Habbo Way and safety quizzes, re-derived from WIN63-202607011411: composers
///     <c>_SafeCls_2671</c> (1982) and <c>_SafeCls_3577</c> (1387), parsers <c>_SafeCls_2728</c>
///     (3999) and <c>_SafeCls_3737</c> (548), driven by <c>HabboWayQuizController</c>.
///
///     Both requests parsed to nothing and both answers were serialized as nothing, so all four
///     shapes are new. The one worth pinning hardest is that no text crosses the wire at all: the
///     client builds every question and option from its own localization, keyed on the numbers sent
///     here.
/// </summary>
public sealed class QuizWireTests
{
    private const int GetQuizQuestions = 1982;
    private const int PostQuizAnswers = 1387;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket Body(Type composerType, IComposer composer)
    {
        byte[] bytes = Revision.Serializers[composerType].Serialize(composer).ToArray();
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);
        return new ClientPacket(0, body);
    }

    [Fact]
    public void QuizData_WritesTheCodeThenTheQuestionNumbers()
    {
        ClientPacket packet = Body(
            typeof(QuizDataMessageComposer),
            new QuizDataMessageComposer { QuizCode = "HabboWay1", QuestionIds = [0, 1, 2] }
        );

        packet.PopString().Should().Be("HabboWay1");
        packet.PopInt().Should().Be(3);
        packet.PopInt().Should().Be(0);
        packet.PopInt().Should().Be(1);
        packet.PopInt().Should().Be(2);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void QuizResults_WritesAZeroCountOnAPass()
    {
        // The client shows the pass screen precisely when this list is empty, so the count still
        // has to be written — an empty body would leave it reading the next packet's bytes.
        ClientPacket packet = Body(
            typeof(QuizResultsMessageComposer),
            new QuizResultsMessageComposer
            {
                QuizCode = "SafetyQuiz1",
                WrongQuestionIds = ImmutableArray<int>.Empty,
            }
        );

        packet.PopString().Should().Be("SafetyQuiz1");
        packet.PopInt().Should().Be(0);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void QuizResults_NamesTheWrongQuestions()
    {
        ClientPacket packet = Body(
            typeof(QuizResultsMessageComposer),
            new QuizResultsMessageComposer { QuizCode = "HabboWay1", WrongQuestionIds = [3, 8] }
        );

        packet.PopString().Should().Be("HabboWay1");
        packet.PopInt().Should().Be(2);
        packet.PopInt().Should().Be(3);
        packet.PopInt().Should().Be(8);
        packet.Remaining.Should().Be(0);
    }

    [Fact]
    public void GetQuizQuestions_ReadsTheCodeItAsksFor()
    {
        // Not a bare "give me the quiz" ping: the client names HabboWay1 or SafetyQuiz1, and our
        // parser used to read neither.
        GetQuizQuestionsMessage message = (GetQuizQuestionsMessage)
            Revision.Parsers[GetQuizQuestions].Parse(Packet(w => w.String("SafetyQuiz1")));

        message.QuizCode.Should().Be("SafetyQuiz1");
    }

    [Fact]
    public void PostQuizAnswers_ReadsTheCodeThenOneAnswerPerQuestion()
    {
        PostQuizAnswersMessage message = (PostQuizAnswersMessage)
            Revision
                .Parsers[PostQuizAnswers]
                .Parse(Packet(w => w.String("HabboWay1").Int(3).Int(2).Int(1).Int(3)));

        message.QuizCode.Should().Be("HabboWay1");
        message.Answers.Should().Equal(2, 1, 3);
    }

    [Fact]
    public void PostQuizAnswers_RejectsADeclaredCountItDoesNotSend()
    {
        // A hostile client can declare any length; without the cap the parser would size a builder
        // from it before discovering the packet is three ints long.
        Action parse = () =>
            Revision
                .Parsers[PostQuizAnswers]
                .Parse(Packet(w => w.String("HabboWay1").Int(999_999)));

        parse.Should().Throw<InvalidDataException>();
    }

    private static ClientPacket Packet(Func<Writer, Writer> build)
    {
        return new ClientPacket(0, build(new Writer()).ToArray());
    }

    /// <summary>Minimal big-endian writer: the incoming parsers need real bytes, and the server
    /// packet writer prepends a header these tests must not carry.</summary>
    private sealed class Writer
    {
        private readonly List<byte> _bytes = [];

        public Writer Int(int value)
        {
            _bytes.Add((byte)(value >> 24));
            _bytes.Add((byte)(value >> 16));
            _bytes.Add((byte)(value >> 8));
            _bytes.Add((byte)value);
            return this;
        }

        public Writer String(string value)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(value);
            _bytes.Add((byte)(utf8.Length >> 8));
            _bytes.Add((byte)utf8.Length);
            _bytes.AddRange(utf8);
            return this;
        }

        public byte[] ToArray() => [.. _bytes];
    }
}
