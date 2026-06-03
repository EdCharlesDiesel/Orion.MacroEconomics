using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Orion.MacroEconomics.ActionFilters;

public class CheckShowStatisticsHeader : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue("ShowStatistics", out var headerValue))
        {
            context.Result = new BadRequestResult();
            return;
        }

        if (!bool.TryParse(headerValue.ToString(), out var showStatistics) || !showStatistics)
        {
            context.Result = new BadRequestResult();
        }
    }
}
