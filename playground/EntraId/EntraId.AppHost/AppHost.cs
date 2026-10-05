// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

// Both app registrations must already exist in your tenant; README.md shows how to create them. The dashboard prompts for
// any of these values that isn't already in user secrets.
var tenantId = builder.AddParameter("entra-tenant-id")
    .WithDescription("The **Directory (tenant) ID** shown on the Overview page of either app registration.", enableMarkdown: true);
var apiClientId = builder.AddParameter("entra-api-client-id")
    .WithDescription("The **Application (client) ID** of the `weather-api` app registration.", enableMarkdown: true);
var webClientId = builder.AddParameter("entra-web-client-id")
    .WithDescription("The **Application (client) ID** of the `weather-web` app registration.", enableMarkdown: true);
var webClientSecret = builder.AddParameter("entra-web-client-secret", secret: true)
    .WithDescription("The **Value** of a client secret created under **Certificates & secrets** on the `weather-web` app registration.", enableMarkdown: true);

// The API only validates the access tokens that callers send, so it needs no credential of its own.
var entraApi = builder.AddEntraIdApplication("entra-api")
    .AsExistingApplication(tenantId, apiClientId);

// The web front end redeems authorization codes and requests tokens for the API, which Entra ID only allows for a client
// that proves its identity with a credential.
var entraWeb = builder.AddEntraIdApplication("entra-web")
    .AsExistingApplication(tenantId, webClientId)
    .WithClientSecret(webClientSecret);

// WaitFor holds each app back until its Entra ID resource has validated the IDs, so a typo shows up on the Entra ID
// resource instead of as a sign-in failure.
var apiService = builder.AddProject<Projects.EntraId_ApiService>("apiservice")
    .WithReference(entraApi)
    .WaitFor(entraApi);

builder.AddProject<Projects.EntraId_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(entraWeb)
    .WaitFor(entraWeb)
    .WithReference(apiService)
    // The scope the front end requests when it calls the API: the API's Application ID URI followed by the scope name.
    // Both come from the "Expose an API" step in README.md.
    .WithEnvironment("WeatherApi__Scopes__0", ReferenceExpression.Create($"api://{apiClientId}/access_as_user"));

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

builder.Build().Run();
