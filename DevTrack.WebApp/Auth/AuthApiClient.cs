using System.Net;
using DevTrack.Features.Access.Auth;
using DevTrack.WebApp.Services;

namespace DevTrack.WebApp.Auth;

public class AuthApiClient(IHttpClientFactory clientFactory)
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        using var client = clientFactory.CreateClient(ApiClientBase.ClientName);
        using var response = await client.PostAsJsonAsync("api/auth/login", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty login response.");
    }

    public async Task<string[]?> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        using var client = clientFactory.CreateClient(ApiClientBase.ClientName);
        using var response = await client.GetAsync("api/auth/permissions", cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<string[]>(cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty permissions response.");
    }
}
