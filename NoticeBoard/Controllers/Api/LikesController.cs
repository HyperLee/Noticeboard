using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NoticeBoard.Hubs;
using NoticeBoard.Models.Requests;
using NoticeBoard.Models.Responses;
using NoticeBoard.Services;

namespace NoticeBoard.Controllers.Api;

/// <summary>
/// 按讚 API 控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LikesController : ControllerBase
{
    private const string AnonymousTokenCookieName = "AnonToken";
    private const int TokenLength = 32;
    private const int TokenCookieExpirationDays = 30;

    private readonly ILikeService _likeService;
    private readonly IMessageService _messageService;
    private readonly IHubContext<MessageHub, IMessageClient> _hubContext;
    private readonly ILogger<LikesController> _logger;

    /// <summary>
    /// 建立 LikesController 實例
    /// </summary>
    public LikesController(
        ILikeService likeService,
        IMessageService messageService,
        IHubContext<MessageHub, IMessageClient> hubContext,
        ILogger<LikesController> logger)
    {
        _likeService = likeService;
        _messageService = messageService;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// 按讚或取消按讚（toggle）
    /// </summary>
    /// <param name="request">按讚請求</param>
    /// <returns>按讚結果</returns>
    /// <response code="200">按讚狀態更新成功</response>
    /// <response code="400">驗證失敗</response>
    /// <response code="404">留言不存在</response>
    [HttpPost]
    [ProducesResponseType(typeof(LikeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LikeResponse>> ToggleLike([FromBody] LikeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // 驗證留言是否存在且為公開狀態
        var message = await _messageService.GetMessageAsync(request.MessageId);
        if (message is null)
        {
            return Problem(
                title: "留言不存在",
                detail: $"找不到識別碼為 {request.MessageId} 的留言",
                statusCode: StatusCodes.Status404NotFound);
        }

        // 取得或建立匿名 Token
        var token = GetOrCreateAnonymousToken();

        try
        {
            // 切換按讚狀態
            var (liked, likeCount) = await _likeService.ToggleLikeAsync(request.MessageId, token);

            _logger.LogDebug(
                "按讚狀態變更: MessageId={MessageId}, Liked={Liked}, LikeCount={LikeCount}",
                request.MessageId, liked, likeCount);

            // 廣播按讚數更新給所有客戶端
            await _hubContext.Clients.All.LikeCountUpdated(request.MessageId, likeCount);

            return Ok(new LikeResponse
            {
                MessageId = request.MessageId,
                Liked = liked,
                LikeCount = likeCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按讚操作失敗: MessageId={MessageId}", request.MessageId);
            return Problem(
                title: "按讚操作失敗",
                detail: "處理按讚請求時發生錯誤",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// 檢查是否已按讚
    /// </summary>
    /// <param name="messageId">留言識別碼</param>
    /// <returns>按讚狀態</returns>
    /// <response code="200">成功取得按讚狀態</response>
    /// <response code="404">留言不存在</response>
    [HttpGet("{messageId}")]
    [ProducesResponseType(typeof(LikeStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LikeStatusResponse>> CheckLikeStatus(string messageId)
    {
        // 驗證留言是否存在
        var message = await _messageService.GetMessageAsync(messageId);
        if (message is null)
        {
            return Problem(
                title: "留言不存在",
                detail: $"找不到識別碼為 {messageId} 的留言",
                statusCode: StatusCodes.Status404NotFound);
        }

        // 取得匿名 Token（若無則建立新的）
        var token = GetOrCreateAnonymousToken();

        var liked = await _likeService.HasLikedAsync(messageId, token);

        return Ok(new LikeStatusResponse
        {
            MessageId = messageId,
            Liked = liked
        });
    }

    /// <summary>
    /// 取得或建立匿名 Token
    /// </summary>
    /// <remarks>
    /// Token 使用安全隨機數產生，不含任何 PII。
    /// 儲存於 HttpOnly Cookie 中，有效期 30 天。
    /// </remarks>
    /// <returns>匿名 Token 字串</returns>
    private string GetOrCreateAnonymousToken()
    {
        // 嘗試從 Cookie 取得現有 Token
        if (Request.Cookies.TryGetValue(AnonymousTokenCookieName, out var existingToken) 
            && !string.IsNullOrWhiteSpace(existingToken))
        {
            return existingToken;
        }

        // 產生新的隨機 Token
        var tokenBytes = RandomNumberGenerator.GetBytes(TokenLength);
        var newToken = Convert.ToBase64String(tokenBytes);

        // 設定 Cookie
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(TokenCookieExpirationDays),
            IsEssential = true // GDPR: 標記為必要 Cookie（功能性用途）
        };

        Response.Cookies.Append(AnonymousTokenCookieName, newToken, cookieOptions);
        _logger.LogDebug("已建立新的匿名 Token");

        return newToken;
    }
}
