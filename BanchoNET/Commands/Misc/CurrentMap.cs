using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils.Extensions;
using static BanchoNET.Commands.CommandHandlerMap;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("current",
        PlayerPrivileges.Unrestricted,
        "If in multiplayer lobby displays currently selected beatmap.",
        aliases: ["c"])]
    private async Task<(bool, string)> CurrentMap(string[] args)
    {
        var lobby = _playerCtx.Match;
        if (lobby == null)
            return (true, "You can only use this command in a multiplayer lobby.");
        
        var beatmap = await beatmaps.GetBeatmap(mapId: lobby.BeatmapId);
        return beatmap == null
            ? (false, BeatmapNotFound)
            : (false, $"Current map: {beatmap.Embed()}");
    }
}