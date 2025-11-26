using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NoticeBoard.Hubs;
using NoticeBoard.Models;
using NoticeBoard.Models.Requests;
using NoticeBoard.Models.Responses;
using NoticeBoard.Models.ViewModels;
using NoticeBoard.Services;

namespace NoticeBoard.Controllers.Api;

/// <summary>
/// 管理後台 API 控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly ILikeService _likeService;
    private readonly IHubContext<MessageHub, IMessageClient> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminController> _logger;

    private const string AdminSessionKey = "IsAdmin";

    /// <summary>
    /// 建立管理後台 API 控制器
    /// </summary>
    public AdminController(
        IMessageService messageService,
        ILikeService likeService,
        IHubContext<MessageHub, IMessageClient> hubContext,
        IConfiguration configuration,
        ILogger<AdminController> logger)
    {
        _messageService = messageService;
        _likeService = likeService;
        _hubContext = hubContext;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 管理者登入
    /// </summary>
    /// <param name="request">登入請求</param>
    /// <returns>登入結果</returns>
    /// <response code="200">登入成功</response>
    /// <response code="401">認證失敗</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] AdminLoginRequest request)
    {
        var adminUsername = _configuration["Admin:Username"] ?? "admin";
        var adminPassword = _configuration["Admin:Password"] ?? "admin999";

        if (request.Username == adminUsername && request.Password == adminPassword)
        {
            HttpContext.Session.SetString(AdminSessionKey, "true");
            _logger.LogInformation("管理者登入成功");

            return Ok(new LoginResponse
            {
                Success = true,
                Message = "登入成功"
            });
        }

        _logger.LogWarning("管理者登入失敗：使用者名稱或密碼錯誤");
        return Unauthorized(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "認證失敗",
            Status = StatusCodes.Status401Unauthorized,
            Detail = "使用者名稱或密碼錯誤"
        });
    }

    /// <summary>
    /// 管理者登出
    /// </summary>
    /// <returns>登出結果</returns>
    /// <response code="200">登出成功</response>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove(AdminSessionKey);
        _logger.LogInformation("管理者已登出");

        return Ok(new LoginResponse
        {
            Success = true,
            Message = "登出成功"
        });
    }

    /// <summary>
    /// 取得所有留言（含隱藏/刪除）
    /// </summary>
    /// <param name="status">篩選狀態（可選）</param>
    /// <returns>留言列表</returns>
    /// <response code="200">成功取得留言列表</response>
    /// <response code="401">未授權</response>
    [HttpGet("messages")]
    [ProducesResponseType(typeof(List<AdminMessageViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMessages([FromQuery] MessageStatus? status = null)
    {
        if (!IsAdminAuthenticated())
        {
            return UnauthorizedResponse();
        }

        var messages = await _messageService.GetAllMessagesAsync(status);
        return Ok(messages);
    }

    /// <summary>
    /// 隱藏留言
    /// </summary>
    /// <param name="id">留言識別碼</param>
    /// <returns>更新後的留言</returns>
    /// <response code="200">隱藏成功</response>
    /// <response code="401">未授權</response>
    /// <response code="404">留言不存在</response>
    /// <response code="409">留言已被刪除，無法隱藏</response>
    [HttpPost("messages/{id}/hide")]
    [ProducesResponseType(typeof(AdminMessageViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> HideMessage(string id)
    {
        if (!IsAdminAuthenticated())
        {
            return UnauthorizedResponse();
        }

        // 檢查留言是否存在及狀態
        var allMessages = await _messageService.GetAllMessagesAsync();
        var message = allMessages.FirstOrDefault(m => m.Id == id);

        if (message is null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "留言不存在",
                Status = StatusCodes.Status404NotFound,
                Detail = $"找不到識別碼為 {id} 的留言"
            });
        }

        if (message.Status == MessageStatus.Deleted)
        {
            return Conflict(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "操作衝突",
                Status = StatusCodes.Status409Conflict,
                Detail = "留言已被刪除，無法隱藏"
            });
        }

        if (message.Status == MessageStatus.Hidden)
        {
            return Conflict(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "操作衝突",
                Status = StatusCodes.Status409Conflict,
                Detail = "留言已是隱藏狀態"
            });
        }

        var success = await _messageService.HideMessageAsync(id);
        if (!success)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "操作失敗",
                Status = StatusCodes.Status404NotFound,
                Detail = "無法隱藏留言"
            });
        }

        // 透過 SignalR 通知前台移除該留言
        await _hubContext.Clients.All.MessageDeleted(id);

        // 取得更新後的留言資訊
        var updatedMessages = await _messageService.GetAllMessagesAsync();
        var updatedMessage = updatedMessages.FirstOrDefault(m => m.Id == id);

        _logger.LogInformation("管理者隱藏留言: {MessageId}", id);
        return Ok(updatedMessage);
    }

    /// <summary>
    /// 恢復留言
    /// </summary>
    /// <param name="id">留言識別碼</param>
    /// <returns>更新後的留言</returns>
    /// <response code="200">恢復成功</response>
    /// <response code="401">未授權</response>
    /// <response code="404">留言不存在</response>
    /// <response code="409">留言非隱藏狀態，無法恢復</response>
    [HttpPost("messages/{id}/restore")]
    [ProducesResponseType(typeof(AdminMessageViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RestoreMessage(string id)
    {
        if (!IsAdminAuthenticated())
        {
            return UnauthorizedResponse();
        }

        // 檢查留言是否存在及狀態
        var allMessages = await _messageService.GetAllMessagesAsync();
        var message = allMessages.FirstOrDefault(m => m.Id == id);

        if (message is null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "留言不存在",
                Status = StatusCodes.Status404NotFound,
                Detail = $"找不到識別碼為 {id} 的留言"
            });
        }

        if (message.Status != MessageStatus.Hidden)
        {
            return Conflict(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "操作衝突",
                Status = StatusCodes.Status409Conflict,
                Detail = "留言非隱藏狀態，無法恢復"
            });
        }

        var success = await _messageService.RestoreMessageAsync(id);
        if (!success)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "操作失敗",
                Status = StatusCodes.Status404NotFound,
                Detail = "無法恢復留言"
            });
        }

        // 取得更新後的留言資訊並透過 SignalR 通知前台
        var publicMessages = await _messageService.GetPublicMessagesAsync();
        var restoredPublicMessage = publicMessages.FirstOrDefault(m => m.Id == id);
        if (restoredPublicMessage is not null)
        {
            await _hubContext.Clients.All.ReceiveMessage(restoredPublicMessage);
        }

        // 取得更新後的留言資訊
        var updatedMessages = await _messageService.GetAllMessagesAsync();
        var updatedMessage = updatedMessages.FirstOrDefault(m => m.Id == id);

        _logger.LogInformation("管理者恢復留言: {MessageId}", id);
        return Ok(updatedMessage);
    }

    /// <summary>
    /// 刪除留言（永久）
    /// </summary>
    /// <param name="id">留言識別碼</param>
    /// <returns>無內容</returns>
    /// <response code="204">刪除成功</response>
    /// <response code="401">未授權</response>
    /// <response code="404">留言不存在</response>
    /// <response code="409">留言已被刪除</response>
    [HttpDelete("messages/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteMessage(string id)
    {
        if (!IsAdminAuthenticated())
        {
            return UnauthorizedResponse();
        }

        // 檢查留言是否存在及狀態
        var allMessages = await _messageService.GetAllMessagesAsync();
        var message = allMessages.FirstOrDefault(m => m.Id == id);

        if (message is null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "留言不存在",
                Status = StatusCodes.Status404NotFound,
                Detail = $"找不到識別碼為 {id} 的留言"
            });
        }

        if (message.Status == MessageStatus.Deleted)
        {
            return Conflict(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "操作衝突",
                Status = StatusCodes.Status409Conflict,
                Detail = "留言已被刪除"
            });
        }

        var wasPublic = message.Status == MessageStatus.Public;
        var success = await _messageService.DeleteMessageAsync(id);
        if (!success)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "操作失敗",
                Status = StatusCodes.Status404NotFound,
                Detail = "無法刪除留言"
            });
        }

        // 如果留言原本是公開的，透過 SignalR 通知前台移除
        if (wasPublic)
        {
            await _hubContext.Clients.All.MessageDeleted(id);
        }

        _logger.LogInformation("管理者刪除留言: {MessageId}", id);
        return NoContent();
    }

    /// <summary>
    /// 手動觸發清理
    /// </summary>
    /// <returns>清理結果</returns>
    /// <response code="200">清理完成</response>
    /// <response code="401">未授權</response>
    [HttpPost("cleanup")]
    [ProducesResponseType(typeof(CleanupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ManualCleanup()
    {
        if (!IsAdminAuthenticated())
        {
            return UnauthorizedResponse();
        }

        var retentionHours = _configuration.GetValue<int>("Cleanup:RetentionHours", 24);

        var messagesDeleted = await _messageService.CleanupOldMessagesAsync(retentionHours);
        var likesDeleted = await _likeService.CleanupOldLikesAsync(retentionHours);

        _logger.LogInformation("手動清理完成: 刪除 {MessagesCount} 則留言、{LikesCount} 則按讚記錄",
            messagesDeleted, likesDeleted);

        return Ok(new CleanupResponse
        {
            MessagesDeleted = messagesDeleted,
            LikesDeleted = likesDeleted
        });
    }

    /// <summary>
    /// 檢查管理者是否已認證
    /// </summary>
    private bool IsAdminAuthenticated()
    {
        var isAdmin = HttpContext.Session.GetString(AdminSessionKey);
        return isAdmin == "true";
    }

    /// <summary>
    /// 回傳未授權回應
    /// </summary>
    private IActionResult UnauthorizedResponse()
    {
        return Unauthorized(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "未授權",
            Status = StatusCodes.Status401Unauthorized,
            Detail = "請先登入管理後台"
        });
    }
}
