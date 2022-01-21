using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Infrastructure.Authentication
{
    internal class AppAuthenticationHandler : AuthenticationHandler<AppAuthenticationSchemeOptions>
    {
        public AppAuthenticationHandler(IOptionsMonitor<AppAuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) 
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var x = ClaimsPrincipal.Current;

            if (Request.Headers.TryGetValue(HeaderNames.Authorization, out var token))
            {
                if (AppConstants.ApiAuthenticationToken.Equals(token))
                {
                    var claims = new[] 
                    {
                        new Claim(ClaimTypes.NameIdentifier, $@"{ Environment.UserDomainName }\{ Environment.UserName }"),
                        new Claim(ClaimTypes.Name, Environment.UserName)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, authenticationType: nameof(AppAuthenticationHandler));
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                    var authenticationTicket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);

                    return Task.FromResult(AuthenticateResult.Success(authenticationTicket));
                }

            }

            return Task.FromResult(AuthenticateResult.Fail("Authorization failed"));
        }
    }
}
