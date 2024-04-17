namespace BanchoNET.Objects.Replay;

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