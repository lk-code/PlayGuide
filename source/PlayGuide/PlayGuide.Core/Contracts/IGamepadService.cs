namespace PlayGuide.Core.Contracts;

/// <summary>
/// A logical, UI-oriented gamepad action raised by <see cref="IGamepadService"/>.
/// Physical buttons and sticks are mapped to these high-level intents so the UI
/// layer can stay free of any input-library details.
/// </summary>
public enum GamepadAction
{
    /// <summary>Move focus up (D-pad up / left stick up).</summary>
    NavigateUp,

    /// <summary>Move focus down (D-pad down / left stick down).</summary>
    NavigateDown,

    /// <summary>Move focus left (D-pad left / left stick left).</summary>
    NavigateLeft,

    /// <summary>Move focus right (D-pad right / left stick right).</summary>
    NavigateRight,

    /// <summary>Activate the focused element (A / cross).</summary>
    Accept,

    /// <summary>Go back / cancel (B / circle).</summary>
    Back,

    /// <summary>Open the menu / settings (Start).</summary>
    Menu,
}

/// <summary>Event payload describing a single <see cref="GamepadAction"/>.</summary>
/// <param name="Action">The logical action that occurred.</param>
public readonly record struct GamepadActionEventArgs(GamepadAction Action);

/// <summary>
/// Cross-platform gamepad input source. Implementations poll connected controllers
/// on a background thread and raise <see cref="ActionTriggered"/> for navigation and
/// activation intents.
/// </summary>
public interface IGamepadService : IDisposable
{
    /// <summary>Raised when a logical gamepad action is detected. May fire off the UI thread.</summary>
    event EventHandler<GamepadActionEventArgs>? ActionTriggered;

    /// <summary>Starts polling connected controllers.</summary>
    void Start();

    /// <summary>Stops polling connected controllers.</summary>
    void Stop();
}
