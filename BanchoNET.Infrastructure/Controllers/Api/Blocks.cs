using BanchoNET.Core.Models.Api.Relationships;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

/*
 "target_id": 2,
        "relation_type": "friend",
        "mutual": true,
        "target": {
            "avatar_url": "https://a.ppy.sh/2?1657169614.png",
            "country_code": "AU",
            "default_group": "default",
            "id": 2,
            "is_active": true,
            "is_bot": false,
            "is_deleted": false,
            "is_online": false,
            "is_supporter": true,
            "last_visit": "2026-03-03T14:33:18+00:00",
            "pm_friends_only": false,
            "profile_colour": "#3366FF",
            "username": "peppy",
            "country": {
                "code": "AU",
                "name": "Australia"
            },
            "cover": {
                "custom_url": "https://assets.ppy.sh/user-profile-covers/2/baba245ef60834b769694178f8f6d4f6166c5188c740de084656ad2b80f1eea7.jpeg",
                "url": "https://assets.ppy.sh/user-profile-covers/2/baba245ef60834b769694178f8f6d4f6166c5188c740de084656ad2b80f1eea7.jpeg",
                "id": null
            },
            "groups": [
                {
                    "colour": "#0066FF",
                    "has_listing": false,
                    "has_playmodes": false,
                    "id": 33,
                    "identifier": "ppy",
                    "is_probationary": false,
                    "name": "ppy",
                    "short_name": "PPY",
                    "playmodes": null
                },
                {
                    "colour": "#E45678",
                    "has_listing": true,
                    "has_playmodes": false,
                    "id": 11,
                    "identifier": "dev",
                    "is_probationary": false,
                    "name": "Developers",
                    "short_name": "DEV",
                    "playmodes": null
                }
            ],
            "statistics": {
                "count_100": 124490,
                "count_300": 687668,
                "count_50": 27470,
                "count_miss": 75614,
                "level": {
                    "current": 67,
                    "progress": 53
                },
                "global_rank": 769222,
                "global_rank_percent": 0.25324656298077036,
                "global_rank_exp": null,
                "pp": 1189.61,
                "pp_exp": 0,
                "ranked_score": 466015627,
                "hit_accuracy": 96.7969,
                "accuracy": 0.967969,
                "play_count": 7766,
                "play_time": 744696,
                "total_score": 2030853396,
                "total_hits": 839628,
                "maximum_combo": 746,
                "replays_watched_by_others": 16727,
                "is_ranked": true,
                "grade_counts": {
                    "ss": 15,
                    "ssh": 0,
                    "s": 67,
                    "sh": 0,
                    "a": 179
                }
            },
            "support_level": 3,
            "team": {
                "flag_url": "https://assets.ppy.sh/teams/flag/1/b46fb10dbfd8a35dc50e6c00296c0dc6172dffc3ed3d3a4b379277ba498399fe.png",
                "id": 1,
                "name": "mom?",
                "short_name": "MOM"
            }
        }

 */

public partial class ApiController
{
    [HttpGet("blocks")]
    public ActionResult<Relationship[]> GetBlocks() {
        if (!TryGetUserId(out var uid)) return Unauthorized();
        
        //TODO get blocks
        
        return new JsonResult(Array.Empty<Relationship>(), SnakeCaseNamingPolicy.Options);
    }
}