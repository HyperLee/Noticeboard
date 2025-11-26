using NoticeBoard.Models;
using NoticeBoard.Models.ViewModels;

namespace NoticeBoard.Services;

/// <summary>
/// 留言服務介面
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// 取得所有公開留言（最新在上）
    /// </summary>
    /// <returns>公開留言列表</returns>
    Task<List<MessageViewModel>> GetPublicMessagesAsync();

    /// <summary>
    /// 取得所有留言（含隱藏/刪除，供管理者使用）
    /// </summary>
    /// <param name="status">篩選狀態（可選）</param>
    /// <returns>所有留言列表</returns>
    Task<List<AdminMessageViewModel>> GetAllMessagesAsync(MessageStatus? status = null);

    /// <summary>
    /// 取得單則留言
    /// </summary>
    /// <param name="id">留言識別碼</param>
    /// <returns>留言視圖模型，若不存在則返回 null</returns>
    Task<MessageViewModel?> GetMessageAsync(string id);

    /// <summary>
    /// 建立新留言
    /// </summary>
    /// <param name="content">留言內容</param>
    /// <param name="nickname">暱稱（可選）</param>
    /// <returns>建立的留言視圖模型</returns>
    Task<MessageViewModel> CreateMessageAsync(string content, string? nickname);

    /// <summary>
    /// 隱藏留言
    /// </summary>
    /// <param name="id">留言識別碼</param>
    /// <returns>是否成功</returns>
    Task<bool> HideMessageAsync(string id);

    /// <summary>
    /// 恢復留言
    /// </summary>
    /// <param name="id">留言識別碼</param>
    /// <returns>是否成功</returns>
    Task<bool> RestoreMessageAsync(string id);

    /// <summary>
    /// 刪除留言（永久）
    /// </summary>
    /// <param name="id">留言識別碼</param>
    /// <returns>是否成功</returns>
    Task<bool> DeleteMessageAsync(string id);

    /// <summary>
    /// 更新留言的按讚數
    /// </summary>
    /// <param name="id">留言識別碼</param>
    /// <param name="likeCount">新的按讚數</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateLikeCountAsync(string id, int likeCount);

    /// <summary>
    /// 清理過期留言
    /// </summary>
    /// <param name="retentionHours">保留時數</param>
    /// <returns>清理的留言數量</returns>
    Task<int> CleanupOldMessagesAsync(int retentionHours);

    /// <summary>
    /// 內容過濾與淨化
    /// </summary>
    /// <param name="content">原始內容</param>
    /// <returns>淨化後的內容</returns>
    string SanitizeContent(string content);
}
