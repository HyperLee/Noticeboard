namespace NoticeBoard.Models.ViewModels;

/// <summary>
/// 後台管理留言視圖模型
/// </summary>
public class AdminMessageViewModel : MessageViewModel
{
    /// <summary>
    /// 留言狀態
    /// </summary>
    public MessageStatus Status { get; set; }

    /// <summary>
    /// 狀態顯示文字
    /// </summary>
    public string StatusText => Status switch
    {
        MessageStatus.Public => "公開",
        MessageStatus.Hidden => "隱藏",
        MessageStatus.Deleted => "已刪除",
        _ => "未知"
    };

    /// <summary>
    /// 是否可隱藏
    /// </summary>
    public bool CanHide => Status == MessageStatus.Public;

    /// <summary>
    /// 是否可恢復
    /// </summary>
    public bool CanRestore => Status == MessageStatus.Hidden;

    /// <summary>
    /// 是否可刪除
    /// </summary>
    public bool CanDelete => Status != MessageStatus.Deleted;
}
