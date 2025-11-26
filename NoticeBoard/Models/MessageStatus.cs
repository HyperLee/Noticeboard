namespace NoticeBoard.Models;

/// <summary>
/// 留言狀態列舉
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// 公開可見
    /// </summary>
    Public = 0,

    /// <summary>
    /// 隱藏（管理者可恢復）
    /// </summary>
    Hidden = 1,

    /// <summary>
    /// 已刪除（不可恢復）
    /// </summary>
    Deleted = 2
}
