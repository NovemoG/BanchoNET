using System.Web;
using BanchoNET.Models;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BanchoNET.Controllers.OsuApi;

public partial class OsuController
{
    [HttpGet("osu-search.php")]
    public async Task<IActionResult> OsuSearch(
        [FromQuery(Name = "u")] string username,
        [FromQuery(Name = "h")] string passwordMD5,
        [FromQuery(Name = "r")] int rankedStatus,
        [FromQuery(Name = "q")] string query,
        [FromQuery(Name = "m")] int mode,
        [FromQuery(Name = "p")] int pageNumber)
    {
        if (await players.GetPlayerFromLogin(username, passwordMD5) == null)
            return Unauthorized("auth fail");
        
        var parameters = HttpUtility.ParseQueryString(string.Empty); //TODO might change if direct suddenly stops working
        parameters["amount"] = "100";
        parameters["offset"] = (pageNumber * 100).ToString();

        //TODO support for 'query'
        
        if (mode != -1)
            parameters["mode"] = mode.ToString();

        if (rankedStatus != 4)
            parameters["status"] = rankedStatus.ToApiFromDirect().ToString();

        var searchEndpoints = AppSettings.OsuDirectSearchEndpoints;
        var uriParams = parameters.ToString();
        string responseJson;
        int statusCode;
        
        var i = 0;
        do
        {
            var uriBuilder = new UriBuilder(searchEndpoints[i])
            {
                Query = uriParams
            };
            
            Console.WriteLine($"osu!direct request uri: {uriBuilder.Uri}");

            var response = await httpClient.GetAsync(uriBuilder.Uri);

            responseJson = await response.Content.ReadAsStringAsync();
            statusCode = (int)response.StatusCode;
            
            i++;
        } while (i < searchEndpoints.Count && statusCode != StatusCodes.Status200OK);

        if (statusCode != StatusCodes.Status200OK)
            return Responses.BytesContentResult("-1\nFailed to retrieve data from the beatmap mirror.");
        
        var osuDirectResponse = JsonConvert.DeserializeObject<List<DirectBeatmapSet>>(responseJson)!;

        var returnResponse = new List<string>
        {
            $"{(osuDirectResponse.Count == 100 ? 101 : osuDirectResponse.Count)}"
        };

        foreach (var mapset in osuDirectResponse)
        {
            if (mapset.Beatmaps == null || mapset.Beatmaps.Count == 0)
                continue;
            
            var hasVideo = mapset.HasVideo is "true" or "1";
            
            mapset.Beatmaps.Sort((a, b) => a.DifficultyRating.CompareTo(b.DifficultyRating));

            var beatmapsString = mapset.Beatmaps.Aggregate("",
                (current, map) => current + $"{map.DiffName.Replace('|', 'I')} [{map.DifficultyRating:F2}⭐]@{map.Mode},")[..^1];
            
            //TODO replace 10.0 with actual rating
            returnResponse.Add($"{mapset.SetId}.osz|{mapset.Artist}|{mapset.Title}|{mapset.Creator}|{mapset.RankedStatus}|10.0|{mapset.LastUpdate}|{mapset.SetId}|0|{hasVideo}|0|0|0|{beatmapsString}");
        }
        
        return Responses.BytesContentResult(string.Join("\n", returnResponse));
    }
}