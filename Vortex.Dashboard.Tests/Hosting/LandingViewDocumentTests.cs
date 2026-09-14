using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using FluentAssertions;
using Vortex.Dashboard.API.Api.Hotel.Contracts;
using Vortex.Dashboard.API.Infrastructure;
using Xunit;

namespace Vortex.Dashboard.Tests.Hosting;

/// <summary>
/// The hotel view read out of flat client keys and written back into them.
/// </summary>
/// <remarks>
/// The fixture is a real hotel's <c>landing.view.*</c> block, not a minimal one, because every trap
/// in this format is a trap of coexistence: a slot whose <c>conf</c> is a schedule next to one whose
/// <c>conf</c> is an element list, a campaign code that looks like a background layer, dotted keys
/// that must not be mistaken for campaigns.
/// <para>
/// What is asserted first is that a save changes nothing when nothing was edited. A configuration
/// editor that quietly rewrites keys it does not understand is worse than the text field it replaces.
/// </para>
/// </remarks>
public sealed class LandingViewDocumentTests
{
    private const string Variables = """
        {
          "landing.view.HC3120131.conf": "caption,landing.view.HC312013.header;bodytext,landing.view.HC312013.body;catalogbutton,landing.view.HC312013.button,hc_membership",
          "landing.view.HC3120131.widget": "generic",
          "landing.view.bgobject.1": "xmas2012/snowflake_size_1;randomwalk;650;-20;6;36;39;0;1991",
          "landing.view.bgobject.2": "xmas2012/snowflake_size_2;randomwalk;1002;-40;7;34;47;0;1906",
          "landing.view.bonus.rare.image.uri": "${url.prefix}/c_images/web_promo_small/bonusbag25_4.png",
          "landing.view.common.etchingcolor": "000000",
          "landing.view.common.etchingposition": "top",
          "landing.view.common.textcolor": "ffffff",
          "landing.view.dynamic.slot.1.widget": "bonusrare",
          "landing.view.dynamic.slot.2.conf": "2026-01-08 12:00,jan26cf;2026-01-13 12:00,jul23bb",
          "landing.view.dynamic.slot.2.widget": "widgetcontainer",
          "landing.view.dynamic.slot.4.conf": "2026-01-09 12:00,jan26r2;2026-01-12 12:00,sep21diamond;2026-01-13 12:00,jan26fam",
          "landing.view.dynamic.slot.4.separator": "false",
          "landing.view.dynamic.slot.4.widget": "widgetcontainer",
          "landing.view.dynamic.slot.5.conf": "2026-01-05 12:00,HC3120131;2026-01-07 12:00,jan26cf1;2026-01-08 12:00,doublecredits231",
          "landing.view.dynamic.slot.5.ignore": "false",
          "landing.view.dynamic.slot.5.separator": "false",
          "landing.view.dynamic.slot.5.widget": "widgetcontainer",
          "landing.view.jan26cf.conf": "caption,landing.view.jan26cf.header;bodytext,landing.view.jan26cf.body;catalogbutton,landing.view.jan26cf.button,credit_exchange",
          "landing.view.jan26cf.layout": "bitmap.uri,${image.library.url}web_promo_small/spromo_DuckFigurine_Jan26.png;bitmap.x,50;bitmap.y,0;container.height,250",
          "landing.view.jan26cf.widget": "generic",
          "landing.view.background_gradient.uri": "${image.library.url}reception/background_gradient_sep26.png",
          "landing.view.background_left.uri": "${image.library.url}reception/background_left_sep26.png",
          "landing.view.background_right.uri": "${image.library.url}reception/reception_flathotel_backdrop_right.png",
          "landing_view.url": "https://help.habbo.com/home",
          "landing_view.use_web": "false",
          "landing_view_promo_slots": "2,4,5,6,3,1"
        }
        """;

    private const string Texts = """
        {
          "landing.view.jan26cf.header": "Duck Figurine",
          "landing.view.jan26cf.body": "Out now in the Credit Exchange.",
          "landing.view.jan26cf.button": "Go shopping"
        }
        """;

    /// <summary>
    /// Loading and saving without editing anything leaves the same keys with the same values, except
    /// for three the client cannot tell apart from their own absence.
    /// </summary>
    /// <remarks>
    /// <c>separator</c> and <c>ignore</c> are read with the client's <c>getBoolean</c>, for which a
    /// missing key and <c>"false"</c> are the same answer. Writing back only the ones that are on is
    /// the one normalisation this editor performs, and asserting it by name is the difference between
    /// a deliberate one and a bug nobody would notice until a diff got large.
    /// </remarks>
    [Fact]
    public void Round_trip_changes_nothing_but_the_no_op_booleans()
    {
        Dictionary<string, string> composed = LandingViewDocument.Compose(Read());
        Dictionary<string, string> original = Original();

        composed
            .Keys.Should()
            .BeEquivalentTo(
                original.Keys.Except([
                    "landing.view.dynamic.slot.4.separator",
                    "landing.view.dynamic.slot.5.separator",
                    "landing.view.dynamic.slot.5.ignore",
                ])
            );

        foreach ((string key, string value) in composed)
        {
            value.Should().Be(original[key], "{0} must survive a save untouched", key);
        }
    }

    /// <summary>A second save of an unedited configuration is identical to the first.</summary>
    [Fact]
    public void Round_trip_is_idempotent()
    {
        Dictionary<string, string> once = LandingViewDocument.Compose(Read());

        HotelViewConfig reloaded = LandingViewDocument.Read(
            ToJson(once),
            JsonNode.Parse(Texts) as JsonObject,
            default,
            default
        );

        LandingViewDocument.Compose(reloaded).Should().BeEquivalentTo(once);
    }

    /// <summary>
    /// A <c>widgetcontainer</c>'s conf is a schedule; a <c>generic</c>'s is an element list. Same
    /// key, same file, two formats.
    /// </summary>
    [Fact]
    public void Conf_is_read_according_to_the_widget()
    {
        HotelViewConfig config = Read();

        HotelViewSlot scheduled = config.Slots.Single(slot => slot.Number == 2);
        scheduled.Schedule.Should().HaveCount(2);
        scheduled.Schedule[0].StartsAt.Should().Be("2026-01-08 12:00");
        scheduled.Schedule[0].Code.Should().Be("jan26cf");
        scheduled.Elements.Should().BeEmpty();

        HotelViewCampaign campaign = config.Campaigns.Single(c => c.Code == "jan26cf");
        campaign.Elements.Should().HaveCount(3);
        campaign.Elements[2].Type.Should().Be("catalogbutton");
        campaign.Elements[2].Args.Should().Equal("landing.view.jan26cf.button", "credit_exchange");
    }

    /// <summary>An element whose first argument is a text key carries the words that key resolves to.</summary>
    [Fact]
    public void Elements_carry_their_text()
    {
        HotelViewCampaign campaign = Read().Campaigns.Single(c => c.Code == "jan26cf");

        campaign.Elements[0].Text.Should().Be("Duck Figurine");
        campaign.Elements[2].Text.Should().Be("Go shopping");
    }

    /// <summary>
    /// A campaign nothing schedules is reported as reachable by nobody, rather than looking healthy.
    /// </summary>
    [Fact]
    public void Campaigns_know_which_slots_reach_them()
    {
        IReadOnlyList<HotelViewCampaign> campaigns = Read().Campaigns;

        campaigns.Single(c => c.Code == "jan26cf").UsedBy.Should().Equal(2);
        campaigns.Single(c => c.Code == "HC3120131").UsedBy.Should().Equal(5);
    }

    /// <summary>
    /// Background layers and objects land in the default scene, and every layer is offered whether the
    /// file names it or not.
    /// </summary>
    [Fact]
    public void Scene_offers_every_layer_and_keeps_the_objects()
    {
        HotelViewScene scene = Read().Scenes.Single();

        scene.Code.Should().BeEmpty();
        scene.Layers.Should().HaveCount(9);
        scene
            .Layers.Single(layer => layer.Layer == "background_left")
            .Uri.Should()
            .Be("${image.library.url}reception/background_left_sep26.png");
        scene.Layers.Single(layer => layer.Layer == "background_back").Uri.Should().BeEmpty();

        HotelViewObject snowflake = scene.Objects[0];
        snowflake.Motion.Should().Be("randomwalk");
        snowflake.Asset.Should().Be("xmas2012/snowflake_size_1");
        snowflake.Fields.Should().Equal("650", "-20", "6", "36", "39", "0", "1991");
    }

    /// <summary>
    /// The three keys spelled <c>landing_view*</c> are surfaced as keys the client never reads.
    /// </summary>
    [Fact]
    public void Dead_keys_are_marked_unread()
    {
        IReadOnlyList<HotelViewExtra> extras = Read().Extras;

        extras
            .Where(extra => !extra.Read)
            .Select(extra => extra.Key)
            .Should()
            .BeEquivalentTo("landing_view.url", "landing_view.use_web", "landing_view_promo_slots");

        extras
            .Single(extra => extra.Key == "landing.view.bonus.rare.image.uri")
            .Read.Should()
            .BeTrue();
    }

    /// <summary>Removing a campaign from the configuration removes its keys, not just its values.</summary>
    [Fact]
    public void Dropping_a_campaign_drops_its_keys()
    {
        HotelViewConfig config = Read();

        HotelViewConfig without = config with
        {
            Campaigns = [.. config.Campaigns.Where(campaign => campaign.Code != "jan26cf")],
        };

        LandingViewDocument
            .Compose(without)
            .Keys.Should()
            .NotContain(key =>
                key.StartsWith("landing.view.jan26cf.", System.StringComparison.Ordinal)
            );
    }

    /// <summary>
    /// Only edited words are written back. An element that came back without text must not blank the
    /// string it points at.
    /// </summary>
    [Fact]
    public void Only_elements_carrying_words_write_texts()
    {
        HotelViewConfig config = Read();

        LandingViewDocument
            .ComposeTexts(config)
            .Should()
            .ContainKey("landing.view.jan26cf.header")
            .And.NotContainKey("landing.view.HC312013.header");
    }

    /// <summary>
    /// Every value the client would accept and then ignore is refused here, because here is the last
    /// place it can still be seen.
    /// </summary>
    [Theory]
    [InlineData("widget", "unknown_widget:genric")]
    [InlineData("element", "unknown_element:captoin")]
    [InlineData("layout", "unknown_layout_key:bitmap.left")]
    [InlineData("layer", "unknown_layer:background_middle")]
    [InlineData("motion", "unknown_motion:wobble")]
    [InlineData("index", "object_out_of_range:21")]
    public void Silent_client_failures_are_refused(string kind, string expected)
    {
        HotelViewConfig config = Read();
        HotelViewScene scene = config.Scenes[0];

        config = kind switch
        {
            "widget" => config with
            {
                Slots = [.. config.Slots.Select(slot => slot with { Widget = "genric" })],
            },
            "element" => config with
            {
                Campaigns =
                [
                    .. config.Campaigns.Select(campaign =>
                        campaign with
                        {
                            Elements = [new HotelViewElement("captoin", ["x"], null)],
                        }
                    ),
                ],
            },
            "layout" => config with
            {
                Campaigns =
                [
                    .. config.Campaigns.Select(campaign =>
                        campaign with
                        {
                            Layout = [new HotelViewLayoutValue("bitmap.left", "10")],
                        }
                    ),
                ],
            },
            "layer" => config with
            {
                Scenes =
                [
                    scene with
                    {
                        Layers = [new HotelViewBackground("background_middle", "x.png", true)],
                    },
                ],
            },
            "motion" => config with
            {
                Scenes = [scene with { Objects = [new HotelViewObject(1, "snow", "wobble", [])] }],
            },
            _ => config with
            {
                Scenes =
                [
                    scene with
                    {
                        Objects = [new HotelViewObject(21, "snow", "randomwalk", [])],
                    },
                ],
            },
        };

        LandingViewDocument.Validate(config).Should().Be(expected);
    }

    /// <summary>A clean configuration passes.</summary>
    [Fact]
    public void The_fixture_itself_is_valid() =>
        LandingViewDocument.Validate(Read()).Should().BeNull();

    private static HotelViewConfig Read() =>
        LandingViewDocument.Read(
            JsonNode.Parse(Variables) as JsonObject,
            JsonNode.Parse(Texts) as JsonObject,
            default,
            default
        );

    private static Dictionary<string, string> Original() =>
        (JsonNode.Parse(Variables) as JsonObject)!.ToDictionary(
            pair => pair.Key,
            pair => pair.Value!.GetValue<string>()
        );

    private static JsonObject ToJson(Dictionary<string, string> keys)
    {
        JsonObject map = [];

        foreach ((string key, string value) in keys)
        {
            map[key] = value;
        }

        return map;
    }
}
