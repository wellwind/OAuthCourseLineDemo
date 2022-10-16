namespace OAuth2.Line.Core;

using System.Text.Json.Serialization;

public class LineLoginVerifyIdTokenResult
{
    [JsonPropertyName("iss")]
    public string Iss { get; set; }

    [JsonPropertyName("sub")]
    public string Sub { get; set; }

    [JsonPropertyName("aud")]
    public string Aud { get; set; }

    [JsonPropertyName("exp")]
    public int exp { get; set; }

    [JsonPropertyName("iat")]
    public int Iat { get; set; }

    [JsonPropertyName("auth_time")]
    public int AuthTime { get; set; }

    [JsonPropertyName("nonce")]
    public string Nonce { get; set; }

    [JsonPropertyName("amr")]
    public IEnumerable<string> amr { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("picture")]
    public string Picture { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }
}