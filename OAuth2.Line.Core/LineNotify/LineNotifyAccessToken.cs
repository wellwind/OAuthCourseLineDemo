using System.Text.Json.Serialization;

namespace OAuth2.Line.Core.LineNotify;

public class LineNotifyAccessToken
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
}