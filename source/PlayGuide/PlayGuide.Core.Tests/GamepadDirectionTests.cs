using FluentAssertions;
using PlayGuide.Core.Contracts;
using PlayGuide.Core.Input;

namespace PlayGuide.Core.Tests;

[TestClass]
public class GamepadDirectionTests
{
    [TestMethod]
    public void ResolveStickDirection_InsideDeadzone_ReturnsNull()
    {
        SdlGamepadService.ResolveStickDirection(0.1f, -0.2f).Should().BeNull();
    }

    [DataTestMethod]
    [DataRow(0f, -0.9f, GamepadAction.NavigateUp)]
    [DataRow(0f, 0.9f, GamepadAction.NavigateDown)]
    [DataRow(-0.9f, 0f, GamepadAction.NavigateLeft)]
    [DataRow(0.9f, 0f, GamepadAction.NavigateRight)]
    public void ResolveStickDirection_MapsDominantAxis(float x, float y, GamepadAction expected)
    {
        SdlGamepadService.ResolveStickDirection(x, y).Should().Be(expected);
    }

    [TestMethod]
    public void ResolveStickDirection_VerticalWinsOnTie()
    {
        // Equal magnitudes -> vertical axis is preferred.
        SdlGamepadService.ResolveStickDirection(0.8f, -0.8f).Should().Be(GamepadAction.NavigateUp);
    }
}
