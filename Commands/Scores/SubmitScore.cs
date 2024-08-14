using System.Globalization;
using AkatsukiPp;
using BanchoNET.Attributes;
using BanchoNET.Objects;
using BanchoNET.Objects.Privileges;
using BanchoNET.Objects.Scores;
using BanchoNET.Utils;
using static BanchoNET.Utils.CommandHandlerMaps;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("submit",
        Privileges.Submitter | Privileges.Administrator,
        "Submits a passed score to the server. Syntax: submit <beatmap_id> <mode> <score> <max_combo> <count300> " +
        "<count100> <count50> <misses> <geki> <katu> <grade> <date> [<perfect_fc>] [<mods>] [<username>]",
        "\nDate format is: yyyyMMddHHmmss. If optional values are not provided, they will be set to 0 (nomod for mods)." +
        "\nIf no username provided or player does not exist your name will be used. Please replace spaces with " +
        "underscores in username.")]
    private async Task<string> SubmitScore(params string[] args)
    {
        if (args.Length is < 12 or > 15)
            return $"Invalid number of parameters provided. Syntax: {_prefix}submit <beatmap_id> <mode> <score> " +
                   $"<max_combo> <count300> <count100> <count50> <misses> <geki> <katu> <grade> <date> [<perfect_fc>] " +
                   $"[<mods>] [<username>]";

        var parsedValues = new List<int>();

        for (var i = 0; i < 10; i++)
        {
            if (!int.TryParse(args[i], out var parsed))
                return "Invalid parameter(s) provided. All parameters must be integers.";

            parsedValues.Add(parsed);
        }

        var beatmap = await beatmaps.GetBeatmapWithId(parsedValues[0]);
        if (beatmap == null) return BeatmapNotFound;
        
        var player = await players.GetPlayerOrOffline(args.Length == 15 ? args[14] : _playerCtx.Username);
        if (player == null) return PlayerNotFound;
        
        if (!DateTime.TryParseExact(args[11], "yyyyMMddHHmmss", null, DateTimeStyles.None, out var date))
            return "Invalid date format provided. Date format is: yyyyMMddHHmmss.";

        if (args.Length == 13 && !bool.TryParse(args[12], out var perfectFc))
            perfectFc = args[12] == "1";
        else perfectFc = false;

        var mods = args.Length == 14 ? args[13].ParseMods((GameMode)parsedValues[1]) : Mods.None;
        
        var score = new Score
        {
            PlayerId = player.Id,
            ClientChecksum = "12345678901234567890123456789012",
            Count300 = parsedValues[4],
            Count100 = parsedValues[5],
            Count50 = parsedValues[6],
            Gekis = parsedValues[8],
            Katus = parsedValues[9],
            Misses = parsedValues[7],
            TotalScore = parsedValues[2],
            MaxCombo = parsedValues[3],
            Perfect = perfectFc,
            Grade = (Grade)Enum.Parse(typeof(Grade), args[10]),
            Mods = mods,
            Passed = true,
            Mode = (GameMode)parsedValues[1],
            ClientTime = date,
            ClientFlags = 0,
        };
        
        //TODO whole ass score submission man...

        return "Currently WIP.";
    }
}