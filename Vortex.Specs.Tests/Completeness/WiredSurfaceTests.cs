using System.Collections.Generic;
using FluentAssertions;
using Vortex.Specs.Completeness;
using Xunit;

namespace Vortex.Specs.Tests.Completeness;

/// <summary>
/// Reading the wired box surface off both sides.
/// </summary>
/// <remarks>
/// Two text formats and one join, and every one of the three has a way of being quietly wrong: a
/// regex that matches nothing reports a family as entirely unimplemented, and a wrong enum reports
/// it the same way. The first run of this analyzer did exactly that to the variable family, which
/// is why the enum mapping has a test of its own below.
/// </remarks>
public class WiredSurfaceTests
{
    [Fact]
    public void ClientCodes_AreReadByNameAndValue()
    {
        IReadOnlyDictionary<int, string> codes = WiredSurfaceAnalyzer.ParseClientCodes(
            """
            package com.sulake.habbo.roomevents.wired_setup.actiontypes
            {
               public class ActionTypeCodes
               {
                  public static var TOGGLE_FURNI_STATE:int = 0;

                  public static var _SafeStr_10453:int = 3;

                  public static var GIVE_SCORE:int = 6;
               }
            }
            """
        );

        codes.Should().HaveCount(3);
        codes[0].Should().Be("TOGGLE_FURNI_STATE");
        codes[3].Should().Be("_SafeStr_10453");
        codes[6].Should().Be("GIVE_SCORE");
    }

    /// <summary>
    /// A repeated code in the client's own constants is the client's business. Taking the first
    /// keeps the table a property of the file rather than of enumeration order.
    /// </summary>
    [Fact]
    public void ARepeatedClientCode_KeepsTheFirst()
    {
        IReadOnlyDictionary<int, string> codes = WiredSurfaceAnalyzer.ParseClientCodes(
            """
            public static var FIRST:int = 4;
            public static var SECOND:int = 4;
            """
        );

        codes.Should().ContainSingle();
        codes[4].Should().Be("FIRST");
    }

    [Fact]
    public void AnObfuscatedConstant_IsNotAName()
    {
        WiredSurfaceAnalyzer.IsObfuscated("_SafeStr_10393").Should().BeTrue();
        WiredSurfaceAnalyzer.IsObfuscated("USER_VARIABLE").Should().BeFalse();
    }

    [Fact]
    public void EnumMembers_AreReadFromACSharpBody()
    {
        IReadOnlyDictionary<string, int> members = WiredSurfaceAnalyzer.ParseEnumMembers(
            """
            namespace Vortex.Primitives.Rooms.Enums.Wired;

            public enum WiredActionType
            {
                TOGGLE_FURNI_STATE = 0,
                SET_FURNI_STATE = 3,
                GIVE_SCORE = 6,
            }
            """
        );

        members.Should().HaveCount(3);
        members["SET_FURNI_STATE"].Should().Be(3);
    }

    [Fact]
    public void ALogicsWiredCode_YieldsItsEnumAndMember()
    {
        (string Enum, string Member)? code = WiredSurfaceAnalyzer.ParseVortexWiredCode(
            "    public override int WiredCode => (int)WiredActionType.BOT_TALK;"
        );

        code.Should().NotBeNull();
        code!.Value.Enum.Should().Be("WiredActionType");
        code.Value.Member.Should().Be("BOT_TALK");
    }

    [Fact]
    public void AClassWithNoWiredCode_YieldsNothing()
    {
        WiredSurfaceAnalyzer
            .ParseVortexWiredCode("public class FurnitureWiredActionLogic { }")
            .Should()
            .BeNull();
    }

    /// <summary>
    /// The trap the first run fell into. Variable boxes route on <c>WiredVariableBoxType</c>; the
    /// similarly named <c>WiredVariableType</c> says what kind of value a variable holds and has
    /// nothing to do with which box the client is configuring. Reading the wrong one reported the
    /// whole family as unimplemented, and the report said so rather than hiding it.
    /// </summary>
    [Fact]
    public void TheVariableFamily_RoutesOnTheBoxTypeEnum()
    {
        IReadOnlyDictionary<string, int> boxTypes = WiredSurfaceAnalyzer.ParseEnumMembers(
            """
            public enum WiredVariableBoxType
            {
                Furni = 0,
                User = 1,
                Global = 2,
            }
            """
        );

        (string Enum, string Member)? code = WiredSurfaceAnalyzer.ParseVortexWiredCode(
            "public override int WiredCode => (int)WiredVariableBoxType.User;"
        );

        code!.Value.Enum.Should().Be("WiredVariableBoxType");
        boxTypes[code.Value.Member].Should().Be(1, "the client's USER_VARIABLE is code 1");
    }
}
