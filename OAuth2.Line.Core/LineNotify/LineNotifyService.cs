using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace OAuth2.Line.Core.LineNotify;

public class LineNotifyService
{
    private ILogger<LineNotifyService> _logger;
    private readonly HttpClient _httpClient;

    public LineNotifyService(ILogger<LineNotifyService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("LineNotifyService");
    }

    /// <summary>
    /// 產生 Line Notify 連動網址
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="redirectUri"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public string GetAuthorizeUrl(string clientId, string redirectUri, string state)
    {
        return $"https://notify-bot.line.me/oauth/authorize?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&scope=notify&state={state}";
    }

    /// <summary>
    /// 依照 code 取得 access token
    /// </summary>
    /// <param name="code"></param>
    /// <param name="clientId"></param>
    /// <param name="clientSecret"></param>
    /// <param name="returnUri"></param>
    /// <returns></returns>
    public async Task<string> GetAccessTokenAsync(string code, string clientId, string clientSecret, string returnUri)
    {
        var endpoint = "https://notify-bot.line.me/oauth/token";
        var response = await _httpClient.PostAsync(endpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", returnUri },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            }));
        response.EnsureSuccessStatusCode();

        var responseStream = await response.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<LineNotifyAccessToken>(responseStream).AccessToken;
    }

    /// <summary>
    /// 撤銷 Line Notify 的 access token
    /// </summary>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    public async Task RevokeAccessTokenAsync(string accessToken)
    {
        var endpoint = "https://notify-api.line.me/api/revoke";
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// 發送 Line Notify 訊息
    /// </summary>
    /// <param name="accessToken"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task<bool> SendMessageAsync(string accessToken, string message)
    {
        var endpoint = "https://notify-api.line.me/api/notify";
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "message", message }
            });

        var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        return true;
    }
}