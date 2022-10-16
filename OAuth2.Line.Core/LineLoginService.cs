namespace OAuth2.Line.Core;

using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;

public class LineLoginService
{
    private ILogger<LineLoginService> _logger;
    private readonly HttpClient _httpClient;

    public LineLoginService(ILogger<LineLoginService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("LineLoginService");
    }

    public string GenerateLineLoginUrl(string clientId, string redirectUri, string state)
    {
        var url = $"https://access.line.me/oauth2/v2.1/authorize?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&state={state}&scope=openid%20profile";
        return url;
    }

    public async Task<LineLoginAccessToken> GetAccessToken(string code, string clientId, string clientSecret, string redirectUri)
    {
        var endpoint = "https://api.line.me/oauth2/v2.1/token";
        var response = await _httpClient.PostAsync(endpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUri }
            }));
        response.EnsureSuccessStatusCode();

        var responseStream = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<LineLoginAccessToken>(responseStream);
    }
}