namespace NoticeBoard.Models;

/// <summary>
/// 代表一則匿名留言
/// </summary>
/// <example>
/// <code>
/// var message = new Message
/// {
///     Id = Guid.NewGuid().ToString(),
///     Content = "這是一則測試留言",
///     Nickname = "訪客",
///     CreatedAt = DateTime.UtcNow,
///     LikeCount = 0,
///     Status = MessageStatus.Public
/// };
/// </code>
/// </example>
public class Message
{
    /// <summary>
    /// 唯一識別碼（UUID v4 字串）
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// 留言內容（1-300 字）
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// 選擇性暱稱（null 顯示為「匿名」）
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// 建立時間（UTC）
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 按讚數
    /// </summary>
    public int LikeCount { get; set; }

    /// <summary>
    /// 留言狀態
    /// </summary>
    public MessageStatus Status { get; set; }
}
