namespace NoticeBoard.Models.ViewModels;

/// <summary>
/// 前台留言顯示視圖模型
/// </summary>
public class MessageViewModel
{
    /// <summary>
    /// 留言識別碼
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// 留言內容（已過濾 XSS）
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// 顯示名稱（無暱稱時顯示「匿名」）
    /// </summary>
    public string DisplayName => Nickname ?? "匿名";

    /// <summary>
    /// 原始暱稱
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 按讚數
    /// </summary>
    public int LikeCount { get; set; }

    /// <summary>
    /// 是否已按讚（前端 localStorage 判斷）
    /// </summary>
    public bool HasLiked { get; set; }
}
