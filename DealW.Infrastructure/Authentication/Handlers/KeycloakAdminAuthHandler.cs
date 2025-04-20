using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace DealW.Infrastructure.Authentication.Handlers;

public class KeycloakAdminAuthHandler(
    IHttpClientFactory httpClientFactory, 
    IOptions<KeycloakOptions> options) : DelegatingHandler
{
    private readonly KeycloakOptions _options = options.Value;
    private string? _cachedToken;
    private DateTime _tokenExpiresAt;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Если токен отсутствует или истек, получаем новый
        if (_cachedToken == null || DateTime.UtcNow >= _tokenExpiresAt)
        {
            using var client = httpClientFactory.CreateClient();
            var response = await client.PostAsync($"{_options.AdminBaseUrl}/realms/{_options.AdminRealm}/protocol/openid-connect/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", _options.AdminClient },
                    { "client_secret", _options.AdminSecret },
                    { "grant_type", "client_credentials" }
                }), cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenObj = JsonSerializer.Deserialize<JsonElement>(json);
            _cachedToken = tokenObj.GetProperty("access_token").GetString();
            var expiresIn = tokenObj.GetProperty("expires_in").GetInt32();
            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Токен истекает за 60 секунд до окончания
        }

        // Добавляем токен в заголовок запроса
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _cachedToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
