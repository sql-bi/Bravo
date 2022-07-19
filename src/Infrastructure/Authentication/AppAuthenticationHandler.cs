namespace Sqlbi.Bravo.Infrastructure.Authentication
{
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Net.Http.Headers;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using System;
    using System.Net;
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;

    internal class AppAuthenticationHandler : AuthenticationHandler<AppAuthenticationSchemeOptions>
    {
        public AppAuthenticationHandler(IOptionsMonitor<AppAuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) 
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // RemoteIpAddress is null when Kestrel is behind a reverse proxy or the connection is not a TCP connection (like a Unix domain socket)
            var isLoopbackRequest = Context.Connection.RemoteIpAddress is not null && IPAddress.IsLoopback(Context.Connection.RemoteIpAddress);
            
            if (isLoopbackRequest && Request.Headers.TryGetValue(HeaderNames.Authorization, out var token))
            {
                var authenticated = AppEnvironment.ApiAuthenticationToken.Equals(token);
                if (authenticated == false)
                {
                    if (UserPreferences.Current.TemplateDevelopmentEnabled)
                    {
                        // TODO: enable
                        // if (Request.Path.StartsWithSegments(TemplateDevelopmentController.ControllerPathSegment))
                        {
                            authenticated = AppEnvironment.ApiAuthenticationTokenTemplateDevelopment.Equals(token);
                        }
                    }
                }
                
                if (authenticated)
                {
                    var claims = new[] 
                    {
                        new Claim(ClaimTypes.NameIdentifier, $@"{ Environment.UserDomainName }\{ Environment.UserName }"),
                        new Claim(ClaimTypes.Name, Environment.UserName)
                    };

                    var identity = new ClaimsIdentity(claims, authenticationType: nameof(AppAuthenticationHandler));
                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, authenticationScheme: Scheme.Name);

                    return Task.FromResult(AuthenticateResult.Success(ticket));
                }
            }

            return Task.FromResult(AuthenticateResult.Fail("Authorization failed"));
        }
    }
}
