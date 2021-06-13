using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorApp.Client.Authentication
{
    public class StaticWebAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;

        public StaticWebAuthenticationStateProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var authenticationData = await _httpClient.GetFromJsonAsync<AuthenticationData>("/.auth/me");
                if (authenticationData == null)
                    return NotAuthorized();

                var principal = authenticationData.ClientPrincipal;
                if (principal == null || principal.UserRoles == null)
                    return NotAuthorized();

                var roles = principal.UserRoles
                    .Except(new string[] { "anonymous" }, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                if (roles.Count == 0)
                    return NotAuthorized();


                var identity = new ClaimsIdentity(principal.IdentityProvider);
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, principal.UserId));
                identity.AddClaim(new Claim(ClaimTypes.Name, principal.UserDetails));
                identity.AddClaims(roles.Select(role => new Claim(ClaimTypes.Role, role)));

                var claimsPrincipal = new ClaimsPrincipal(identity);

                return new AuthenticationState(claimsPrincipal);
            }
            catch
            {
                return NotAuthorized();
            }
        }

        private AuthenticationState NotAuthorized()
        {
            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);

            return new AuthenticationState(principal);
        }
    }
}
