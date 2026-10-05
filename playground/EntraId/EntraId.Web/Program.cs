// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text;
using EntraId.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// The AppHost sets this to the API's scope, api://<api-client-id>/access_as_user.
var weatherApiScopes = builder.Configuration.GetSection("WeatherApi:Scopes").Get<string[]>() ?? [];

// The AppHost's WithReference(entraWeb) fills in the AzureAd section, including the client secret that Microsoft.Identity.Web
// needs to redeem the authorization code. Requesting the API's scope at sign-in lets the user consent to it up front, and the
// token cache keeps the resulting access and refresh tokens so the app can call the API on the user's behalf.
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(weatherApiScopes)
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization();
builder.Services.AddAntiforgery();

builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
    // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
    client.BaseAddress = new("https+http://apiservice");
});

var app = builder.Build();

// The sign-in handler builds the redirect URI from the request URL, and README.md registers only the HTTPS one.
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", (HttpContext context, IAntiforgery antiforgery) =>
{
    if (context.User.Identity is not { IsAuthenticated: true } identity)
    {
        return Page("Entra ID playground", """
            <p>You're not signed in.</p>
            <p><a href="/signin">Sign in with Microsoft Entra ID</a></p>
            """);
    }

    var antiforgeryTokens = antiforgery.GetAndStoreTokens(context);
    return Page("Entra ID playground", $"""
        <p>Signed in as <strong>{WebUtility.HtmlEncode(identity.Name)}</strong>.</p>
        <p><a href="/weather">Get the weather forecast from the API</a></p>
        <form method="post" action="/signout">
            <input type="hidden" name="{antiforgeryTokens.FormFieldName}" value="{antiforgeryTokens.RequestToken}">
            <button type="submit">Sign out</button>
        </form>
        """);
});

app.MapGet("/signin", () => Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }));

app.MapPost("/signout", async (HttpContext context, IAntiforgery antiforgery) =>
{
    // The form on the home page carries an antiforgery token, so another site can't sign the user out.
    if (!await antiforgery.IsRequestValidAsync(context))
    {
        return Results.BadRequest();
    }

    // Signing out of both schemes deletes the app's cookie, then ends the Entra ID session. Entra ID sends the browser back
    // to /signout-callback-oidc, which must be a registered redirect URI, and the handler then redirects to "/".
    return Results.SignOut(
        new AuthenticationProperties { RedirectUri = "/" },
        [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
});

app.MapGet("/weather", async (WeatherApiClient weatherApi, CancellationToken cancellationToken) =>
{
    WeatherForecast[] forecasts;
    try
    {
        forecasts = await weatherApi.GetWeatherAsync(weatherApiScopes, cancellationToken);
    }
    catch (MicrosoftIdentityWebChallengeUserException)
    {
        // The sign-in cookie survives an app restart but the in-memory token cache doesn't, so a signed-in user can have no
        // token for the API. Signing in again, which Entra ID usually completes without prompting, refills the cache.
        return Results.Challenge(new AuthenticationProperties { RedirectUri = "/weather" });
    }

    var rows = string.Concat(forecasts.Select(forecast => $"""
        <tr><td>{forecast.Date:d}</td><td>{forecast.TemperatureC}</td><td>{forecast.TemperatureF}</td><td>{WebUtility.HtmlEncode(forecast.Summary)}</td></tr>
        """));
    return Page("Weather forecast", $"""
        <table>
            <thead><tr><th>Date</th><th>Temp. (C)</th><th>Temp. (F)</th><th>Summary</th></tr></thead>
            <tbody>{rows}</tbody>
        </table>
        <p><a href="/">Home</a></p>
        """);
})
.RequireAuthorization();

app.MapDefaultEndpoints();

app.Run();

static IResult Page(string title, string body) => Results.Content($"""
    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="utf-8">
        <title>{title}</title>
    </head>
    <body>
        <h1>{title}</h1>
        {body}
    </body>
    </html>
    """, "text/html", Encoding.UTF8);
