
using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;
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

    public IActionResult Index()
    {
        var idToken = HttpContext.Request.Cookies["IdToken"];
        if (!String.IsNullOrEmpty(idToken))
        {
            var payload = idToken.Split(".")[1];
            payload = payload.Replace('_', '/').Replace('-', '+');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }
            ViewBag.Payload = System.Text.Json.JsonSerializer.Deserialize<IdToken>(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
        }
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

    public async Task<IActionResult> LineLoginCallback([FromQuery(Name = "code")] string code)
    {
        if (String.IsNullOrWhiteSpace(code))
        {
            return BadRequest();
        }

        var accessToken = await _lineLoginService.GetAccessToken(code, _lineLoginConfig.ChannelId, _lineLoginConfig.ChannelSecret, _redirectUri);
        HttpContext.Response.Cookies.Append("AccessToken", accessToken.AccessToken);
        HttpContext.Response.Cookies.Append("ExpiresIn", accessToken.ExpiresIn.ToString());
        HttpContext.Response.Cookies.Append("IdToken", accessToken.IdToken);
        HttpContext.Response.Cookies.Append("RefreshToken", accessToken.RefreshToken);
        HttpContext.Response.Cookies.Append("Scope", accessToken.Scope);
        HttpContext.Response.Cookies.Append("TokenType", accessToken.TokenType);

        return RedirectToAction("Index");
    }
}
