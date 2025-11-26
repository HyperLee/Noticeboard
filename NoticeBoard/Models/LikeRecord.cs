namespace NoticeBoard.Models;

/// <summary>
/// 按讚記錄（伺服器端儲存）
/// </summary>
/// <remarks>
/// Token 為 RNG 產生的匿名識別碼，不含任何個人識別資訊（PII）
/// </remarks>
/// <example>
/// <code>
/// var likeRecord = new LikeRecord
/// {
///     MessageId = "550e8400-e29b-41d4-a716-446655440000",
///     Token = "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
///     Timestamp = DateTime.UtcNow
/// };
/// </code>
/// </example>
public class LikeRecord
{
    /// <summary>
    /// 留言識別碼
    /// </summary>
    public required string MessageId { get; set; }

    /// <summary>
    /// 匿名 Token（Cookie 產生的隨機識別碼）
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// 按讚時間（UTC）
    /// </summary>
    public DateTime Timestamp { get; set; }
}
