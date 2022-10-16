
using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using OAuth2.Line.Core;
using OAuth2.Line.Frontdesk.Models;

namespace OAuth2.Line.Frontdesk.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _configuration;
    private readonly LineLoginConfig _lineLoginConfig;
    private readonly LineLoginService _lineLoginService;

    private string _redirectUri
    {
        get { return $"{Request.Scheme}://{Request.Host}{Request.PathBase}{_lineLoginConfig.ReturnPath}"; }
    }

    public HomeController(
        ILogger<HomeController> logger,
        IOptions<LineLoginConfig> lineLoginConfigOptions,
        LineLoginService lineLoginService)
    {
        _logger = logger;
        _lineLoginConfig = lineLoginConfigOptions.Value;
        _lineLoginService = lineLoginService;
    }

    public DateTime UnixTimeStampToDateTime(double unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch
        System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dtDateTime;
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
        ViewBag.User = user;
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
        return Redirect(_lineLoginService.GenerateLineLoginUrl(
            _lineLoginConfig.ChannelId,
            UrlEncoder.Default.Encode(_redirectUri),
            Guid.NewGuid().ToString()));
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

    public async Task<IActionResult> LineLoginCallback([FromQuery(Name = "code")] string code)
    {
        if (String.IsNullOrWhiteSpace(code))
        {
            return BadRequest();
        }

        var accessToken = await _lineLoginService.GetAccessTokenAsync(code, _lineLoginConfig.ChannelId, _lineLoginConfig.ChannelSecret, _redirectUri);

        if (TryParseIdToken(accessToken.IdToken, out var idToken))
        {
            // TODO: write idToken & accessToken to database

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
}
