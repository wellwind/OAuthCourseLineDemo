
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
        var accessToken = HttpContext.Request.Cookies["AccessToken"];
        var idToken = HttpContext.Request.Cookies["IdToken"];

        if (String.IsNullOrEmpty(accessToken) || String.IsNullOrEmpty(idToken))
        {
            return View();
        }

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

        var user = await _lineLoginService.GetUserProfileAsync(accessToken);
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

    public IActionResult LineLogin()
    {
        var state = _jwtService.GenerateToken(_jwtConfig.SignKey, _jwtConfig.Issuer, new Claim[] { }, DateTime.UtcNow.AddMinutes(10));
        return Redirect(_lineLoginService.GenerateLineLoginUrl(
            _lineLoginConfig.ChannelId,
            UrlEncoder.Default.Encode(_lineLoginRedirectUri),
            state));
    }

    public async Task<IActionResult> LineLogoutAsync()
    {
        var accessToken = HttpContext.Request.Cookies["AccessToken"];
        var idToken = HttpContext.Request.Cookies["IdToken"];

        try
        {
            await _lineLoginService.RevokeAccessTokenAsync(accessToken, _lineLoginConfig.ChannelId, _lineLoginConfig.ChannelSecret);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

        HttpContext.Response.Cookies.Delete("AccessToken");
        HttpContext.Response.Cookies.Delete("ExpiresIn");
        HttpContext.Response.Cookies.Delete("IdToken");
        HttpContext.Response.Cookies.Delete("RefreshToken");
        HttpContext.Response.Cookies.Delete("Scope");
        HttpContext.Response.Cookies.Delete("TokenType");

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> LineLoginCallback([FromQuery(Name = "code")] string code, [FromQuery(Name = "state")] string state)
    {
        if (String.IsNullOrWhiteSpace(code))
        {
            return BadRequest();
        }

        var stateValidateResult = _jwtService.ValidateToken(state, _jwtConfig.Issuer, _jwtConfig.SignKey, out var exception);
        if (stateValidateResult is null)
        {
            _logger.LogError(exception.Message);
            return BadRequest();
        }

        var accessToken = await _lineLoginService.GetAccessTokenAsync(code, _lineLoginConfig.ChannelId, _lineLoginConfig.ChannelSecret, _lineLoginRedirectUri);

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

    public async Task<IActionResult> BindLineNotify()
    {
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

        var state = _jwtService.GenerateToken(_jwtConfig.SignKey, _jwtConfig.Issuer, new Claim[] { new Claim("sub", idTokenVerifyResult.Sub) }, DateTime.UtcNow.AddMinutes(10));

        var url = _lineNotifyService.GetAuthorizeUrl(_lineNotifyConfig.ClientId, UrlEncoder.Default.Encode(_lineNotifyRedirectUri), state);

        return Redirect(url);
    }

    public async Task<IActionResult> LineNotifyCallback([FromQuery(Name = "code")] string code, [FromQuery(Name = "state")] string state)
    {
        var stateVerifyResult = _jwtService.ValidateToken(state, _jwtConfig.Issuer, _jwtConfig.SignKey, out var exception);
        if (stateVerifyResult is null)
        {
            _logger.LogError(exception.Message);
            return RedirectToAction("Index");
        }

        var lineNotifyAccessToken = await _lineNotifyService.GetAccessTokenAsync(code, _lineNotifyConfig.ClientId, _lineNotifyConfig.ClientSecret, _lineNotifyRedirectUri);

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

        await _lineNotifyBindingService.UpdateLineNotifyAccessTokenAsync(idTokenVerifyResult.Sub, lineNotifyAccessToken);

        await _lineNotifyService.SendMessageAsync(lineNotifyAccessToken, "綁定成功");

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> RevokeLineNotify()
    {
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

        var lineNotifyAccessToken = await _lineNotifyBindingService.GetLineNotifyAccessTokenAsync(idTokenVerifyResult.Sub);
        await _lineNotifyBindingService.ClearLineNotifyAccessTokenAsync(idTokenVerifyResult.Sub);
        await _lineNotifyService.RevokeAccessTokenAsync(lineNotifyAccessToken);

        return RedirectToAction("Index");
    }
}
