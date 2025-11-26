using Microsoft.AspNetCore.SignalR;
using NoticeBoard.Models.ViewModels;

namespace NoticeBoard.Hubs;

/// <summary>
/// SignalR 留言 Hub
/// </summary>
public class MessageHub : Hub<IMessageClient>
{
    private const string GlobalGroup = "global";
    private readonly ILogger<MessageHub> _logger;

    /// <summary>
    /// 建立 MessageHub 實例
    /// </summary>
    public MessageHub(ILogger<MessageHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 連線建立時
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        // 所有使用者加入全域群組
        await Groups.AddToGroupAsync(Context.ConnectionId, GlobalGroup);
        _logger.LogDebug("客戶端已連線: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 連線中斷時
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("客戶端已斷線: {ConnectionId}, 原因: {Exception}", 
            Context.ConnectionId, exception?.Message ?? "正常斷線");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 廣播新留言給所有客戶端
    /// </summary>
    /// <param name="message">留言視圖模型</param>
    public async Task BroadcastNewMessage(MessageViewModel message)
    {
        await Clients.Group(GlobalGroup).ReceiveMessage(message);
        _logger.LogDebug("已廣播新留言: {MessageId}", message.Id);
    }

    /// <summary>
    /// 廣播留言更新給所有客戶端
    /// </summary>
    /// <param name="message">更新後的留言視圖模型</param>
    public async Task BroadcastMessageUpdated(MessageViewModel message)
    {
        await Clients.Group(GlobalGroup).MessageUpdated(message);
        _logger.LogDebug("已廣播留言更新: {MessageId}", message.Id);
    }

    /// <summary>
    /// 廣播留言刪除/隱藏給所有客戶端
    /// </summary>
    /// <param name="messageId">留言識別碼</param>
    public async Task BroadcastMessageDeleted(string messageId)
    {
        await Clients.Group(GlobalGroup).MessageDeleted(messageId);
        _logger.LogDebug("已廣播留言刪除: {MessageId}", messageId);
    }

    /// <summary>
    /// 廣播按讚數更新給所有客戶端
    /// </summary>
    /// <param name="messageId">留言識別碼</param>
    /// <param name="likeCount">新的按讚數</param>
    public async Task BroadcastLikeCountUpdated(string messageId, int likeCount)
    {
        await Clients.Group(GlobalGroup).LikeCountUpdated(messageId, likeCount);
        _logger.LogDebug("已廣播按讚數更新: {MessageId} = {LikeCount}", messageId, likeCount);
    }
}
