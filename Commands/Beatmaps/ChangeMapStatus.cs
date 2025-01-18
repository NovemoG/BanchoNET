using BanchoNET.Attributes;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Objects.Privileges;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;
using static BanchoNET.Utils.Maps.CommandHandlerMap;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("map",
        PlayerPrivileges.Nominator | PlayerPrivileges.Administrator,
        "Changes the status of previously /np'd map. Syntax: map <status> [<set>]",
        "\nAvailable statuses: love, qualify, approve, rank, unrank" +
        "\nmrs - ranks whole set",
        ["mrs"])]
    private async Task<string> ChangeMapStatus(string[] args)
    {
        if (args.Length == 0 && _commandBase != "mrs")
            return $"No parameter(s) provided. Syntax: {Prefix}map <status> [<set>].";

        if (_commandBase == "mrs")
            return await ChangeStatus(BeatmapStatus.Ranked, true);
        
        var status = args[0].ToLower();
        if (!ValidStatuses.Contains(status))
            return $"Invalid status provided. Available statuses: {string.Join(", ", ValidStatuses)}.";

        if (args.Length > 1 && args[1] != "set")
            return "Did you mean 'set'?";
        
        return await ChangeStatus(status.ToBeatmapStatus(), args is [_, "set"]);
    }

    private async Task<string> ChangeStatus(BeatmapStatus targetStatus, bool set)
    {
        var playerNp = _playerCtx.LastNp;
        
        if (playerNp == null || playerNp.SetId == -1)
            return "Please /np a map first.";
        
        var changed = set
            ? await beatmaps.UpdateBeatmapSetStatus(targetStatus, playerNp.SetId) >= 1
            : await beatmaps.UpdateBeatmapStatus(targetStatus, playerNp.BeatmapId) == 1;
        
        return changed                                                       // Quick fix for Latest Pending
            ? $"Successfully updated beatmap status to {targetStatus.ToString().Replace("P", " P")}"
            : $"{(set ? "Set" : "Map")} was not found or status is the same.";
    }
}