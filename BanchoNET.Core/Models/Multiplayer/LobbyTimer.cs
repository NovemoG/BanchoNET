using System.Timers;
using Timer = System.Timers.Timer;

namespace BanchoNET.Core.Models.Multiplayer;

public sealed class LobbyTimer : IDisposable
{
    private readonly string _alertMessage;
    private readonly bool _startGame;
    private readonly Func<Task>? _onStart;
    private readonly Action<string>? _sendMessageCallback;
    
    private readonly Timer _timer;
    private readonly uint[] _timerAlerts = [
        60,
        30,
        15,
        10,
        5,
        4,
        3,
        2,
        1
    ];
    private int _timerAlertIndex;
    private int _secondsToFinish;
    
    public LobbyTimer(
        int secondsToFinish,
        bool startGame = false,
        Func<Task>? onStart = null,
        Action<string>? sendMessage = null
    ) {
        _secondsToFinish = secondsToFinish == 0 ? 1 : secondsToFinish; // no one is going to notice that :tf:
        _startGame = startGame;
        _alertMessage = startGame
            ? "Match starting in {0} seconds."
            : "Countdown will end in {0} seconds.";
        _onStart = onStart;
        _sendMessageCallback = sendMessage;
        
        for (var i = 0; i < _timerAlerts.Length; i++)
        {
            if (_timerAlerts[i] == secondsToFinish)
            {
                _timerAlertIndex = Math.Min(i + 1, _timerAlerts.Length - 1);
                break;
            }
            
            if (_timerAlerts[i] > secondsToFinish)
                continue;
            
            _timerAlertIndex = i;
            break;
        }

        var initialMsg = _startGame
            ? $"Match will start in {secondsToFinish} seconds."
            : $"Countdown will end in {secondsToFinish} seconds.";

        _sendMessageCallback?.Invoke(initialMsg);

        _timer = new Timer
        {
            Interval = 1000,
            Enabled = true,
        };
        _timer.Elapsed += Tick;
        _timer.Start();
    }

    public void Stop()
    {
        if (_timer.Enabled)
            _timer.Stop();
        _timer.Elapsed -= Tick;
        _timer.Dispose();
    }

    private void Tick(object? source, ElapsedEventArgs e)
    {
        var secondsLeft = Interlocked.Decrement(ref _secondsToFinish);

        if (secondsLeft > 0)
        {
            if (_timerAlertIndex < _timerAlerts.Length && secondsLeft == _timerAlerts[_timerAlertIndex])
            {
                _sendMessageCallback?.Invoke(string.Format(_alertMessage, secondsLeft));
                _timerAlertIndex = Math.Min(_timerAlertIndex + 1, _timerAlerts.Length - 1);
            }

            return;
        }
        
        if (_startGame)
        {
            _sendMessageCallback?.Invoke("Good luck, have fun!");
            if (_onStart != null)
            {
                _ = _onStart.Invoke();
            }
        }
        else
        {
            _sendMessageCallback?.Invoke("Countdown has finished.");
        }
        
        Stop();
    }
    
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        try
        {
            _timer.Stop();
            _timer.Elapsed -= Tick;
            _timer.Dispose();
        }
        catch { /* ignore */ }

        _disposed = true;
    }
}