using CrewService.BlazorUI.Services;

namespace CrewService.BlazorUI.Middleware;

/// <summary>
/// Reads context-switcher cookies and hydrates <see cref="AppContextService"/>
/// so SSR pages can access the selected parent/railroad without JS interop.
/// </summary>
public class AppContextCookieMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext httpContext, AppContextService appContext)
    {
        if (httpContext.Request.Cookies.TryGetValue("ctx_parent_ctrlnbr", out var parentCtrlNbrStr)
            && long.TryParse(parentCtrlNbrStr, out var parentCtrlNbr)
            && httpContext.Request.Cookies.TryGetValue("ctx_parent_name", out var parentName)
            && !string.IsNullOrEmpty(parentName))
        {
            appContext.SetParent(parentCtrlNbr, parentName);

            if (httpContext.Request.Cookies.TryGetValue("ctx_railroad_ctrlnbr", out var railroadCtrlNbrStr)
                && long.TryParse(railroadCtrlNbrStr, out var railroadCtrlNbr)
                && httpContext.Request.Cookies.TryGetValue("ctx_railroad_name", out var railroadName)
                && !string.IsNullOrEmpty(railroadName))
            {
                appContext.SetRailroad(railroadCtrlNbr, railroadName);
            }
        }

        return next(httpContext);
    }
}
