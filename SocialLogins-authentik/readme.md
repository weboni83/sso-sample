# Social Logins With ASP.NET Core

This sample shows how to integrate with a third-party OpenID connect provider. In this example, we'll use GitHub as our auth provider, using the [ASP.NET Contrib libraries.](https://www.nuget.org/profiles/aspnet-contrib). There are other providers such as LinkedIn, OpenId (vanilla), Apple, Salesforce, and so many more.

As a **_bonus_** we'll use OctoKit to call the GitHub APIs on behalf of the user. This is likely an operation for apps that use an exclusive identity provider.

This copy also adds [authentik](https://goauthentik.io) as a second login provider using the standard OpenID Connect handler (`Microsoft.AspNetCore.Authentication.OpenIdConnect`). GitHub and authentik can be used side by side; each provider is only registered when its settings are present.

## Getting Started

Before running the application, you'll need to add secrets to your .NET User Secret Store or by adding the secrets directly to any of the `appSettings.json` files in the project folder. 

**I recommend using the secret store as there is no chance of leaking your credentials by accident.**

```console
‣ dotnet user-secrets set github:clientId "<from github>" --project SocialLogins
‣ dotnet user-secrets set github:clientSecret "<from github>" --project SocialLogins
‣ dotnet user-secrets list
   github:clientSecret = <secret>
   github:clientId = <secret>            
```

To get these values, you can create [a new OAuth App under your GitHub account](https://github.com/settings/developers).

You'll also need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) installed. The sample targets `net10.0` and uses `AspNet.Security.OAuth.GitHub` 10.x and `Octokit` 14.x.

> **Note:** User secrets are only loaded in the `Development` environment (the default for `dotnet run` via `launchSettings.json`). In any other environment, supply the settings through environment variables (e.g. `github__clientId`, `authentik__authority`) or another configuration source. A provider without settings is simply not shown on the sign-in page.

```
‣ dotnet restore
‣ dotnet run --project SocialLogins
```

## authentik Setup

### 1. Create the provider and application in authentik

In the authentik admin interface, go to **Applications → Applications → Create with Provider** (or create the provider and application separately):

| Setting | Value |
|---|---|
| Provider type | **OAuth2/OpenID Provider** |
| Client type | **Confidential** |
| Redirect URIs | `https://localhost:5001/signin-authentik` (Strict) |
| Post-logout redirect URI | `https://localhost:5001/signout-callback-authentik` |
| Signing Key | select a certificate (e.g. *authentik Self-signed Certificate*) so tokens are signed with RS256 |
| Scopes | `openid`, `profile`, `email` (default mappings) |

Note the **application slug**, **Client ID** and **Client Secret**.

### 2. Configure the app

The authority is the provider's issuer URL, `https://<authentik-host>/application/o/<app-slug>/` (the trailing slash matters; the discovery document lives at `<authority>.well-known/openid-configuration`).

```console
‣ dotnet user-secrets set authentik:authority "https://<authentik-host>/application/o/<app-slug>/" --project SocialLogins
‣ dotnet user-secrets set authentik:clientId "<from authentik>" --project SocialLogins
‣ dotnet user-secrets set authentik:clientSecret "<from authentik>" --project SocialLogins
```

If authentik runs locally over plain HTTP (development only), also set:

```console
‣ dotnet user-secrets set authentik:requireHttpsMetadata false --project SocialLogins
```

### How it works

- Authorization Code flow with PKCE. On .NET 9+ the handler automatically uses Pushed Authorization Requests (PAR) when the server advertises it.
- Claims are kept as authentik sends them (`MapInboundClaims = false`): the user name comes from `preferred_username`, roles from `groups`.
- An `idp` claim (`GitHub` or `authentik`) records which provider the user signed in with. The GitHub API call on the Account page only runs for GitHub users.
- `/signout` clears the local cookie and, for authentik users, also ends the authentik session (RP-initiated logout) before returning to `/`.

## GitHub Scopes

When working with OpenID connect providers, the client must request what scopes its asking the authentication server. In the case of GitHub, we could have the ability to read a user's information, update their bio, change repos, and much more. To limit what our app can do, we need to specify the necessary scopes.

See the [GitHub scopes documentation](https://docs.github.com/en/developers/apps/building-oauth-apps/scopes-for-oauth-apps) for more information.

## Notable Code Blocks

Here are some of the more notable code blocks you may want to investigate when looking through this sample.

- [Startup](./SocialLogins/Startup.cs#L41-L124): Registering authentication handlers
- [GitHub](./SocialLogins/Startup.cs#L54-L81): GitHub OAuth handler, saving the access token to claims
- [authentik](./SocialLogins/Startup.cs#L83-L124): authentik OpenID Connect handler
- [Signin](./SocialLogins/Pages/Signin.cshtml): Page with login buttons
- [Startup - Sign Out](./SocialLogins/Startup.cs#L158-L170): Handle Sign out process (including authentik logout)
- [Account Page](./SocialLogins/Pages/Account.cshtml): Account information from Claims and GitHub API call.

## License

The MIT License (MIT)
Copyright © 2021 Khalid Abuhakmeh

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE