using System.Timers;
using BanchoNET.Objects.Multiplayer;
using Timer = System.Timers.Timer;

namespace BanchoNET.Utils;

public class LobbyTimer
{
    private readonly MultiplayerLobby _lobby;
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
    
    public LobbyTimer(
        MultiplayerLobby lobby,
        uint secondsToFinish,
        bool startGame = false,
        Func<Task>? onStart = null)
    {
        _lobby = lobby;
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
            _lobby.Chat.SendBotMessage(startGame
                ? $"Match will start in {secondsToFinish} seconds."
                : $"Countdown will end in {secondsToFinish} seconds.");
        
        _timer = new Timer
        {
            Interval = 1000,
            Enabled = true,
        };
        _timer.Elapsed += Tick;
    }

    public void Stop()
    {
        _lobby.Timer = null;
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
            
            _lobby.Chat.SendBotMessage(string.Format(_alertMessage, secondsLeft));
            _timerAlertIndex = Math.Min(++_timerAlertIndex, 8);
            return;
        }
        
        if (_startGame)
        {
            _lobby.Chat.SendBotMessage("Good luck, have fun!");
            _lobby.Start();
            _onStart?.Invoke();
        }
        else
        {
            _lobby.Chat.SendBotMessage("Countdown has finished.");
        }
        
        Stop();
    }
}