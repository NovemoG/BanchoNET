using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Api.Relationships;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

[Route("api/v2")]
public partial class ApiController(
    IAuthService auth,
    IPlayersRepository players,
    ILazerPlayerService playerService,
    IBeatmapHandler beatmaps
) : ApiControllerBase(players, playerService, beatmaps)
{
    private async Task<List<Relationship>> PopulateRelationships(
        List<RelationshipReadDto> relationships,
        string type
    ) {
        var online = await PlayerService.FilterOnline(
            relationships.Select(r => r.TargetId).ToArray()
        );

        List<Relationship> relationshipList = [];
        relationshipList.AddRange(
            from relationship in relationships
            let target = relationship.Target
            select new Relationship
            {
                Mutual = relationship.Mutual,
                RelationType = type,
                TargetId = relationship.TargetId,
                Target = new TargetPlayer
                {
                    CountryCode = target.Country.ToUpper(),
                    Id = target.Id,
                    IsActive = !target.Inactive,
                    IsBot = false,
                    IsDeleted = target.Deleted,
                    IsOnline = online.Contains(target.Id),
                    IsSupporter = target.IsSupporter,
                    LastVisit = target.LastActivityTime,
                    PmFriendsOnly = target.PmFriendsOnly,
                    ProfileColour = null, //TODO
                    Username = target.Username,
                    Country = target.Country.ParseCountry(),
                    //Groups = [], TODO
                    //Statistics = new Statistics(), TODO
                    SupportLevel = target.SupporterLevel,
                    Team = null //TODO
                }
            }
        );

        return relationshipList;
    }
}