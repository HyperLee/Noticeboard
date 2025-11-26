using System.ComponentModel.DataAnnotations;

namespace NoticeBoard.Models.Requests;

/// <summary>
/// 按讚請求
/// </summary>
public class LikeRequest
{
    /// <summary>
    /// 留言識別碼
    /// </summary>
    [Required(ErrorMessage = "留言識別碼為必填")]
    public required string MessageId { get; set; }
}
