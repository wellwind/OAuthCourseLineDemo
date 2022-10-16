namespace OAuth2.Line.Core;

using System.Net.Http;
using System.Net.Http.Headers;
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

    public async Task<LineLoginAccessToken> GetAccessTokenAsync(string code, string clientId, string clientSecret, string redirectUri)
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

        var responseStream = await response.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<LineLoginAccessToken>(responseStream);
    }

    public async Task<LineLoginVerifyAccessTokenResult> VerifyAccessTokenAsync(string accessToken)
    {
        var endpoint = $"https://api.line.me/oauth2/v2.1/verify?access_token={accessToken}";
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var responseStream = await response.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<LineLoginVerifyAccessTokenResult>(responseStream);
    }

    public async Task<LineLoginVerifyIdTokenResult> VerifyIdTokenAsync(string idToken, string cliendId)
    {
        var endpoint = "https://api.line.me/oauth2/v2.1/verify";
        var response = await _httpClient.PostAsync(endpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "id_token", idToken },
                { "client_id", cliendId }
            }));
        response.EnsureSuccessStatusCode();

        var responseStream = await response.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<LineLoginVerifyIdTokenResult>(responseStream);
    }

    public async Task<LineLoginUserProfile> GetUserProfileAsync(string accessToken)
    {
        var endpoint = "https://api.line.me/v2/profile";
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseStream = await response.Content.ReadAsStringAsync();
        _logger.LogInformation(responseStream);
        return JsonSerializer.Deserialize<LineLoginUserProfile>(responseStream);
    }

    public async Task RevokeAccessTokenAsync(string accessToken, string clientId, string clientSecret)
    {
        var endpoint = "https://api.line.me/oauth2/v2.1/revoke";
        var response = await _httpClient.PostAsync(endpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "access_token", accessToken },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            }));
        response.EnsureSuccessStatusCode();
    }
}