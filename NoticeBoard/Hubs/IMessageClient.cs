using NoticeBoard.Models.ViewModels;

namespace NoticeBoard.Hubs;

/// <summary>
/// SignalR 客戶端介面（強型別 Hub）
/// </summary>
public interface IMessageClient
{
    /// <summary>
    /// 接收新留言
    /// </summary>
    /// <param name="message">留言視圖模型</param>
    Task ReceiveMessage(MessageViewModel message);

    /// <summary>
    /// 留言已更新
    /// </summary>
    /// <param name="message">更新後的留言視圖模型</param>
    Task MessageUpdated(MessageViewModel message);

    /// <summary>
    /// 留言已刪除/隱藏
    /// </summary>
    /// <param name="messageId">留言識別碼</param>
    Task MessageDeleted(string messageId);

    /// <summary>
    /// 按讚數已更新
    /// </summary>
    /// <param name="messageId">留言識別碼</param>
    /// <param name="likeCount">新的按讚數</param>
    Task LikeCountUpdated(string messageId, int likeCount);
}
