namespace OAuth2.Line.Core.LineLogin;

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

    /// <summary>
    /// 產生 Line Login 連動網址
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="redirectUri"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public string GenerateLineLoginUrl(string clientId, string redirectUri, string state)
    {
        var url = $"https://access.line.me/oauth2/v2.1/authorize?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&state={state}&scope=openid%20profile";
        return url;
    }

    /// <summary>
    /// 取得 Line Login 的 access token
    /// </summary>
    /// <param name="code"></param>
    /// <param name="clientId"></param>
    /// <param name="clientSecret"></param>
    /// <param name="redirectUri"></param>
    /// <returns></returns>
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

    /// <summary>
    /// 驗證 Line Login 的 access token
    /// </summary>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    public async Task<LineLoginVerifyAccessTokenResult> VerifyAccessTokenAsync(string accessToken)
    {
        var endpoint = $"https://api.line.me/oauth2/v2.1/verify?access_token={accessToken}";
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var responseStream = await response.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<LineLoginVerifyAccessTokenResult>(responseStream);
    }

    /// <summary>
    /// 驗證 Line Login 的 id token
    /// </summary>
    /// <param name="idToken"></param>
    /// <param name="cliendId"></param>
    /// <returns></returns>
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

    /// <summary>
    /// 取得 Line 使用者資料
    /// </summary>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    public async Task<LineLoginUserProfile> GetUserProfileAsync(string accessToken)
    {
        var endpoint = "https://api.line.me/v2/profile";
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseStream = await response.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<LineLoginUserProfile>(responseStream);
    }

    /// <summary>
    /// 撤銷 Line Login 的 access token
    /// </summary>
    /// <param name="accessToken"></param>
    /// <param name="clientId"></param>
    /// <param name="clientSecret"></param>
    /// <returns></returns>
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