// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Azure.EntraId.Tests;

public class EntraIdResourceBuilderTests
{
    // Placeholder IDs from the Microsoft Learn documentation. They are well-formed but identify nothing.
    private const string TenantId = "aaaabbbb-0000-cccc-1111-dddd2222eeee";
    private const string ClientId = "00001111-aaaa-2222-bbbb-3333cccc4444";

    [Fact]
    public void AddEntraIdApplication_CreatesResource()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Equal("entra-api", resource.Name);
        Assert.Equal(TenantId, resource.TenantId);
        Assert.Equal(ClientId, resource.ClientId);
    }

    [Fact]
    public void AddEntraIdApplication_DefaultConfigSection()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Equal("AzureAd", resource.ConfigSectionName);
    }

    [Fact]
    public void AddEntraIdApplication_CustomConfigSection()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-api", "AzureAdApi")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Equal("AzureAdApi", resource.ConfigSectionName);
    }

    [Fact]
    public void AddEntraIdApplication_AsExistingApplicationWithTenantIdParameter()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var tenantId = appBuilder.AddParameter("EntraTenantId");
        var clientId = appBuilder.AddParameter("EntraApiClientId");

        appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: tenantId, clientId: clientId);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.NotNull(resource.TenantIdParameter);
        Assert.Equal("EntraTenantId", resource.TenantIdParameter.Name);
    }

    [Fact]
    public void AddEntraIdApplication_AsExistingApplicationWithClientIdParameter()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var tenantId = appBuilder.AddParameter("EntraTenantId");
        var clientId = appBuilder.AddParameter("EntraApiClientId");

        appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: tenantId, clientId: clientId);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.NotNull(resource.ClientIdParameter);
        Assert.Equal("EntraApiClientId", resource.ClientIdParameter.Name);
    }

    [Fact]
    public void AsExistingApplication_AcceptsTenantDomainName()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var entra = appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: "contoso.onmicrosoft.com", clientId: ClientId);

        Assert.Equal("contoso.onmicrosoft.com", entra.Resource.TenantId);
    }

    [Theory]
    [InlineData("organizations", "AzureADMultipleOrgs")]
    [InlineData("Common", "AzureADandPersonalMicrosoftAccount")]
    [InlineData("CONSUMERS", "PersonalMicrosoftAccount")]
    public void AsExistingApplication_ThrowsWhenTenantIdIsSignInKeyword(string tenantId, string expectedSignInAudience)
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var entra = appBuilder.AddEntraIdApplication("entra-api");

        var exception = Assert.Throws<ArgumentException>(() => entra.AsExistingApplication(tenantId: tenantId, clientId: ClientId));
        Assert.Equal("tenantId", exception.ParamName);
        Assert.Equal(
            $"'{tenantId}' is not a valid tenant ID. The keywords 'organizations', 'common' and 'consumers' choose who can sign in; " +
            "they do not identify a tenant. Use the ID of the tenant where the app is registered, and call " +
            $"WithSignInAudience(EntraIdSignInAudience.{expectedSignInAudience}) instead. (Parameter 'tenantId')",
            exception.Message);
    }

    [Fact]
    public void AsExistingApplication_ThrowsWhenTenantIdIsMalformed()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var entra = appBuilder.AddEntraIdApplication("entra-api");

        var exception = Assert.Throws<ArgumentException>(() => entra.AsExistingApplication(tenantId: "my-tenant", clientId: ClientId));
        Assert.Equal("tenantId", exception.ParamName);
        Assert.Equal(
            "'my-tenant' is not a valid tenant ID. Expected the directory (tenant) ID shown on the app registration's Overview page, " +
            "such as 'aaaabbbb-0000-cccc-1111-dddd2222eeee', or a domain name such as 'contoso.onmicrosoft.com'. (Parameter 'tenantId')",
            exception.Message);
    }

    [Fact]
    public void AsExistingApplication_ThrowsWhenClientIdIsMalformed()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var entra = appBuilder.AddEntraIdApplication("entra-api");

        var exception = Assert.Throws<ArgumentException>(() => entra.AsExistingApplication(tenantId: TenantId, clientId: "my-client"));
        Assert.Equal("clientId", exception.ParamName);
        Assert.Equal(
            "'my-client' is not a valid client ID. Expected the application (client) ID shown on the app registration's Overview page, " +
            "such as '00001111-aaaa-2222-bbbb-3333cccc4444'. (Parameter 'clientId')",
            exception.Message);
    }

    [Fact]
    public void AsExistingApplication_ThrowsWhenIdsAreMissing()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var entra = appBuilder.AddEntraIdApplication("entra-api");

        Assert.Throws<ArgumentNullException>(() => entra.AsExistingApplication(tenantId: (string)null!, clientId: ClientId));
        Assert.Throws<ArgumentException>(() => entra.AsExistingApplication(tenantId: string.Empty, clientId: ClientId));
        Assert.Throws<ArgumentNullException>(() => entra.AsExistingApplication(tenantId: TenantId, clientId: (string)null!));
        Assert.Throws<ArgumentException>(() => entra.AsExistingApplication(tenantId: TenantId, clientId: string.Empty));
    }

    [Fact]
    public void AddEntraIdApplication_WithClientSecret()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var secret = appBuilder.AddParameter("EntraWebClientSecret", secret: true);

        appBuilder.AddEntraIdApplication("entra-web")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithClientSecret(secret);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Single(resource.ClientCredentials);
        var cred = Assert.IsType<EntraIdClientSecretCredential>(resource.ClientCredentials[0]);
        Assert.Equal("ClientSecret", cred.SourceType);
        Assert.Equal("EntraWebClientSecret", cred.ClientSecret.Name);
    }

    [Fact]
    public void AddEntraIdApplication_DefaultInstance()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Equal("https://login.microsoftonline.com/", resource.Instance);
    }

    [Theory]
    [InlineData("https://login.microsoftonline.us/")]
    [InlineData("https://contoso.ciamlogin.com/")]
    public void AddEntraIdApplication_WithCustomInstance(string instance)
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-api")
            .WithInstance(instance)
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Equal(instance, resource.Instance);
    }

    [Theory]
    [InlineData("login.microsoftonline.com")]
    [InlineData("http://login.microsoftonline.com/")]
    [InlineData("https://login.microsoftonline.com/?slice=testslice")]
    [InlineData("https://login.microsoftonline.com/#tenant")]
    public void WithInstance_ThrowsWhenInstanceIsNotHttpsUrl(string instance)
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var entra = appBuilder.AddEntraIdApplication("entra-api");

        var exception = Assert.Throws<ArgumentException>(() => entra.WithInstance(instance));
        Assert.Equal("instance", exception.ParamName);
        Assert.Equal(
            $"'{instance}' is not a valid Entra ID instance. Expected an absolute HTTPS URL with no query string or fragment, " +
            "such as 'https://login.microsoftonline.com/'. (Parameter 'instance')",
            exception.Message);
    }

    [Fact]
    public void AddEntraIdApplication_DefaultSignInAudienceIsHomeTenantOnly()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var entra = appBuilder.AddEntraIdApplication("entra-api");

        Assert.Equal(EntraIdSignInAudience.AzureADMyOrg, entra.Resource.SignInAudience);
    }

    [Fact]
    public void WithSignInAudience_ThrowsWhenValueIsNotDefined()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var entra = appBuilder.AddEntraIdApplication("entra-api");

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => entra.WithSignInAudience((EntraIdSignInAudience)42));
        Assert.Equal("signInAudience", exception.ParamName);
    }

    [Fact]
    public void AddEntraIdApplication_IsExcludedFromManifest()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var entra = appBuilder.AddEntraIdApplication("entra-api");

        Assert.True(entra.Resource.TryGetAnnotationsOfType<ManifestPublishingCallbackAnnotation>(out var annotations));
        Assert.Equal(ManifestPublishingCallbackAnnotation.Ignore, Assert.Single(annotations));
    }

    [Fact]
    public void AddEntraIdApplication_StartsInWaitingState()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var entra = appBuilder.AddEntraIdApplication("entra-api");

        var annotation = Assert.Single(entra.Resource.Annotations.OfType<ResourceSnapshotAnnotation>());
        Assert.Equal("EntraIdApplication", annotation.InitialSnapshot.ResourceType);
        Assert.Equal(KnownResourceStates.Waiting, annotation.InitialSnapshot.State?.Text);
        Assert.Empty(annotation.InitialSnapshot.Properties);
    }

    [Fact]
    public void AddEntraIdApplication_WithClientCapability()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithClientCapability("cp1");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Single(resource.ClientCapabilities);
        Assert.Contains("cp1", resource.ClientCapabilities);
    }

    [Fact]
    public void AddEntraIdApplication_WithAzureRegion()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithAzureRegion("TryAutoDetect");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Equal("TryAutoDetect", resource.AzureRegion);
    }

    [Fact]
    public void AddEntraIdApplication_WithACLAuthorization()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithAllowWebApiToBeAuthorizedByACL();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.True(resource.AllowWebApiToBeAuthorizedByACL);
    }

    [Fact]
    public void AddEntraIdApplication_WithExtraQueryParameter()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithExtraQueryParameter("dc", "prod-wst-01")
            .WithExtraQueryParameter("slice", "testslice");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Equal(2, resource.ExtraQueryParameters.Count);
        Assert.Equal("prod-wst-01", resource.ExtraQueryParameters["dc"]);
        Assert.Equal("testslice", resource.ExtraQueryParameters["slice"]);
    }

    [Fact]
    public void AddEntraIdApplication_WithAudiences()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithAudience($"api://{ClientId}");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Single(resource.Audiences);
        Assert.Contains($"api://{ClientId}", resource.Audiences);
    }

    [Fact]
    public void AddEntraIdApplication_WithFicMsi()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-web")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithFicMsi("mi-client-id");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Single(resource.ClientCredentials);
        var cred = Assert.IsType<EntraIdFederatedIdentityCredential>(resource.ClientCredentials[0]);
        Assert.Equal("SignedAssertionFromManagedIdentity", cred.SourceType);
        Assert.Equal("mi-client-id", cred.ManagedIdentityClientId);
    }

    [Fact]
    public void AddEntraIdApplication_WithFicMsi_SystemAssigned()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-web")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithFicMsi();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Single(resource.ClientCredentials);
        var cred = Assert.IsType<EntraIdFederatedIdentityCredential>(resource.ClientCredentials[0]);
        Assert.Equal("SignedAssertionFromManagedIdentity", cred.SourceType);
        Assert.Null(cred.ManagedIdentityClientId);
    }

    [Fact]
    public void AddEntraIdApplication_WithManagedCertificate()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-web")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithManagedCertificate();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Single(resource.ClientCredentials);
        var cred = Assert.IsType<EntraIdManagedCertificateCredential>(resource.ClientCredentials[0]);
        Assert.Equal("ManagedCertificate", cred.SourceType);
    }

    [Fact]
    public void AddEntraIdApplication_MultipleCredentials()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var secret = appBuilder.AddParameter("EntraSecret", secret: true);

        appBuilder.AddEntraIdApplication("entra-web")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithClientSecret(secret)
            .WithFicMsi("mi-client-id");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Equal(2, resource.ClientCredentials.Count);
        Assert.IsType<EntraIdClientSecretCredential>(resource.ClientCredentials[0]);
        Assert.IsType<EntraIdFederatedIdentityCredential>(resource.ClientCredentials[1]);
    }

    [Fact]
    public void AddEntraIdApplication_WithCertificateFromKeyVault()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-web")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithCertificateFromKeyVault("https://myvault.vault.azure.net", "MyCert");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Single(resource.ClientCredentials);
        var cred = Assert.IsType<EntraIdKeyVaultCertificateCredential>(resource.ClientCredentials[0]);
        Assert.Equal("KeyVault", cred.SourceType);
        Assert.Equal("https://myvault.vault.azure.net", cred.KeyVaultUrl);
        Assert.Equal("MyCert", cred.CertificateNameInKeyVault);
    }

    [Fact]
    public async Task WithReference_InjectsEnvironmentVariables()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var secret = appBuilder.AddParameter("EntraSecret", "super-secret", secret: true);

        var entra = appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithClientSecret(secret)
            .WithAudience($"api://{ClientId}")
            .WithClientCapability("cp1")
            .WithAzureRegion("westus2")
            .WithAllowWebApiToBeAuthorizedByACL()
            .WithExtraQueryParameter("dc", "prod-wst-01");

        var container = appBuilder.AddContainer("api", "myimage")
            .WithReference(entra);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            container.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal(new Dictionary<string, string>
        {
            ["AzureAd__Instance"] = "https://login.microsoftonline.com/",
            ["AzureAd__TenantId"] = TenantId,
            ["AzureAd__ClientId"] = ClientId,
            ["AzureAd__AzureRegion"] = "westus2",
            ["AzureAd__ClientCredentials__0__SourceType"] = "ClientSecret",
            ["AzureAd__ClientCredentials__0__ClientSecret"] = "super-secret",
            ["AzureAd__ClientCapabilities__0"] = "cp1",
            ["AzureAd__Audiences__0"] = $"api://{ClientId}",
            ["AzureAd__AllowWebApiToBeAuthorizedByACL"] = "true",
            ["AzureAd__ExtraQueryParameters__dc"] = "prod-wst-01"
        }, env);
    }

    [Fact]
    public async Task WithReference_DefaultSignInAudience_EmitsHomeTenantAsTenantId()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var entra = appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId);

        var container = appBuilder.AddContainer("api", "myimage")
            .WithReference(entra);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            container.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal(new Dictionary<string, string>
        {
            ["AzureAd__Instance"] = "https://login.microsoftonline.com/",
            ["AzureAd__TenantId"] = TenantId,
            ["AzureAd__ClientId"] = ClientId
        }, env);
    }

    [Theory]
    [InlineData(EntraIdSignInAudience.AzureADMultipleOrgs, "organizations")]
    [InlineData(EntraIdSignInAudience.AzureADandPersonalMicrosoftAccount, "common")]
    [InlineData(EntraIdSignInAudience.PersonalMicrosoftAccount, "consumers")]
    public async Task WithReference_OtherSignInAudiences_EmitKeywordAndHomeTenant(EntraIdSignInAudience signInAudience, string expectedTenantId)
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var entra = appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithSignInAudience(signInAudience);

        var container = appBuilder.AddContainer("api", "myimage")
            .WithReference(entra);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            container.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal(signInAudience, entra.Resource.SignInAudience);
        Assert.Equal(new Dictionary<string, string>
        {
            ["AzureAd__Instance"] = "https://login.microsoftonline.com/",
            ["AzureAd__TenantId"] = expectedTenantId,
            ["AzureAd__AppHomeTenantId"] = TenantId,
            ["AzureAd__ClientId"] = ClientId
        }, env);
    }

    [Fact]
    public async Task WithReference_OtherSignInAudiences_EmitHomeTenantFromParameter()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var tenantId = appBuilder.AddParameter("EntraTenantId", TenantId);
        var clientId = appBuilder.AddParameter("EntraApiClientId", ClientId);

        var entra = appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: tenantId, clientId: clientId)
            .WithSignInAudience(EntraIdSignInAudience.AzureADMultipleOrgs);

        var container = appBuilder.AddContainer("api", "myimage")
            .WithReference(entra);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            container.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal(new Dictionary<string, string>
        {
            ["AzureAd__Instance"] = "https://login.microsoftonline.com/",
            ["AzureAd__TenantId"] = "organizations",
            ["AzureAd__AppHomeTenantId"] = TenantId,
            ["AzureAd__ClientId"] = ClientId
        }, env);
    }

    [Fact]
    public async Task WithSendX5C_EmitsSendX5C()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var entra = appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithSendX5C();

        var container = appBuilder.AddContainer("api", "myimage")
            .WithReference(entra);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            container.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal("true", env["AzureAd__SendX5C"]);
    }

    [Fact]
    public async Task WithCredential_FileCertificatePasswordFlowsThroughParameter()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var password = appBuilder.AddParameter("CertPassword", "p@ssw0rd", secret: true);

        var entra = appBuilder.AddEntraIdApplication("entra-web")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithCredential(new EntraIdFileCertificateCredential
            {
                FilePath = "/certs/app.pfx",
                Password = password.Resource
            });

        var container = appBuilder.AddContainer("web", "myimage")
            .WithReference(entra);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            container.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal("Path", env["AzureAd__ClientCredentials__0__SourceType"]);
        Assert.Equal("/certs/app.pfx", env["AzureAd__ClientCredentials__0__CertificateDiskPath"]);
        Assert.Equal("p@ssw0rd", env["AzureAd__ClientCredentials__0__CertificatePassword"]);
    }

    [Fact]
    public async Task WithReference_UsesCustomConfigSectionNameAsPrefix()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var entra = appBuilder.AddEntraIdApplication("entra-api", "AzureAdApi")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId);

        var container = appBuilder.AddContainer("api", "myimage")
            .WithReference(entra);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            container.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal(new Dictionary<string, string>
        {
            ["AzureAdApi__Instance"] = "https://login.microsoftonline.com/",
            ["AzureAdApi__TenantId"] = TenantId,
            ["AzureAdApi__ClientId"] = ClientId
        }, env);
    }

    [Fact]
    public async Task WithReference_EmitsCertificateStoreCredential()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var entra = appBuilder.AddEntraIdApplication("entra-web")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithCertificateThumbprint("CurrentUser/My", "ABC123");

        var container = appBuilder.AddContainer("web", "myimage")
            .WithReference(entra);

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            container.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal(new Dictionary<string, string>
        {
            ["AzureAd__Instance"] = "https://login.microsoftonline.com/",
            ["AzureAd__TenantId"] = TenantId,
            ["AzureAd__ClientId"] = ClientId,
            ["AzureAd__ClientCredentials__0__SourceType"] = "StoreWithThumbprint",
            ["AzureAd__ClientCredentials__0__CertificateStorePath"] = "CurrentUser/My",
            ["AzureAd__ClientCredentials__0__CertificateThumbprint"] = "ABC123"
        }, env);
    }

    [Fact]
    public void AddEntraIdApplication_WithCertificateThumbprint()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-web")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithCertificateThumbprint("CurrentUser/My", "ABC123");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Single(resource.ClientCredentials);
        var cred = Assert.IsType<EntraIdStoreCertificateCredential>(resource.ClientCredentials[0]);
        Assert.Equal("StoreWithThumbprint", cred.SourceType);
        Assert.Equal("CurrentUser/My", cred.StorePath);
        Assert.Equal("ABC123", cred.Thumbprint);
        Assert.Null(cred.DistinguishedName);
    }

    [Fact]
    public void AddEntraIdApplication_WithCertificateDistinguishedName()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-web")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithCertificateDistinguishedName("CurrentUser/My", "CN=MyCert");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Single(resource.ClientCredentials);
        var cred = Assert.IsType<EntraIdStoreCertificateCredential>(resource.ClientCredentials[0]);
        Assert.Equal("StoreWithDistinguishedName", cred.SourceType);
        Assert.Equal("CurrentUser/My", cred.StorePath);
        Assert.Equal("CN=MyCert", cred.DistinguishedName);
    }

    [Fact]
    public void AddEntraIdApplication_WithRawCredential()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-web")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId)
            .WithCredential(new EntraIdSignedAssertionFileCredential
            {
                FilePath = "/var/run/secrets/token"
            });

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.Single(resource.ClientCredentials);
        var cred = Assert.IsType<EntraIdSignedAssertionFileCredential>(resource.ClientCredentials[0]);
        Assert.Equal("SignedAssertionFilePath", cred.SourceType);
        Assert.Equal("/var/run/secrets/token", cred.FilePath);
    }

    [Fact]
    public void WithReference_CreatesReferenceRelationship()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var entra = appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId);

        var project = appBuilder.AddContainer("api", "myimage")
            .WithReference(entra);

        var relationships = project.Resource.Annotations
            .OfType<ResourceRelationshipAnnotation>()
            .ToList();

        Assert.Contains(relationships, r => r.Resource == entra.Resource);
    }

    [Fact]
    public void AddEntraIdApplication_ThrowsWhenNameNull()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        Assert.Throws<ArgumentNullException>(() =>
            appBuilder.AddEntraIdApplication(null!));
    }

    [Fact]
    public void AddEntraIdApplication_ThrowsWhenNameEmpty()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        Assert.Throws<ArgumentException>(() =>
            appBuilder.AddEntraIdApplication(string.Empty));
    }

    [Fact]
    public void EntraIdStoreCertificateCredential_ThrowsWhenNeitherThumbprintNorDN()
    {
        var credential = new EntraIdStoreCertificateCredential
        {
            StorePath = "CurrentUser/My"
        };

        Assert.Throws<InvalidOperationException>(() => _ = credential.SourceType);
    }

    [Fact]
    public void EntraIdStoreCertificateCredential_ThrowsWhenBothThumbprintAndDN()
    {
        var credential = new EntraIdStoreCertificateCredential
        {
            StorePath = "CurrentUser/My",
            Thumbprint = "ABC123",
            DistinguishedName = "CN=MyCert"
        };

        Assert.Throws<InvalidOperationException>(() => _ = credential.SourceType);
    }

    [Fact]
    public void WithClientSecret_ThrowsWhenParameterIsNotSecret()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var notSecret = appBuilder.AddParameter("EntraWebClientSecret");

        var entra = appBuilder.AddEntraIdApplication("entra-web")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId);

        Assert.Throws<ArgumentException>(() => entra.WithClientSecret(notSecret));
    }

    [Fact]
    public void EntraIdApplicationResource_ThrowsWhenConfigSectionNameIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new EntraIdApplicationResource("entra-api", string.Empty));
        Assert.Throws<ArgumentNullException>(() => new EntraIdApplicationResource("entra-api", null!));
    }

    [Fact]
    public void AddEntraIdApplication_ThrowsWhenConfigSectionNameIsEmpty()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        Assert.Throws<ArgumentException>(() =>
            appBuilder.AddEntraIdApplication("entra-api", string.Empty));
    }

    [Fact]
    public void AddEntraIdApplication_DoesNotImplementIResourceWithConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddEntraIdApplication("entra-api")
            .AsExistingApplication(tenantId: TenantId, clientId: ClientId);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<EntraIdApplicationResource>());
        Assert.IsNotAssignableFrom<IResourceWithConnectionString>(resource);
    }
}
