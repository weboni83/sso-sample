using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SocialLogins.Models;

namespace SocialLogins
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Step 0.
            //  From the root of the solution, use the terminal to add secrets to Project User Secrets.
            //  Each provider is only registered when its settings are present.
            //
            //  GitHub
            // ‣ dotnet user-secrets set github:clientId "<from github>" --project SocialLogins
            // ‣ dotnet user-secrets set github:clientSecret "<from github>" --project SocialLogins
            //
            //  authentik (OpenID Connect)
            // ‣ dotnet user-secrets set authentik:authority "https://<authentik-host>/application/o/<app-slug>/" --project SocialLogins
            // ‣ dotnet user-secrets set authentik:clientId "<from authentik>" --project SocialLogins
            // ‣ dotnet user-secrets set authentik:clientSecret "<from authentik>" --project SocialLogins

            // Step 1. Add Authentication Mechanisms
            var authentication = services
                .AddAuthentication(o =>
                {
                    o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(o =>
                {
                    // set the path for the authentication challenge
                    o.LoginPath = "/signin";
                    // set the path for the sign out
                    o.LogoutPath = "/signout";
                });

            var github = Configuration.GetSection("github");
            if (!string.IsNullOrEmpty(github["clientId"]))
            {
                authentication.AddGitHub(o =>
                {
                    o.ClientId = github["clientId"]!;
                    o.ClientSecret = github["clientSecret"]!;
                    o.CallbackPath = "/signin-github";

                    // Grants access to read a user's profile data.
                    // https://docs.github.com/en/developers/apps/building-oauth-apps/scopes-for-oauth-apps
                    o.Scope.Add("read:user");

                    // Optional
                    // if you need an access token to call GitHub Apis
                    o.Events.OnCreatingTicket += context =>
                    {
                        context.Identity?.AddClaim(new Claim(ClaimsExtensions.IdentityProviderClaim, Providers.GitHub));

                        if (context.AccessToken is { })
                        {
                            context.Identity?.AddClaim(new Claim("access_token", context.AccessToken));
                        }

                        return Task.CompletedTask;
                    };
                });
            }

            var authentik = Configuration.GetSection("authentik");
            if (!string.IsNullOrEmpty(authentik["authority"]))
            {
                authentication.AddOpenIdConnect(Providers.Authentik, "authentik", o =>
                {
                    // e.g. https://auth.example.com/application/o/<app-slug>/
                    // discovery document: <authority>.well-known/openid-configuration
                    o.Authority = authentik["authority"];
                    o.ClientId = authentik["clientId"];
                    o.ClientSecret = authentik["clientSecret"];
                    o.RequireHttpsMetadata = authentik.GetValue("requireHttpsMetadata", true);

                    // Authorization Code flow with PKCE
                    o.ResponseType = "code";
                    o.UsePkce = true;

                    // Must match the Redirect URIs registered on the authentik OAuth2/OpenID Provider
                    o.CallbackPath = "/signin-authentik";
                    o.SignedOutCallbackPath = "/signout-callback-authentik";

                    o.Scope.Clear();
                    o.Scope.Add("openid");
                    o.Scope.Add("profile");
                    o.Scope.Add("email");

                    // keep tokens in the auth cookie; the id_token is needed for RP-initiated logout
                    o.SaveTokens = true;
                    o.GetClaimsFromUserInfoEndpoint = true;

                    // keep claim names as authentik sends them (sub, preferred_username, groups, ...)
                    o.MapInboundClaims = false;
                    o.TokenValidationParameters.NameClaimType = "preferred_username";
                    o.TokenValidationParameters.RoleClaimType = "groups";

                    o.Events.OnTokenValidated = context =>
                    {
                        (context.Principal?.Identity as ClaimsIdentity)?
                            .AddClaim(new Claim(ClaimsExtensions.IdentityProviderClaim, Providers.Authentik));
                        return Task.CompletedTask;
                    };
                });
            }

            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // 2. add Authentication and Authorization Middleware
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();

                // 3. Sign out Endpoint
                // go to ./Pages/Signin.cshtml for 4.
                endpoints.MapGet("/signout", async ctx =>
                {
                    var properties = new AuthenticationProperties { RedirectUri = "/" };

                    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme, properties);

                    // authentik: also end the session at the identity provider (RP-initiated logout),
                    // which redirects back to /signout-callback-authentik and then to "/"
                    if (ctx.User.IdentityProvider() == Providers.Authentik)
                    {
                        await ctx.SignOutAsync(Providers.Authentik, properties);
                    }
                });

            });
        }
    }
}
