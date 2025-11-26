using NoticeBoard.Models;

namespace NoticeBoard.Services;

/// <summary>
/// 按讚服務介面
/// </summary>
public interface ILikeService
{
    /// <summary>
    /// 切換按讚狀態
    /// </summary>
    /// <param name="messageId">留言識別碼</param>
    /// <param name="token">匿名 Token</param>
    /// <returns>按讚結果（是否已按讚、新的按讚數）</returns>
    Task<(bool liked, int likeCount)> ToggleLikeAsync(string messageId, string token);

    /// <summary>
    /// 檢查是否已按讚
    /// </summary>
    /// <param name="messageId">留言識別碼</param>
    /// <param name="token">匿名 Token</param>
    /// <returns>是否已按讚</returns>
    Task<bool> HasLikedAsync(string messageId, string token);

    /// <summary>
    /// 取得留言的按讚數
    /// </summary>
    /// <param name="messageId">留言識別碼</param>
    /// <returns>按讚數</returns>
    Task<int> GetLikeCountAsync(string messageId);

    /// <summary>
    /// 清理與指定留言相關的按讚記錄
    /// </summary>
    /// <param name="messageId">留言識別碼</param>
    /// <returns>清理的記錄數量</returns>
    Task<int> CleanupLikesForMessageAsync(string messageId);

    /// <summary>
    /// 清理過期的按讚記錄
    /// </summary>
    /// <param name="retentionHours">保留時數</param>
    /// <returns>清理的記錄數量</returns>
    Task<int> CleanupOldLikesAsync(int retentionHours);
}
