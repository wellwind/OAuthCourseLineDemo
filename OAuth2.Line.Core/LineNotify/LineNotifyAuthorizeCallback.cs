using System.Text.Json.Serialization;

namespace OAuth2.Line.Core.LineNotify;

public class LineNotifyAuthorizeCallback
{
    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }
}