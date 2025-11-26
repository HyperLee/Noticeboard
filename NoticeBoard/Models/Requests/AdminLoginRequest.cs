using System.ComponentModel.DataAnnotations;

namespace NoticeBoard.Models.Requests;

/// <summary>
/// 管理者登入請求
/// </summary>
public class AdminLoginRequest
{
    /// <summary>
    /// 使用者名稱
    /// </summary>
    [Required(ErrorMessage = "使用者名稱為必填")]
    public required string Username { get; set; }

    /// <summary>
    /// 密碼
    /// </summary>
    [Required(ErrorMessage = "密碼為必填")]
    public required string Password { get; set; }
}
