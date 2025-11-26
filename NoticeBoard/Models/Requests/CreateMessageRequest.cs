using System.ComponentModel.DataAnnotations;

namespace NoticeBoard.Models.Requests;

/// <summary>
/// 建立留言請求
/// </summary>
public class CreateMessageRequest
{
    /// <summary>
    /// 留言內容（1-300 字）
    /// </summary>
    [Required(ErrorMessage = "留言內容為必填")]
    [StringLength(300, MinimumLength = 1, ErrorMessage = "留言內容須為 1-300 字")]
    public required string Content { get; set; }

    /// <summary>
    /// 選擇性暱稱
    /// </summary>
    [StringLength(50, ErrorMessage = "暱稱不可超過 50 字")]
    public string? Nickname { get; set; }
}
