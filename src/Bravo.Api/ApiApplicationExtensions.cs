using System;
using Bravo.Api.Events;
using Bravo.Api.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bravo.Api;

public static class ApiApplicationExtensions
{
    /// <summary>
    /// Adds the API to the application: what every request goes through, in its order, and the routes.
    /// </summary>
    public static WebApplication UseApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // First, so that an error of anything below is answered as a problem.
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.UseHostFiltering();
        app.UseRouting();

        // Before the token is asked for: a browser sends its preflight request without one.
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRoutes();

        return app;
    }

    // A route goes into the group, which declares the answers that any route can give.
    private static void MapRoutes(this IEndpointRouteBuilder app)
    {
        var routes = app.MapGroup(string.Empty)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        routes.MapOptions();
        routes.MapEvents();
    }
}
