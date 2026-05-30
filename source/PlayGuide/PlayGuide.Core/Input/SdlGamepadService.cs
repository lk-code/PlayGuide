using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PlayGuide.Core.Contracts;
using Silk.NET.SDL;
using Thread = System.Threading.Thread;

namespace PlayGuide.Core.Input;

/// <summary>
/// Cross-platform gamepad input via SDL2 (through Silk.NET). Polls connected game
/// controllers on a background thread and raises high-level
/// <see cref="GamepadAction"/> events. Button presses use SDL's event queue; the
/// left stick is sampled each tick with a deadzone, an initial delay and auto-repeat
/// so holding a direction keeps moving focus.
/// </summary>
/// <param name="logger">Diagnostics logger.</param>
public sealed unsafe class SdlGamepadService(ILogger<SdlGamepadService> logger) : IGamepadService
{
    private const float StickDeadzone = 0.5f;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(16);
    private static readonly TimeSpan RepeatInitialDelay = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan RepeatInterval = TimeSpan.FromMilliseconds(150);

    private readonly ILogger<SdlGamepadService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly List<nint> _controllers = [];
    private readonly object _gate = new();

    private Sdl? _sdl;
    private Thread? _thread;
    private CancellationTokenSource? _cts;

    // Left-stick direction tracking for auto-repeat.
    private float _stickX;
    private float _stickY;
    private GamepadAction? _heldDirection;
    private long _nextRepeatTicks;

    /// <inheritdoc />
    public event EventHandler<GamepadActionEventArgs>? ActionTriggered;

    /// <inheritdoc />
    public void Start()
    {
        lock (_gate)
        {
            if (_thread is not null)
            {
                return;
            }

            try
            {
                _sdl = Sdl.GetApi();
                if (_sdl.Init(Sdl.InitGamecontroller) != 0)
                {
                    _logger.LogWarning("SDL gamecontroller subsystem failed to initialise; gamepad input disabled.");
                    _sdl = null;
                    return;
                }
            }
            catch (Exception ex)
            {
                // Missing native SDL2 or unsupported platform: degrade to no-op.
                _logger.LogWarning(ex, "SDL could not be loaded; gamepad input disabled.");
                _sdl = null;
                return;
            }

            _cts = new CancellationTokenSource();
            _thread = new Thread(() => PollLoop(_cts.Token))
            {
                IsBackground = true,
                Name = "PlayGuide.Gamepad",
            };
            _thread.Start();
            _logger.LogInformation("Gamepad polling started.");
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        Thread? thread;
        lock (_gate)
        {
            if (_thread is null)
            {
                return;
            }
            _cts!.Cancel();
            thread = _thread;
            _thread = null;
        }

        thread.Join(TimeSpan.FromMilliseconds(500));

        lock (_gate)
        {
            CloseAllControllers();
            _sdl?.Quit();
            _sdl?.Dispose();
            _sdl = null;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void PollLoop(CancellationToken token)
    {
        var sdl = _sdl!;
        OpenConnectedControllers(sdl);

        while (!token.IsCancellationRequested)
        {
            DrainEvents(sdl);
            EvaluateStick();
            Thread.Sleep(PollInterval);
        }
    }

    private void DrainEvents(Sdl sdl)
    {
        Event e = default;
        while (sdl.PollEvent(&e) == 1)
        {
            switch ((EventType)e.Type)
            {
                case EventType.Controllerdeviceadded:
                    OpenController(sdl, e.Cdevice.Which);
                    break;

                case EventType.Controllerdeviceremoved:
                    // Instance/handle matching is not needed for v1: close and re-open
                    // whatever remains connected on the next added event.
                    CloseAllControllers();
                    OpenConnectedControllers(sdl);
                    break;

                case EventType.Controllerbuttondown:
                    HandleButton((GameControllerButton)e.Cbutton.Button);
                    break;

                case EventType.Controlleraxismotion:
                    HandleAxis((GameControllerAxis)e.Caxis.Axis, e.Caxis.Value);
                    break;
            }
        }
    }

    private void HandleButton(GameControllerButton button)
    {
        var action = button switch
        {
            GameControllerButton.A => GamepadAction.Accept,
            GameControllerButton.B => GamepadAction.Back,
            GameControllerButton.Start => GamepadAction.Menu,
            GameControllerButton.DpadUp => GamepadAction.NavigateUp,
            GameControllerButton.DpadDown => GamepadAction.NavigateDown,
            GameControllerButton.DpadLeft => GamepadAction.NavigateLeft,
            GameControllerButton.DpadRight => GamepadAction.NavigateRight,
            _ => (GamepadAction?)null,
        };

        if (action is { } value)
        {
            Raise(value);
        }
    }

    private void HandleAxis(GameControllerAxis axis, short value)
    {
        const float scale = 1f / 32767f;
        switch (axis)
        {
            case GameControllerAxis.Leftx:
                _stickX = value * scale;
                break;
            case GameControllerAxis.Lefty:
                _stickY = value * scale;
                break;
        }
    }

    /// <summary>Translates the current stick position into repeating navigation actions.</summary>
    private void EvaluateStick()
    {
        var direction = ResolveStickDirection(_stickX, _stickY);

        if (direction is null)
        {
            _heldDirection = null;
            return;
        }

        var now = Stopwatch.GetTimestamp();
        if (_heldDirection != direction)
        {
            _heldDirection = direction;
            _nextRepeatTicks = now + RepeatInitialDelay.Ticks * Stopwatch.Frequency / TimeSpan.TicksPerSecond;
            Raise(direction.Value);
        }
        else if (now >= _nextRepeatTicks)
        {
            _nextRepeatTicks = now + RepeatInterval.Ticks * Stopwatch.Frequency / TimeSpan.TicksPerSecond;
            Raise(direction.Value);
        }
    }

    /// <summary>
    /// Maps a stick position to a single dominant direction, or <c>null</c> inside the
    /// deadzone. Exposed for unit testing.
    /// </summary>
    public static GamepadAction? ResolveStickDirection(float x, float y)
    {
        if (MathF.Abs(x) < StickDeadzone && MathF.Abs(y) < StickDeadzone)
        {
            return null;
        }

        if (MathF.Abs(y) >= MathF.Abs(x))
        {
            // SDL Y axis points down (positive = down).
            return y > 0 ? GamepadAction.NavigateDown : GamepadAction.NavigateUp;
        }

        return x > 0 ? GamepadAction.NavigateRight : GamepadAction.NavigateLeft;
    }

    private void OpenConnectedControllers(Sdl sdl)
    {
        var count = sdl.NumJoysticks();
        for (var i = 0; i < count; i++)
        {
            if (sdl.IsGameController(i) == SdlBool.True)
            {
                OpenController(sdl, i);
            }
        }
    }

    private void OpenController(Sdl sdl, int joystickIndex)
    {
        var handle = sdl.GameControllerOpen(joystickIndex);
        if (handle is not null)
        {
            _controllers.Add((nint)handle);
            _logger.LogInformation("Gamepad connected (index {Index}).", joystickIndex);
        }
    }

    private void CloseAllControllers()
    {
        var sdl = _sdl;
        if (sdl is not null)
        {
            foreach (var handle in _controllers)
            {
                sdl.GameControllerClose((GameController*)handle);
            }
        }
        _controllers.Clear();
    }

    private void Raise(GamepadAction action) =>
        ActionTriggered?.Invoke(this, new GamepadActionEventArgs(action));

    /// <inheritdoc />
    public void Dispose() => Stop();
}
