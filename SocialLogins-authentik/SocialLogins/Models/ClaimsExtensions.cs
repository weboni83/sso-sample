#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace SocialLogins.Models
{
    /// <summary>
    /// Authentication scheme names of the external login providers.
    /// </summary>
    public static class Providers
    {
        public const string GitHub = "GitHub";
        public const string Authentik = "authentik";
    }

    public static class ClaimsExtensions
    {
        /// <summary>
        /// Claim added at sign-in to record which provider the user authenticated with.
        /// </summary>
        public const string IdentityProviderClaim = "idp";

        public static string? FirstClaim(this IEnumerable<Claim>? claims, string type)
        {
            return claims?
                .Where(c => c.Type == type)
                .Select(c => c.Value)
                .FirstOrDefault();
        }

        public static string? AccessToken(this ClaimsPrincipal principal) =>
            principal.Claims.FirstClaim("access_token");

        public static string? IdentityProvider(this ClaimsPrincipal principal) =>
            principal.Claims.FirstClaim(IdentityProviderClaim);
    }
}
