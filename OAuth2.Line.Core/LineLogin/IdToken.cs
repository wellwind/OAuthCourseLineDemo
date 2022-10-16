using System.Text.Json.Serialization;

namespace OAuth2.Line.Core.LineLogin;

public class IdToken
{
    [JsonPropertyName("iss")]
    public string Iss { get; set; }

    [JsonPropertyName("sub")]
    public string Sub { get; set; }

    [JsonPropertyName("aud")]
    public string Aud { get; set; }

    [JsonPropertyName("exp")]
    public int Exp { get; set; }

    [JsonPropertyName("iat")]
    public int Iat { get; set; }

    [JsonPropertyName("amr")]
    public List<string> Amr { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("picture")]
    public string Picture { get; set; }
}

