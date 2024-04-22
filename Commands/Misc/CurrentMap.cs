using BanchoNET.Attributes;
using BanchoNET.Objects.Privileges;
using BanchoNET.Utils;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("current",
        Privileges.Unrestricted,
        "If in multiplayer lobby displays currently selected beatmap.",
        aliases: ["c"])]
    private async Task<string> CurrentMap(params string[] args)
    {
        var lobby = _playerCtx.Lobby;
        if (lobby == null)
            return "";
        
        var beatmap = await beatmaps.GetBeatmap(mapId: lobby.BeatmapId);
        return beatmap == null
            ? "Beatmap not found."
            : $"Current map: {beatmap.Embed()}";
    }
}