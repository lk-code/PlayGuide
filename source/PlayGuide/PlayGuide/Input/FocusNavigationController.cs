using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Input;

namespace PlayGuide.Input;

/// <summary>
/// Bridges <see cref="IGamepadService"/> actions to XAML focus navigation: directional
/// actions move keyboard focus (XYFocus, shared with the keyboard), and
/// <see cref="GamepadAction.Accept"/> invokes the focused control. Gamepad events arrive
/// off the UI thread and are marshalled onto it before touching XAML.
/// </summary>
/// <param name="gamepad">The gamepad input source.</param>
/// <param name="logger">Diagnostics logger.</param>
public sealed class FocusNavigationController(IGamepadService gamepad, ILogger<FocusNavigationController> logger)
{
    private readonly IGamepadService _gamepad = gamepad ?? throw new ArgumentNullException(nameof(gamepad));
    private readonly ILogger<FocusNavigationController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private Window? _window;

    /// <summary>Attaches to the given window and starts listening for gamepad input.</summary>
    public void Attach(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _gamepad.ActionTriggered += OnActionTriggered;
        _gamepad.Start();
    }

    private void OnActionTriggered(object? sender, GamepadActionEventArgs e)
    {
        var dispatcher = _window?.DispatcherQueue;
        dispatcher?.TryEnqueue(() => Handle(e.Action));
    }

    private void Handle(GamepadAction action)
    {
        try
        {
            // The parameterless-root TryMoveFocus overload is intentional and supported
            // on Uno; it moves focus relative to the currently focused element.
#pragma warning disable CS0618
            switch (action)
            {
                case GamepadAction.NavigateUp:
                    FocusManager.TryMoveFocus(FocusNavigationDirection.Up);
                    break;
                case GamepadAction.NavigateDown:
                    FocusManager.TryMoveFocus(FocusNavigationDirection.Down);
                    break;
                case GamepadAction.NavigateLeft:
                    FocusManager.TryMoveFocus(FocusNavigationDirection.Left);
                    break;
                case GamepadAction.NavigateRight:
                    FocusManager.TryMoveFocus(FocusNavigationDirection.Right);
                    break;
                case GamepadAction.Accept:
                    InvokeFocusedElement();
                    break;
                case GamepadAction.Back:
                case GamepadAction.Menu:
                    // Reserved for future navigation handling.
                    break;
            }
#pragma warning restore CS0618
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Gamepad action {Action} could not be handled.", action);
        }
    }

    private void InvokeFocusedElement()
    {
        if (_window?.Content?.XamlRoot is not { } xamlRoot)
        {
            return;
        }

        if (FocusManager.GetFocusedElement(xamlRoot) is not FrameworkElement focused)
        {
            return;
        }

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(focused);
        if (peer?.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoke)
        {
            invoke.Invoke();
        }
    }
}
