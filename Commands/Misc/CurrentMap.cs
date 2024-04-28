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
    private async Task<(bool, string)> CurrentMap(params string[] args)
    {
        var lobby = _playerCtx.Lobby;
        if (lobby == null)
            return (true, "You can only use this command in a multiplayer lobby.");
        
        var beatmap = await beatmaps.GetBeatmap(mapId: lobby.BeatmapId);
        return beatmap == null
            ? (false, "Beatmap not found.")
            : (false, $"Current map: {beatmap.Embed()}");
    }
}