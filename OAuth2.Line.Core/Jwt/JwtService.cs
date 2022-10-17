namespace OAuth2.Line.Core.Jwt;

using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

public class JwtService
{
    /// <summary>
    /// 產生 Jwt Token
    /// </summary>
    /// <param name="signKey"></param>
    /// <param name="issuer"></param>
    /// <param name="claims"></param>
    /// <param name="expires"></param>
    /// <returns></returns>
    public string GenerateToken(string signKey, string issuer, IEnumerable<Claim> claims, DateTime? expires = null)
    {
        var userClaimsIdentity = new ClaimsIdentity(claims);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signKey));

        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

        // 建立 SecurityTokenDescriptor
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Subject = userClaimsIdentity,
            SigningCredentials = signingCredentials
        };

        if (expires.HasValue)
        {
            // 避免 IDX12401 錯誤，設定一個 expires 之前的時間當作 nbf
            tokenDescriptor.NotBefore = DateTime.UtcNow.AddSeconds(-1);
            tokenDescriptor.Expires = expires.Value;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(securityToken);
    }

    /// <summary>
    /// 驗證 Jwt Token
    /// </summary>
    /// <param name="token"></param>
    /// <param name="issuer"></param>
    /// <param name="signKey"></param>
    /// <param name="validateErrorException"></param>
    /// <returns></returns>
    public SecurityToken ValidateToken(string token, string issuer, string signKey, out Exception validateErrorException)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signKey));

        var tokenHandler = new JwtSecurityTokenHandler();

        var parameters = new TokenValidationParameters()
        {
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            IssuerSigningKey = securityKey,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            tokenHandler.ValidateToken(token, parameters, out SecurityToken validatedToken);
            validateErrorException = null;
            return validatedToken;
        }
        catch (Exception ex)
        {
            validateErrorException = ex;
            return null;
        }
    }
}
