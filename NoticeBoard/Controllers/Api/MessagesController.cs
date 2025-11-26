using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NoticeBoard.Hubs;
using NoticeBoard.Models.Requests;
using NoticeBoard.Models.ViewModels;
using NoticeBoard.Services;

namespace NoticeBoard.Controllers.Api;

/// <summary>
/// 留言 API 控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly IHubContext<MessageHub, IMessageClient> _hubContext;
    private readonly ILogger<MessagesController> _logger;

    /// <summary>
    /// 建立 MessagesController 實例
    /// </summary>
    public MessagesController(
        IMessageService messageService,
        IHubContext<MessageHub, IMessageClient> hubContext,
        ILogger<MessagesController> logger)
    {
        _messageService = messageService;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// 取得所有公開留言
    /// </summary>
    /// <returns>公開留言列表（最新在上）</returns>
    /// <response code="200">成功取得留言列表</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<MessageViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MessageViewModel>>> GetMessages()
    {
        var messages = await _messageService.GetPublicMessagesAsync();
        return Ok(messages);
    }

    /// <summary>
    /// 取得單則留言
    /// </summary>
    /// <param name="id">留言識別碼</param>
    /// <returns>留言視圖模型</returns>
    /// <response code="200">成功取得留言</response>
    /// <response code="404">留言不存在</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MessageViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageViewModel>> GetMessage(string id)
    {
        var message = await _messageService.GetMessageAsync(id);
        if (message is null)
        {
            return Problem(
                title: "留言不存在",
                detail: $"找不到識別碼為 {id} 的留言",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(message);
    }

    /// <summary>
    /// 發表新留言
    /// </summary>
    /// <param name="request">建立留言請求</param>
    /// <returns>建立的留言視圖模型</returns>
    /// <response code="201">留言建立成功</response>
    /// <response code="400">驗證失敗</response>
    [HttpPost]
    [ProducesResponseType(typeof(MessageViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageViewModel>> CreateMessage([FromBody] CreateMessageRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // 額外驗證內容長度
        var content = request.Content?.Trim() ?? string.Empty;
        if (content.Length < 1 || content.Length > 300)
        {
            ModelState.AddModelError(nameof(request.Content), "留言內容須為 1-300 字");
            return ValidationProblem(ModelState);
        }

        try
        {
            var message = await _messageService.CreateMessageAsync(content, request.Nickname?.Trim());
            
            // 透過 SignalR 廣播新留言給全域群組的所有客戶端
            await _hubContext.Clients.Group("global").ReceiveMessage(message);
            _logger.LogInformation("新留言已建立並廣播: {MessageId}", message.Id);

            return CreatedAtAction(
                nameof(GetMessage),
                new { id = message.Id },
                message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立留言時發生錯誤");
            return Problem(
                title: "建立留言失敗",
                detail: "伺服器發生錯誤，請稍後再試",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
