namespace NoticeBoard.Models.Responses;

/// <summary>
/// 按讚狀態查詢回應
/// </summary>
/// <example>
/// <code>
/// var response = new LikeStatusResponse
/// {
///     MessageId = "550e8400-e29b-41d4-a716-446655440000",
///     Liked = true
/// };
/// </code>
/// </example>
public class LikeStatusResponse
{
    /// <summary>
    /// 留言識別碼
    /// </summary>
    public required string MessageId { get; set; }

    /// <summary>
    /// 是否已按讚
    /// </summary>
    public bool Liked { get; set; }
}
