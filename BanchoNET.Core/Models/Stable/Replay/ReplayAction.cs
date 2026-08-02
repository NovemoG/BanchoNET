namespace BanchoNET.Core.Models.Stable.Replay;

public enum ReplayAction : byte
{
    Standard,
    NewSong,
    Skip,
    Completion,
    Fail,
    Pause,
    Unpause,
    SongSelect,
    WatchingOther
}