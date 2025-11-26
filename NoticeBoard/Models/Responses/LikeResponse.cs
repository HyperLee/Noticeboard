namespace NoticeBoard.Models.Responses;

/// <summary>
/// 按讚操作回應
/// </summary>
/// <example>
/// <code>
/// var response = new LikeResponse
/// {
///     MessageId = "550e8400-e29b-41d4-a716-446655440000",
///     Liked = true,
///     LikeCount = 6
/// };
/// </code>
/// </example>
public class LikeResponse
{
    /// <summary>
    /// 留言識別碼
    /// </summary>
    public required string MessageId { get; set; }

    /// <summary>
    /// 目前按讚狀態（true = 已按讚，false = 未按讚）
    /// </summary>
    public bool Liked { get; set; }

    /// <summary>
    /// 更新後的按讚數
    /// </summary>
    public int LikeCount { get; set; }
}
