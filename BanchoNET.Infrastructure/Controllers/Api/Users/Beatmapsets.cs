using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Users;

public partial class UsersController
{
    [HttpGet("{userId:int}/beatmapsets/{type}")]
    [AllowClientCredentials]
    public async Task<ActionResult> GetBeatmapsets(
        int userId,
        string type,
        [FromQuery] int offset,
        [FromQuery] int limit
    ) {
        switch (type)
        {
            case "most_played":
                var beatmapPlays = await beatmapsRepository.GetPlayerMostPlayedBeatmaps(userId, offset, limit);

                List<PlayCountCard> playCountList = [];
                playCountList.AddRange(
                    beatmapPlays.Select(play =>
                        new PlayCountCard
                        {
                            BeatmapId = play.BeatmapId, Count = play.Plays,
                            Beatmap = new PlayCountBeatmap(play.Beatmap),
                            Beatmapset = new BasicApiBeatmapset(play.Beatmap.Beatmapset)
                        }
                    )
                );

                return JsonSnake(playCountList);
            
            case "favourite":
                var beatmapsetIds = await beatmapsRepository.GetPlayerFavoriteBeatmapsets(userId, offset, limit);

                List<ApiBeatmapsetFull> favoriteBeatmapsets = [];
                foreach (var beatmapsetId in beatmapsetIds)
                {
                    var beatmapset = await Beatmaps.GetBeatmapsetFromApiOrCached(beatmapsetId);
                    if (beatmapset == null) continue;
                    
                    favoriteBeatmapsets.Add(beatmapset);
                }
                
                return JsonSnake(favoriteBeatmapsets);
            
            case "nominated":
            case "graveyard":
            case "pending":
            case "guest":
            case "loved":
            case "ranked":
                return JsonSnake(new List<ApiBeatmapset>());
            
            default:
                return BadRequest();
        }
    }
}