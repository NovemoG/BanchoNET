using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BanchoNET.Core.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace BanchoNET.Tests;

[TestFixture]
public class RequireUserFilterTests
{
    private static ClaimsPrincipal UserToken(int userId) => Authenticated(
        new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
        new Claim("scopes", "*")
    );
    
    private static ClaimsPrincipal ClientToken() => Authenticated(new Claim("scopes", "public"));

    private static ClaimsPrincipal Authenticated(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "Bearer"));

    private static IActionResult? Run(
        ClaimsPrincipal user,
        params RequireUserAttribute[] filters
    ) {
        var httpContext = new DefaultHttpContext { User = user };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutingContext(
            actionContext,
            filters.Cast<IFilterMetadata>().ToList(),
            new Dictionary<string, object?>(),
            controller: new object()
        );

        foreach (var filter in filters)
        {
            filter.OnActionExecuting(context);

            // MVC stops running the remaining filters once one short-circuits
            if (context.Result != null) break;
        }

        return context.Result;
    }

    [Test]
    public void ClientTokenIsRejectedWhenOnlyTheControllerRequirementApplies()
    {
        var result = Run(ClientToken(), new RequireUserAttribute());

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result!).StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
    }

    [Test]
    public void ClientTokenPassesWhenTheActionOptsOut()
    {
        var result = Run(ClientToken(), new RequireUserAttribute(), new AllowClientCredentialsAttribute());

        Assert.That(result, Is.Null);
    }

    [Test]
    public void UserTokenPassesEitherWay()
    {
        Assert.That(Run(UserToken(2), new RequireUserAttribute()), Is.Null);
        Assert.That(
            Run(UserToken(2), new RequireUserAttribute(), new AllowClientCredentialsAttribute()),
            Is.Null);
    }

    [Test]
    public void AnonymousRequestIsLeftToTheAuthorizationFilter()
    {
        var result = Run(new ClaimsPrincipal(new ClaimsIdentity()), new RequireUserAttribute());

        Assert.That(result, Is.Null);
    }
}