using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth2.Line.Core.LineNotify;
using OAuth2.Line.Core.LineNotifyBinding;
using OAuth2.Line.Core.Message;
using OAuth2.Line.Dashboard.Models;

namespace OAuth2.Line.Dashboard.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly LineNotifyConfig _lineNotifyConfig;
    private readonly LineNotifyService _lineNotifyService;
    private readonly LineNotifyBindingService _lineNotifyBindingService;
    private readonly MessageService _messageService;

    public HomeController(
        ILogger<HomeController> logger,
        IOptions<LineNotifyConfig> lineNotifyConfigOptions,
        LineNotifyService lineNotifyService,
        LineNotifyBindingService lineNotifyBindingService,
        MessageService messageService)
    {
        _logger = logger;
        _lineNotifyConfig = lineNotifyConfigOptions.Value;
        _lineNotifyService = lineNotifyService;
        _lineNotifyBindingService = lineNotifyBindingService;
        _messageService = messageService;
    }

    public IActionResult Index()
    {
        ViewBag.FlashMessageType = TempData["FlashMessageType"] as String;
        ViewBag.FlashMessage = TempData["FlashMessage"] as String;

        ViewBag.SendLogs = _messageService.GetMessages().OrderByDescending(item => item.CreatedAt);
        ViewBag.Subscribers = _lineNotifyBindingService.GetLineNotifyBindings();
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BroadcastMessage(string message, string packageId, string stickerId)
    {
        _logger.LogError(packageId);

        if (!ModelState.IsValid)
        {
            TempData["FlashMessage"] = "Message is required.";
            return RedirectToAction("Index");
        }

        // 先建立一筆 message
        var messageId = await _messageService.CreateMessage(message, packageId, stickerId);

        // 找出所有已連動 Line Notify 的帳號
        var bindings = _lineNotifyBindingService.GetLineNotifyBindings()
            .Where(binding => !String.IsNullOrEmpty(binding.LineNotifyAccessToken))
            .ToList();
        foreach (var binding in bindings)
        {
            // 將該帳號的訊息狀態設為 false
            await _messageService.UpdateMessageStatusAsync(binding.Sub, messageId, false, null);

            try
            {
                // 發送訊息
                await _lineNotifyService.SendMessageAsync(binding.LineNotifyAccessToken, message, packageId, stickerId);

                // 將該帳號的訊息狀態設為 true
                await _messageService.UpdateMessageStatusAsync(binding.Sub, messageId, true, null);
            }
            catch (Exception ex)
            {
                // 如果有錯誤，記錄錯誤訊息
                _logger.LogError(ex.Message);
                await _messageService.UpdateMessageStatusAsync(binding.Sub, messageId, false, ex.Message);
            }
        }

        TempData["FlashMessageType"] = "success";
        TempData["FlashMessage"] = "Message sent";
        return RedirectToAction("Index");
    }

    public IActionResult MessageDetails(int id)
    {
        var message = _messageService.GetMessages().FirstOrDefault(item => item.Id == id);
        if (message is null)
        {
            return NotFound();
        }

        var result = _messageService.GetMessageStatuses(id);

        ViewBag.MessageText = message.MessageText;
        ViewBag.StickerId = message.StickerId;
        return View(result);
    }
}
