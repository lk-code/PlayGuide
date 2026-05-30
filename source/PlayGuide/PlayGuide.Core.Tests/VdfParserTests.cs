using FluentAssertions;
using PlayGuide.Core.Steam;

namespace PlayGuide.Core.Tests;

[TestClass]
public class VdfParserTests
{
    [TestMethod]
    public void Parse_NestedBlocks_ReadsValues()
    {
        const string vdf = """
            "AppState"
            {
                "appid"      "440"
                "name"       "Team Fortress 2"
                "installdir" "Team Fortress 2"
                "UserConfig"
                {
                    "language" "english"
                }
            }
            """;

        var root = VdfParser.Parse(vdf);

        var state = root["AppState"];
        state.Should().NotBeNull();
        state!.GetString("appid").Should().Be("440");
        state.GetString("name").Should().Be("Team Fortress 2");
        state["UserConfig"]!.GetString("language").Should().Be("english");
    }

    [TestMethod]
    public void Parse_KeysAreCaseInsensitive()
    {
        var root = VdfParser.Parse("\"AppState\" { \"AppID\" \"10\" }");

        root["appstate"]!.GetString("appid").Should().Be("10");
    }

    [TestMethod]
    public void Parse_IgnoresLineCommentsAndConditionals()
    {
        const string vdf = """
            // a comment
            "root"
            {
                "key" "value" [$WINDOWS]
            }
            """;

        var root = VdfParser.Parse(vdf);

        root["root"]!.GetString("key").Should().Be("value");
    }

    [TestMethod]
    public void Parse_HandlesEscapedCharacters()
    {
        var root = VdfParser.Parse("\"path\" \"C:\\\\Games\\\\Steam\"");

        root.GetString("path").Should().Be(@"C:\Games\Steam");
    }

    [TestMethod]
    public void Parse_UnbalancedBraces_Throws()
    {
        var act = () => VdfParser.Parse("\"root\" { \"key\" \"value\"");

        act.Should().Throw<FormatException>();
    }

    [TestMethod]
    public void Parse_DanglingKey_Throws()
    {
        var act = () => VdfParser.Parse("\"key\"");

        act.Should().Throw<FormatException>();
    }
}
