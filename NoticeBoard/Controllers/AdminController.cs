using Microsoft.AspNetCore.Mvc;
using NoticeBoard.Models.Requests;
using NoticeBoard.Services;

namespace NoticeBoard.Controllers;

/// <summary>
/// 管理後台 MVC 控制器
/// </summary>
/// <remarks>
/// 此控制器處理管理後台的頁面路由，與 Api/AdminController 分開處理。
/// 認證由 AdminAuthorizationMiddleware 處理。
/// </remarks>
public class AdminController : Controller
{
    private readonly IMessageService _messageService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminController> _logger;

    private const string AdminSessionKey = "IsAdmin";

    /// <summary>
    /// 建立管理後台 MVC 控制器
    /// </summary>
    public AdminController(
        IMessageService messageService,
        IConfiguration configuration,
        ILogger<AdminController> logger)
    {
        _messageService = messageService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 後台首頁（留言管理）
    /// </summary>
    /// <remarks>需要認證，由中介軟體處理</remarks>
    public async Task<IActionResult> Index()
    {
        var messages = await _messageService.GetAllMessagesAsync();
        return View(messages);
    }

    /// <summary>
    /// 登入頁面
    /// </summary>
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // 如果已登入，重新導向至後台首頁
        var isAdmin = HttpContext.Session.GetString(AdminSessionKey);
        if (isAdmin == "true")
        {
            return RedirectToAction(nameof(Index));
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    /// <summary>
    /// 處理登入表單提交
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(AdminLoginRequest request, string? returnUrl = null)
    {
        var adminUsername = _configuration["Admin:Username"] ?? "admin";
        var adminPassword = _configuration["Admin:Password"] ?? "admin999";

        if (request.Username == adminUsername && request.Password == adminPassword)
        {
            HttpContext.Session.SetString(AdminSessionKey, "true");
            _logger.LogInformation("管理者透過 MVC 登入成功");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        _logger.LogWarning("管理者透過 MVC 登入失敗：使用者名稱或密碼錯誤");
        ModelState.AddModelError(string.Empty, "使用者名稱或密碼錯誤");
        ViewData["ReturnUrl"] = returnUrl;
        return View(request);
    }

    /// <summary>
    /// 登出
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove(AdminSessionKey);
        _logger.LogInformation("管理者透過 MVC 登出");
        return RedirectToAction(nameof(Login));
    }
}
