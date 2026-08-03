using BanchoNET.Core.Models.Api.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BanchoNET.Core.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class FormErrorAttribute : ActionFilterAttribute
{
    private readonly string _defaultGroup;

    public FormErrorAttribute(
        string defaultGroup = "user"
    ) {
        _defaultGroup = defaultGroup;
        Order = -2001;
    }

    public override void OnActionExecuting(
        ActionExecutingContext context
    ) {
        if (!context.ModelState.IsValid)
        {
            context.Result = new ObjectResult(FormError.FromModelState(context.ModelState, _defaultGroup))
            {
                StatusCode = StatusCodes.Status422UnprocessableEntity
            };

            return;
        }

        base.OnActionExecuting(context);
    }
}