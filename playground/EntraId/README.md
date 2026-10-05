# Microsoft Entra ID playground

This playground uses the `Aspire.Hosting.Azure.EntraId` integration to protect a web API and a web front end with Microsoft Entra ID. You sign in to the front end, `webfrontend`, which then calls the API, `apiservice`, on your behalf with an access token for the API's `access_as_user` scope.

The integration doesn't create app registrations yet, so you create both registrations in your own tenant and give the AppHost their IDs. You need a tenant where you can create app registrations, such as the default directory of an Azure subscription.

## Register the API

1. In the [Microsoft Entra admin center](https://entra.microsoft.com), open **App registrations** and select **New registration**.
1. Name it `weather-api`, choose **Accounts in this organizational directory only**, leave **Redirect URI** empty, and select **Register**.
1. From the **Overview** page, copy the **Application (client) ID** and the **Directory (tenant) ID**.
1. Open **Expose an API**, select **Add** next to **Application ID URI**, and save the default value, `api://<client-id>`. The AppHost builds the API's scope from this value, so keep the default.
1. Select **Add a scope**, name it `access_as_user`, choose **Admins and users**, fill in the display names and descriptions, and select **Add scope**.

## Register the web front end

1. Create another registration named `weather-web` with **Accounts in this organizational directory only**. Under **Redirect URI**, choose **Web**, enter `https://localhost:7251/signin-oidc`, and select **Register**.
1. Copy the **Application (client) ID**.
1. Open **Authentication** and add `https://localhost:7251/signout-callback-oidc` as a second redirect URI, so that Entra ID sends you back to the app after you sign out.
1. Open **Certificates & secrets**, select **New client secret**, and copy the secret's **Value**. The portal shows it only once, and the **Secret ID** next to it won't work.
1. Open **API permissions**, select **Add a permission** > **My APIs** > `weather-api` > **Delegated permissions**, check `access_as_user`, and select **Add permissions**. If your tenant doesn't let users consent to apps, select **Grant admin consent**, or ask an administrator to.

Entra ID redirects only to URIs that exactly match a registered one, so the web front end has a single `https` launch profile that pins it to `https://localhost:7251`, whichever profile the AppHost runs with. Aspire's proxy listens on that port and forwards to the app.

## Provide the IDs

The AppHost declares a parameter for each value:

| Parameter | Value |
| --- | --- |
| `entra-tenant-id` | Directory (tenant) ID |
| `entra-api-client-id` | Application (client) ID of `weather-api` |
| `entra-web-client-id` | Application (client) ID of `weather-web` |
| `entra-web-client-secret` | Client secret value of `weather-web` |

When you run the AppHost, the dashboard prompts for any value that's missing and can save it to user secrets. To set the values ahead of time instead, run these commands from the `EntraId.AppHost` directory:

```bash
dotnet user-secrets set "Parameters:entra-tenant-id" "<tenant-id>"
dotnet user-secrets set "Parameters:entra-api-client-id" "<api-client-id>"
dotnet user-secrets set "Parameters:entra-web-client-id" "<web-client-id>"
dotnet user-secrets set "Parameters:entra-web-client-secret" "<web-client-secret>"
```

## Run the playground

From this directory, run `aspire run`, or `dotnet run --project EntraId.AppHost`. In the dashboard, open the `webfrontend` URL, sign in, and select the link to get the weather forecast.

## What to look for

- `entra-api` and `entra-web` report **Running** once their IDs are valid. Each one links to its app registration in the Azure portal and to the tenant's OpenID Connect discovery document.
- The environment variables of `apiservice` and `webfrontend` show the `AzureAd__*` settings that `WithReference` adds. The web front end's client secret arrives as `AzureAd__ClientCredentials__0__ClientSecret`.
- If a value is malformed, such as a tenant ID that's neither a GUID nor a domain name, the Entra ID resource reports **FailedToStart** and its console logs say what's wrong. `WaitFor` keeps `apiservice` and `webfrontend` waiting rather than starting with bad settings.

## Troubleshooting

| Error | Likely cause |
| --- | --- |
| AADSTS50011 | The redirect URI doesn't match. Check that `weather-web` has `https://localhost:7251/signin-oidc` and that you opened the front end on port 7251. |
| AADSTS7000215 | The client secret is wrong. Enter the secret's **Value**, not its **Secret ID**. |
| AADSTS65001 or **Need admin approval** | The `access_as_user` permission hasn't been consented to. Grant admin consent under **API permissions** on `weather-web`. |
| AADSTS500011 | `weather-api` doesn't have the Application ID URI `api://<client-id>`. |

The front end caches tokens in memory, so after you restart the AppHost, it signs you in again the first time it calls the API.
