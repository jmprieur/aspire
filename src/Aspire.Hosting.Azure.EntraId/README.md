# Microsoft Entra ID hosting integration

Use this integration to model, configure, and orchestrate Microsoft Entra ID application registrations in an Aspire solution.

## Getting started

### Prerequisites

- A Microsoft Entra ID tenant
- An app registration created in the [Microsoft Entra admin center](https://entra.microsoft.com)

### Add the integration

From your AppHost directory, add the `Aspire.Hosting.Azure.EntraId` integration with the Aspire CLI:

```bash
aspire add Aspire.Hosting.Azure.EntraId
```

## Usage example

Then, in the AppHost, add an Entra ID application resource and reference it from another resource:

```csharp
var tenantId = builder.AddParameter("EntraTenantId");
var apiClientId = builder.AddParameter("EntraApiClientId");

var entraApi = builder.AddEntraIdApplication("entra-api")
                      .AsExistingApplication(tenantId: tenantId, clientId: apiClientId);

var api = builder.AddProject<Projects.Api>("api")
                 .WithReference(entraApi)
                 .WaitFor(entraApi);
```

Pass the **Directory (tenant) ID** and **Application (client) ID** shown on the app registration's **Overview** page. When the parameters have no value, the dashboard prompts for them.

`WithReference` injects environment variables in the form `AzureAd__{Key}` — for example `AzureAd__Instance`, `AzureAd__TenantId`, and `AzureAd__ClientId`. .NET's configuration system maps these to the `AzureAd` configuration section, so the referencing resource reads them as ordinary configuration with no glue code.

When the AppHost starts, the Entra ID resource validates its IDs, instance, and credentials, then reports **Running** and shows links to the app registration and its OpenID Connect discovery document in the dashboard. If a setting is invalid, the resource fails to start and its console logs explain why. `WaitFor` keeps the referencing resource from starting with invalid settings.

To inject into a different configuration section, pass the section name when adding the resource:

```csharp
var entraApi = builder.AddEntraIdApplication("entra-api", "AzureAdApi")
                      .AsExistingApplication(tenantId: tenantId, clientId: apiClientId);
```

## Sign-in audiences

By default, only accounts in the app's home tenant can sign in. When the app registration's **Supported account types** setting allows other accounts, call `WithSignInAudience` with the matching value:

```csharp
var entraApi = builder.AddEntraIdApplication("entra-api")
                      .AsExistingApplication(tenantId: tenantId, clientId: apiClientId)
                      .WithSignInAudience(EntraIdSignInAudience.AzureADMultipleOrgs);
```

| Sign-in audience | Who can sign in | `AzureAd__TenantId` |
|------------------|-----------------|---------------------|
| `AzureADMyOrg` (default) | Accounts in the home tenant | The home tenant ID |
| `AzureADMultipleOrgs` | Accounts in any Microsoft Entra tenant | `organizations` |
| `AzureADandPersonalMicrosoftAccount` | Accounts in any Microsoft Entra tenant and personal Microsoft accounts | `common` |
| `PersonalMicrosoftAccount` | Personal Microsoft accounts only | `consumers` |

For the audiences other than `AzureADMyOrg`, the home tenant ID is injected as `AzureAd__AppHomeTenantId`, which Microsoft.Identity.Web needs when the app acquires tokens as itself. Always pass the home tenant ID to `AsExistingApplication`, not `organizations`, `common`, or `consumers`.

## Client credentials

An application that acquires tokens for itself needs a client credential. Each `With*` call appends an entry to the injected `AzureAd__ClientCredentials__{index}__*` variables, and multiple credentials can be configured as a fallback chain.

### Client secret

```csharp
var webSecret = builder.AddParameter("EntraWebClientSecret", secret: true);

var entraWeb = builder.AddEntraIdApplication("entra-web")
                      .AsExistingApplication(tenantId: tenantId, clientId: webClientId)
                      .WithClientSecret(webSecret);
```

The parameter must be created with `secret: true`.

### Federated identity credential with managed identity

For deployed applications, use a federated identity credential so that no secret is stored:

```csharp
var entraWeb = builder.AddEntraIdApplication("entra-web")
                      .AsExistingApplication(tenantId: tenantId, clientId: webClientId)
                      .WithFicMsi();
```

Pass a client ID to use a user-assigned managed identity instead of the system-assigned one.

### Certificate from Azure Key Vault

```csharp
var entraWeb = builder.AddEntraIdApplication("entra-web")
                      .AsExistingApplication(tenantId: tenantId, clientId: webClientId)
                      .WithCertificateFromKeyVault("https://myvault.vault.azure.net", "MyCert");
```

### Certificate from the certificate store

```csharp
var entraWeb = builder.AddEntraIdApplication("entra-web")
                      .AsExistingApplication(tenantId: tenantId, clientId: webClientId)
                      .WithCertificateThumbprint("CurrentUser/My", "ABC123...");
```

Use `WithCertificateDistinguishedName` to locate the certificate by subject instead of thumbprint.

### Advanced credentials

For credential types without a dedicated method, use `WithCredential`:

```csharp
var entraWeb = builder.AddEntraIdApplication("entra-web")
                      .AsExistingApplication(tenantId: tenantId, clientId: webClientId)
                      .WithCredential(new EntraIdSignedAssertionFileCredential());
```

## Sovereign clouds

To target a sovereign cloud instance such as Azure Government:

```csharp
var entraApi = builder.AddEntraIdApplication("entra-api")
                      .WithInstance("https://login.microsoftonline.us/")
                      .AsExistingApplication(tenantId: tenantId, clientId: apiClientId);
```

## Current limitations

The integration references app registrations that already exist. It doesn't yet:

* Create or update app registrations.
* Add the referencing resources' URLs as redirect URIs.
* Expose API scopes or grant API permissions.
* Create, store, or rotate client secrets and certificates.

Create and configure the app registrations in the [Microsoft Entra admin center](https://entra.microsoft.com), then pass their IDs to `AsExistingApplication`.

## Additional documentation

* https://aspire.dev/integrations/gallery/
* https://learn.microsoft.com/entra/identity-platform/
* https://learn.microsoft.com/entra/msal/dotnet/microsoft-identity-web/
* https://devblogs.microsoft.com/aspire/securing-dotnet-aspire-apps-with-microsoft-entra-id/

## Feedback & contributing

https://github.com/microsoft/aspire
