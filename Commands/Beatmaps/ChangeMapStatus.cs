using BanchoNET.Attributes;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Objects.Privileges;
using BanchoNET.Utils;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    private readonly string[] _validStatuses = ["love", "qualify", "approve", "rank", "unrank"];
    
    [Command("map",
        Privileges.Nominator,
        "Changes the status of previously /np'd map. Syntax: map <status> <map/set>",
        "\nAvailable statuses: love, qualify, approve, rank, unrank" +
        "\nmrs - ranks whole set",
        ["mrs"])]
    private async Task<string> ChangeMapStatus(params string[] args)
    {
        if (args.Length == 0 && _commandBase != "mrs")
            return $"No parameter(s) provided. Syntax: {_prefix}map <status> <map/set>.";

        if (_commandBase == "mrs")
            return await ChangeStatus(BeatmapStatus.Ranked, true);
        
        if (args.Length == 1)
            return "Choose whether u want to rank one map or whole set.";
        
        var status = args[0].ToLower();
        if (!_validStatuses.Contains(status))
            return $"Invalid status provided. Available statuses: {string.Join(", ", _validStatuses)}.";

        return await ChangeStatus(status.ToBeatmapStatus(), args[1] != "map" && args[1] == "set");
    }

    private async Task<string> ChangeStatus(BeatmapStatus targetStatus, bool set)
    {
        var playerNp = _playerCtx.LastNp;
        
        if (playerNp == null || playerNp.SetId == -1)
            return "Please /np a map first.";
        
        var changed = set
            ? await beatmaps.ChangeBeatmapStatus(targetStatus, setId: playerNp.SetId) >= 1
            : await beatmaps.ChangeBeatmapStatus(targetStatus, beatmapId: playerNp.BeatmapId) == 1;
        
        return changed
            ? $"Successfully updated beatmap status to {targetStatus.ToString().Replace("P", " P")}"
            : $"{(set ? "Set" : "Map")} was not found or status is the same.";
    }
}