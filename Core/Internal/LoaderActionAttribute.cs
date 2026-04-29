using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace Vault.Core;

/// <summary>
/// Apply Attribute to all Loader-triggered POST requests. Any Action without an action tag returns 404
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class LoaderActionAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var req = context.HttpContext.Request;

        // Only enforce on POST
        if (req.Method == "POST")
        {
            if (!req.Headers.TryGetValue("X-Loader-Action", out var action) ||
                string.IsNullOrWhiteSpace(action))
            {
                context.Result = new NotFoundResult();
                return;
            }
        }

        await next();
    }
}
