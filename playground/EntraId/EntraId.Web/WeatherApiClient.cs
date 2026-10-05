// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;
using Microsoft.Identity.Abstractions;

namespace EntraId.Web;

public sealed class WeatherApiClient(HttpClient httpClient, IAuthorizationHeaderProvider authorizationHeaderProvider)
{
    public async Task<WeatherForecast[]> GetWeatherAsync(IEnumerable<string> scopes, CancellationToken cancellationToken)
    {
        // Returns a cached access token for the signed-in user, or redeems the cached refresh token for a new one. The result
        // is the whole header value, such as "Bearer eyJ0eXAi...".
        var authorizationHeader = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(scopes, cancellationToken: cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/weatherforecast");
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorizationHeader);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<WeatherForecast[]>(cancellationToken) ?? [];
    }
}

public sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
