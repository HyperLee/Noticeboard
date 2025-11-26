namespace NoticeBoard.Models.Responses;

/// <summary>
/// 清理作業回應
/// </summary>
public class CleanupResponse
{
    /// <summary>
    /// 刪除的留言數量
    /// </summary>
    public int MessagesDeleted { get; set; }

    /// <summary>
    /// 刪除的按讚記錄數量
    /// </summary>
    public int LikesDeleted { get; set; }
}
