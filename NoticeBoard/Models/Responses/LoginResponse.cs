namespace NoticeBoard.Models.Responses;

/// <summary>
/// 管理者登入回應
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 回應訊息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 登入成功後的重新導向 URL
    /// </summary>
    public string? RedirectUrl { get; set; }
}
