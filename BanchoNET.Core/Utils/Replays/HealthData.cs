namespace BanchoNET.Core.Utils.Replays;

public readonly struct HealthData(float health, long time)
{
    public readonly float Health = health;
    public readonly long Time = time;
}