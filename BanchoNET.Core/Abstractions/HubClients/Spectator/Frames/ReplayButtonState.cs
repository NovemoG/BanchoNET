namespace BanchoNET.Core.Abstractions.HubClients.Spectator.Frames;

[Flags]
public enum ReplayButtonState
{
    None = 0,
    Left1 = 1,
    Right1 = 2,
    Left2 = 4,
    Right2 = 8,
    Smoke = 16
}