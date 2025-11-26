using System.Text.RegularExpressions;
using System.Web;
using NoticeBoard.Models;
using NoticeBoard.Models.ViewModels;

namespace NoticeBoard.Services;

/// <summary>
/// 留言服務實作
/// </summary>
public partial class MessageService : IMessageService
{
    private readonly IJsonStorageService<Message> _storage;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MessageService> _logger;
    private readonly string _messagesFilePath;

    /// <summary>
    /// 建立留言服務實例
    /// </summary>
    public MessageService(
        IJsonStorageService<Message> storage,
        IConfiguration configuration,
        ILogger<MessageService> logger)
    {
        _storage = storage;
        _configuration = configuration;
        _logger = logger;

        var dataDir = _configuration["Storage:DataDirectory"] ?? "Data";
        var messagesFile = _configuration["Storage:MessagesFile"] ?? "messages.json";
        _messagesFilePath = Path.Combine(AppContext.BaseDirectory, dataDir, messagesFile);
    }

    /// <inheritdoc/>
    public async Task<List<MessageViewModel>> GetPublicMessagesAsync()
    {
        var messages = await _storage.LoadAsync(_messagesFilePath);
        return messages
            .Where(m => m.Status == MessageStatus.Public)
            .OrderByDescending(m => m.CreatedAt)
            .Select(ToViewModel)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<List<AdminMessageViewModel>> GetAllMessagesAsync(MessageStatus? status = null)
    {
        var messages = await _storage.LoadAsync(_messagesFilePath);
        var query = messages.AsEnumerable();

        if (status.HasValue)
        {
            query = query.Where(m => m.Status == status.Value);
        }

        return query
            .OrderByDescending(m => m.CreatedAt)
            .Select(ToAdminViewModel)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<MessageViewModel?> GetMessageAsync(string id)
    {
        var messages = await _storage.LoadAsync(_messagesFilePath);
        var message = messages.FirstOrDefault(m => m.Id == id && m.Status == MessageStatus.Public);
        return message is null ? null : ToViewModel(message);
    }

    /// <inheritdoc/>
    public async Task<MessageViewModel> CreateMessageAsync(string content, string? nickname)
    {
        var sanitizedContent = SanitizeContent(content);
        var sanitizedNickname = nickname is null ? null : SanitizeContent(nickname);

        var message = new Message
        {
            Id = Guid.NewGuid().ToString(),
            Content = sanitizedContent,
            Nickname = string.IsNullOrWhiteSpace(sanitizedNickname) ? null : sanitizedNickname,
            CreatedAt = DateTime.UtcNow,
            LikeCount = 0,
            Status = MessageStatus.Public
        };

        await _storage.AddAsync(_messagesFilePath, message);
        _logger.LogInformation("新留言已建立: {MessageId}", message.Id);

        return ToViewModel(message);
    }

    /// <inheritdoc/>
    public async Task<bool> HideMessageAsync(string id)
    {
        var result = await _storage.UpdateAsync(
            _messagesFilePath,
            m => m.Id == id && m.Status == MessageStatus.Public,
            m => m.Status = MessageStatus.Hidden);

        if (result)
        {
            _logger.LogInformation("留言已隱藏: {MessageId}", id);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> RestoreMessageAsync(string id)
    {
        var result = await _storage.UpdateAsync(
            _messagesFilePath,
            m => m.Id == id && m.Status == MessageStatus.Hidden,
            m => m.Status = MessageStatus.Public);

        if (result)
        {
            _logger.LogInformation("留言已恢復: {MessageId}", id);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteMessageAsync(string id)
    {
        var result = await _storage.UpdateAsync(
            _messagesFilePath,
            m => m.Id == id && m.Status != MessageStatus.Deleted,
            m => m.Status = MessageStatus.Deleted);

        if (result)
        {
            _logger.LogInformation("留言已刪除: {MessageId}", id);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateLikeCountAsync(string id, int likeCount)
    {
        return await _storage.UpdateAsync(
            _messagesFilePath,
            m => m.Id == id,
            m => m.LikeCount = likeCount);
    }

    /// <inheritdoc/>
    public async Task<int> CleanupOldMessagesAsync(int retentionHours)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-retentionHours);
        var removedCount = await _storage.RemoveAsync(
            _messagesFilePath,
            m => m.CreatedAt < cutoffTime);

        if (removedCount > 0)
        {
            _logger.LogInformation("已清理 {Count} 則過期留言", removedCount);
        }

        return removedCount;
    }

    /// <inheritdoc/>
    public string SanitizeContent(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        // XSS 防護：HTML 編碼
        var sanitized = HttpUtility.HtmlEncode(content);

        // 移除多餘空白
        sanitized = WhitespaceRegex().Replace(sanitized, " ");

        // 黑名單詞彙過濾（從 ContentModeration:BlacklistedWords 讀取）
        var blacklist = _configuration.GetSection("ContentModeration:BlacklistedWords").Get<string[]>() ?? [];
        foreach (var word in blacklist)
        {
            if (!string.IsNullOrWhiteSpace(word))
            {
                sanitized = sanitized.Replace(word, new string('*', word.Length), StringComparison.OrdinalIgnoreCase);
            }
        }

        return sanitized.Trim();
    }

    /// <summary>
    /// 轉換為前台視圖模型
    /// </summary>
    private static MessageViewModel ToViewModel(Message message)
    {
        return new MessageViewModel
        {
            Id = message.Id,
            Content = message.Content,
            Nickname = message.Nickname,
            CreatedAt = message.CreatedAt,
            LikeCount = message.LikeCount,
            HasLiked = false // 由前端判斷
        };
    }

    /// <summary>
    /// 轉換為後台視圖模型
    /// </summary>
    private static AdminMessageViewModel ToAdminViewModel(Message message)
    {
        return new AdminMessageViewModel
        {
            Id = message.Id,
            Content = message.Content,
            Nickname = message.Nickname,
            CreatedAt = message.CreatedAt,
            LikeCount = message.LikeCount,
            HasLiked = false,
            Status = message.Status
        };
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
