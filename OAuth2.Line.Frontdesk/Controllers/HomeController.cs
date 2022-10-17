
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using OAuth2.Line.Core.Jwt;
using OAuth2.Line.Core.LineLogin;
using OAuth2.Line.Core.LineNotify;
using OAuth2.Line.Core.LineNotifyBinding;
using OAuth2.Line.Frontdesk.Models;

namespace OAuth2.Line.Frontdesk.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _configuration;
    private readonly LineLoginConfig _lineLoginConfig;
    private readonly LineLoginService _lineLoginService;
    private readonly LineNotifyConfig _lineNotifyConfig;
    private readonly JwtConfig _jwtConfig;
    private readonly LineNotifyService _lineNotifyService;
    private readonly JwtService _jwtService;
    private readonly LineNotifyBindingService _lineNotifyBindingService;

    private string _lineLoginRedirectUri
    {
        get { return $"{Request.Scheme}://{Request.Host}{Request.PathBase}{_lineLoginConfig.ReturnPath}"; }
    }

    private string _lineNotifyRedirectUri
    {
        get { return $"{Request.Scheme}://{Request.Host}{Request.PathBase}{_lineNotifyConfig.ReturnPath}"; }
    }

    public HomeController(
        ILogger<HomeController> logger,
        IOptions<LineLoginConfig> lineLoginConfigOptions,
        IOptions<LineNotifyConfig> lineNotifyConfigOptions,
        IOptions<JwtConfig> jwtConfigOptions,
        LineLoginService lineLoginService,
        LineNotifyService lineNotifyService,
        JwtService jwtService,
        LineNotifyBindingService lineNotifyBindingService)
    {
        _logger = logger;
        _lineLoginConfig = lineLoginConfigOptions.Value;
        _lineNotifyConfig = lineNotifyConfigOptions.Value;
        _jwtConfig = jwtConfigOptions.Value;
        _lineLoginService = lineLoginService;
        _lineNotifyService = lineNotifyService;
        _jwtService = jwtService;
        _lineNotifyBindingService = lineNotifyBindingService;
    }

    /// <summary>
    /// 取得 JwtToken 的 payload 部分，型別為 IdToken
    /// </summary>
    /// <param name="jwtToken"></param>
    /// <param name="idToken"></param>
    /// <returns></returns>
    public bool TryParseIdToken(string jwtToken, out IdToken idToken)
    {
        try
        {
            var payloadString = jwtToken.Split(".")[1];
            var result = JsonSerializer.Deserialize<IdToken>(Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(payloadString)));
            if (result is not null)
            {
                idToken = result;
                return true;
            }

        }
        catch { }
        idToken = null;
        return false;
    }

    public async Task<IActionResult> Index()
    {
        // 如果有登入過，目前會把資料存在 cookie 中
        var accessToken = HttpContext.Request.Cookies["AccessToken"];
        var idToken = HttpContext.Request.Cookies["IdToken"];

        if (String.IsNullOrEmpty(accessToken) || String.IsNullOrEmpty(idToken))
        {
            return View();
        }

        // 驗證 LineLogin 的 access token
        LineLoginVerifyAccessTokenResult accessTokenVerifyResult = null;
        try
        {
            accessTokenVerifyResult = await _lineLoginService.VerifyAccessTokenAsync(accessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return View();
        }

        // 驗證 LineLogin 的 id token
        LineLoginVerifyIdTokenResult idTokenVerifyResult = null;
        try
        {
            idTokenVerifyResult = await _lineLoginService.VerifyIdTokenAsync(idToken, _lineLoginConfig.ChannelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return View();
        }

        // 取得目前的 user profile
        var user = await _lineLoginService.GetUserProfileAsync(accessToken);

        // 檢查是否已綁定 Line Notify
        var isLineNotifyBinded = _lineNotifyBindingService.IsLineNotifyAccessTokenBinded(idTokenVerifyResult.Sub);

        ViewBag.User = user;
        ViewBag.IsLineNotifyBinded = isLineNotifyBinded;
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    /// <summary>
    /// 進行 Line Login
    /// </summary>
    /// <returns></returns>
    public IActionResult LineLogin()
    {
        // 產生一個包含簽章的 jtw token 來當作 state，以避免 CSRF 攻擊
        var state = _jwtService.GenerateToken(_jwtConfig.SignKey, _jwtConfig.Issuer, new Claim[] { }, DateTime.UtcNow.AddMinutes(10));

        // 轉到 Line Login 登入網址
        var lineLoginUrl = _lineLoginService.GenerateLineLoginUrl(_lineLoginConfig.ChannelId, UrlEncoder.Default.Encode(_lineLoginRedirectUri), state);
        return Redirect(lineLoginUrl);
    }

    /// <summary>
    /// 登出 Line Login
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult> LineLogoutAsync()
    {
        var accessToken = HttpContext.Request.Cookies["AccessToken"];
        var idToken = HttpContext.Request.Cookies["IdToken"];

        try
        {
            // 撤銷 Line Login 的 access token
            await _lineLoginService.RevokeAccessTokenAsync(accessToken, _lineLoginConfig.ChannelId, _lineLoginConfig.ChannelSecret);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

        // 刪除 cookie 資料
        HttpContext.Response.Cookies.Delete("AccessToken");
        HttpContext.Response.Cookies.Delete("ExpiresIn");
        HttpContext.Response.Cookies.Delete("IdToken");
        HttpContext.Response.Cookies.Delete("RefreshToken");
        HttpContext.Response.Cookies.Delete("Scope");
        HttpContext.Response.Cookies.Delete("TokenType");

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Line Login 的 callback action
    /// </summary>
    /// <param name="code"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public async Task<IActionResult> LineLoginCallback([FromQuery(Name = "code")] string code, [FromQuery(Name = "state")] string state)
    {
        if (String.IsNullOrWhiteSpace(code))
        {
            return BadRequest();
        }

        // 驗證 state 簽章
        var stateValidateResult = _jwtService.ValidateToken(state, _jwtConfig.Issuer, _jwtConfig.SignKey, out var exception);
        if (stateValidateResult is null)
        {
            _logger.LogError(exception.Message);
            return BadRequest();
        }

        // 透過 code 取得 access token
        var accessToken = await _lineLoginService.GetAccessTokenAsync(code, _lineLoginConfig.ChannelId, _lineLoginConfig.ChannelSecret, _lineLoginRedirectUri);

        // 取得 id token 物件後，將相關資訊塞到 cookie 中
        if (TryParseIdToken(accessToken.IdToken, out var idToken))
        {
            await _lineNotifyBindingService.UpdateLoginAsync(
                idToken.Sub,
                idToken.Name,
                idToken.Picture,
                accessToken.AccessToken,
                accessToken.RefreshToken,
                accessToken.IdToken);

            HttpContext.Response.Cookies.Append("AccessToken", accessToken.AccessToken);
            HttpContext.Response.Cookies.Append("ExpiresIn", accessToken.ExpiresIn.ToString());
            HttpContext.Response.Cookies.Append("IdToken", accessToken.IdToken);
            HttpContext.Response.Cookies.Append("RefreshToken", accessToken.RefreshToken);
            HttpContext.Response.Cookies.Append("Scope", accessToken.Scope);
            HttpContext.Response.Cookies.Append("TokenType", accessToken.TokenType);

            return RedirectToAction("Index");
        }

        return BadRequest();
    }

    /// <summary>
    /// 綁定 Line Notify
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult> BindLineNotify()
    {
        // 驗證現在的 IdToken 是否有效
        var idToken = HttpContext.Request.Cookies["IdToken"];
        LineLoginVerifyIdTokenResult idTokenVerifyResult = null;
        try
        {
            idTokenVerifyResult = await _lineLoginService.VerifyIdTokenAsync(idToken, _lineLoginConfig.ChannelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return RedirectToAction("Index");
        }

        // 從 IdToken 中拿到 sub，並以 sub 來產生 jwt token，之後 callback 時便可將 sub 與 line notify 的 access token 綁定
        var state = _jwtService.GenerateToken(_jwtConfig.SignKey, _jwtConfig.Issuer, new Claim[] { new Claim("sub", idTokenVerifyResult.Sub) }, DateTime.UtcNow.AddMinutes(10));

        // 轉到 Line Notify 連動頁面
        var url = _lineNotifyService.GetAuthorizeUrl(_lineNotifyConfig.ClientId, UrlEncoder.Default.Encode(_lineNotifyRedirectUri), state);
        return Redirect(url);
    }

    /// <summary>
    /// Line Notify 連動的 callback action
    /// </summary>
    /// <param name="code"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public async Task<IActionResult> LineNotifyCallback([FromQuery(Name = "code")] string code, [FromQuery(Name = "state")] string state)
    {
        // 驗證 state
        var stateVerifyResult = _jwtService.ValidateToken(state, _jwtConfig.Issuer, _jwtConfig.SignKey, out var exception);
        if (stateVerifyResult is null)
        {
            _logger.LogError(exception.Message);
            return RedirectToAction("Index");
        }

        // 驗證 id token 依然有效
        var idToken = HttpContext.Request.Cookies["IdToken"];
        LineLoginVerifyIdTokenResult idTokenVerifyResult = null;
        try
        {
            idTokenVerifyResult = await _lineLoginService.VerifyIdTokenAsync(idToken, _lineLoginConfig.ChannelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return RedirectToAction("Index");
        }

        // 透過 code 取得 acccess token
        var lineNotifyAccessToken = await _lineNotifyService.GetAccessTokenAsync(code, _lineNotifyConfig.ClientId, _lineNotifyConfig.ClientSecret, _lineNotifyRedirectUri);

        // 更新資料庫綁定狀態
        await _lineNotifyBindingService.UpdateLineNotifyAccessTokenAsync(idTokenVerifyResult.Sub, lineNotifyAccessToken);

        // 發送訊息
        await _lineNotifyService.SendMessageAsync(lineNotifyAccessToken, "綁定成功");

        return RedirectToAction("Index");
    }

    /// <summary>
    /// 撤銷 Line Notify 的 access token
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult> RevokeLineNotify()
    {
        // 驗證 id token 依然有效
        var idToken = HttpContext.Request.Cookies["IdToken"];
        LineLoginVerifyIdTokenResult idTokenVerifyResult = null;
        try
        {
            idTokenVerifyResult = await _lineLoginService.VerifyIdTokenAsync(idToken, _lineLoginConfig.ChannelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return RedirectToAction("Index");
        }

        // 先從資料庫中取得 access token
        var lineNotifyAccessToken = await _lineNotifyBindingService.GetLineNotifyAccessTokenAsync(idTokenVerifyResult.Sub);

        // 清除 sub 的 access token
        await _lineNotifyBindingService.ClearLineNotifyAccessTokenAsync(idTokenVerifyResult.Sub);

        // 撤銷 access token
        await _lineNotifyService.RevokeAccessTokenAsync(lineNotifyAccessToken);

        return RedirectToAction("Index");
    }
}
