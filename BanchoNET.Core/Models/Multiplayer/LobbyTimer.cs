using System.Timers;
using BanchoNET.Core.Utils.Extensions;
using Timer = System.Timers.Timer;

namespace BanchoNET.Core.Models.Multiplayer;

public class LobbyTimer : IDisposable
{
    private readonly MultiplayerMatch _match;
    private readonly string _alertMessage;
    
    private readonly bool _startGame;
    private readonly Func<Task>? _onStart;
    
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
    private uint _timerAlertIndex;
    private uint _secondsToFinish;

    public event Action<string>? OnSendMessage;
    
    public LobbyTimer(
        MultiplayerMatch match,
        uint secondsToFinish,
        bool startGame = false,
        Func<Task>? onStart = null
    ) {
        _match = match;
        _secondsToFinish = secondsToFinish == 0 ? 1 : secondsToFinish; // no one is going to notice that :tf:
        _startGame = startGame;
        _alertMessage = startGame
            ? "Match starting in {0} seconds."
            : "Countdown will end in {0} seconds.";
        _onStart = onStart;
        
        for (uint i = 0; i < _timerAlerts.Length; i++)
        {
            if (_timerAlerts[i] == secondsToFinish)
            {
                _timerAlertIndex = i + 1;
                break;
            }
            
            if (_timerAlerts[i] > secondsToFinish)
                continue;
            
            _timerAlertIndex = i;
            break;
        }
        
        if (secondsToFinish > 0)
            OnSendMessage?.Invoke(startGame
                ? $"Match will start in {secondsToFinish} seconds."
                : $"Countdown will end in {secondsToFinish} seconds.");
        
        _timer = new Timer
        {
            Interval = 1000,
            Enabled = true,
        };
        _timer.Elapsed += Tick;
    }
    
    //TODO helper methods for timer in multiplayerlobby instead of managing lobbytimer directly

    public void Stop()
    {
        _match.Timer = null;
        _timer.Stop();
        _timer.Dispose();
    }

    private void Tick(object? source, ElapsedEventArgs e)
    {
        _secondsToFinish--;
        
        if (_secondsToFinish != 0)
        {
            var secondsLeft = _timerAlerts[_timerAlertIndex];
            if (_secondsToFinish != secondsLeft) return;
            
            OnSendMessage?.Invoke(string.Format(_alertMessage, secondsLeft));
            _timerAlertIndex = Math.Min(++_timerAlertIndex, 8);
            return;
        }
        
        if (_startGame)
        {
            OnSendMessage?.Invoke($"Good luck, have fun!");
            _match.Start();
            _onStart?.Invoke();
        }
        else
        {
            OnSendMessage?.Invoke("Countdown has finished.");
        }
        
        Stop();
    }
    
    private bool _disposed;

    public void Dispose() {
        if(_disposed)  return;
        _timer.Stop();
        _timer.Elapsed -= Tick;
        _timer.Dispose();
        _disposed = true;
    }
}