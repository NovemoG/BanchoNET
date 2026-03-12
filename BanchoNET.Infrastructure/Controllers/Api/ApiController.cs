using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Relationships;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

[ApiController]
[Route("api/v2")]
[Authorize]
[SubdomainAuthorize("osu")]
public partial class ApiController(
    IAuthService auth,
    IPlayersRepository players,
    IBeatmapsRepository beatmaps
) : ControllerBase
{
    protected readonly IAuthService Auth = auth;
    protected readonly IPlayersRepository Players = players;

    private static List<Relationship> PopulateRelationships(
        List<RelationshipDto> relationships,
        string type
    ) {
        List<Relationship> relationshipList = [];
        relationshipList.AddRange(
            from relationship in relationships
            let target = relationship.Target
            select new Relationship
            {
                Mutual = relationship.IsMutual,
                RelationType = type,
                TargetId = relationship.TargetId,
                Target = new TargetPlayer
                {
                    CountryCode = target.Country,
                    Id = target.Id,
                    IsActive = !target.Inactive,
                    IsBot = false,
                    IsDeleted = target.Deleted,
                    IsOnline = false, //TODO
                    IsSupporter = target.IsSupporter,
                    LastVisit = target.LastActivityTime,
                    PmFriendsOnly = target.PmFriendsOnly,
                    ProfileColour = null, //TODO
                    Username = target.Username,
                    Country = target.Country.ParseCountry(),
                    //Cover = new Cover(), TODO
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