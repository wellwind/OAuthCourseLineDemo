namespace OAuth2.Line.Core;

using System.Text.Json.Serialization;

public class LineLoginVerifyAccessTokenResult
{
    [JsonPropertyName("scope")]
    public string Scope {get;set;}

    [JsonPropertyName("client_id")]
    public string CliendId {get;set;}

    [JsonPropertyName("expires_in")]
    public int ExpiresIn {get;set;}
}