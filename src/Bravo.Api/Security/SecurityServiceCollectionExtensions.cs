using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Bravo.Api.Security;

internal static class SecurityServiceCollectionExtensions
{
    /// <summary>
    /// The only name a request may give to the host: the loopback address that the server listens on. A page
    /// that reaches the port through another name, as after a DNS rebinding, is refused.
    /// </summary>
    public const string AllowedHost = "127.0.0.1";

    // How long a browser may keep the answer to a preflight request, which it sends before every request
    // with the token. Without a value it keeps the answer 5 seconds; no Chromium browser keeps it longer
    // than two hours, whatever the value.
    private static readonly TimeSpan s_preflightMaxAge = TimeSpan.FromHours(2);

    /// <summary>
    /// Adds what protects an API that any program and any page of this machine can reach: the token, the
    /// check of the host name, and the origins a browser may call from.
    /// </summary>
    public static IServiceCollection AddApiSecurity(this IServiceCollection services)
    {
        services.AddSingleton<ApiToken>();
        services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, ApiTokenAuthenticationHandler>(
            ApiTokenAuthenticationDefaults.AuthenticationScheme,
            configureOptions: null);

        // The fallback applies where nothing else was asked: to a route added later, and to a path that
        // matches no route.
        var tokenRequired = new AuthorizationPolicyBuilder(ApiTokenAuthenticationDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .Build();
        services.AddAuthorizationBuilder().SetFallbackPolicy(tokenRequired);

        services.AddHostFiltering(options =>
        {
            options.AllowedHosts = [AllowedHost];
            options.AllowEmptyHosts = false;

            // No page of the middleware: the answer gets the body of every other error.
            options.IncludeFailureMessage = false;
        });

        services.AddCors();
        services.AddOptions<CorsOptions>().Configure<IOptions<ApiOptions>>(AddDefaultPolicy);

        return services;
    }

    // Everything is named: no wildcard, and no credentials, because the token travels in a header that the
    // page sets itself.
    private static void AddDefaultPolicy(CorsOptions cors, IOptions<ApiOptions> api)
    {
        var origins = api.Value.AllowedOrigins.ToArray();
        cors.AddDefaultPolicy(policy => policy
            .WithOrigins(origins)
            .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete)
            .WithHeaders(HeaderNames.Authorization, HeaderNames.ContentType)
            .SetPreflightMaxAge(s_preflightMaxAge));
    }
}
