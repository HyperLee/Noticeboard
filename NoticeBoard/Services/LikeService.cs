using NoticeBoard.Models;

namespace NoticeBoard.Services;

/// <summary>
/// 按讚服務實作
/// </summary>
public class LikeService : ILikeService
{
    private readonly IJsonStorageService<LikeRecord> _storage;
    private readonly IMessageService _messageService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LikeService> _logger;
    private readonly string _likesFilePath;

    /// <summary>
    /// 建立按讚服務實例
    /// </summary>
    public LikeService(
        IJsonStorageService<LikeRecord> storage,
        IMessageService messageService,
        IConfiguration configuration,
        ILogger<LikeService> logger)
    {
        _storage = storage;
        _messageService = messageService;
        _configuration = configuration;
        _logger = logger;

        var dataDir = _configuration["Storage:DataDirectory"] ?? "Data";
        var likesFile = _configuration["Storage:LikesFile"] ?? "likes.json";
        _likesFilePath = Path.Combine(AppContext.BaseDirectory, dataDir, likesFile);
    }

    /// <inheritdoc/>
    public async Task<(bool liked, int likeCount)> ToggleLikeAsync(string messageId, string token)
    {
        var hasLiked = await HasLikedAsync(messageId, token);
        int newLikeCount;

        if (hasLiked)
        {
            // 取消按讚
            await _storage.RemoveAsync(_likesFilePath, r => r.MessageId == messageId && r.Token == token);
            newLikeCount = await GetLikeCountAsync(messageId);
            await _messageService.UpdateLikeCountAsync(messageId, newLikeCount);
            _logger.LogDebug("取消按讚: MessageId={MessageId}, Token={Token}", messageId, MaskToken(token));
            return (false, newLikeCount);
        }
        else
        {
            // 新增按讚
            var record = new LikeRecord
            {
                MessageId = messageId,
                Token = token,
                Timestamp = DateTime.UtcNow
            };
            await _storage.AddAsync(_likesFilePath, record);
            newLikeCount = await GetLikeCountAsync(messageId);
            await _messageService.UpdateLikeCountAsync(messageId, newLikeCount);
            _logger.LogDebug("新增按讚: MessageId={MessageId}, Token={Token}", messageId, MaskToken(token));
            return (true, newLikeCount);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasLikedAsync(string messageId, string token)
    {
        var records = await _storage.LoadAsync(_likesFilePath);
        return records.Any(r => r.MessageId == messageId && r.Token == token);
    }

    /// <inheritdoc/>
    public async Task<int> GetLikeCountAsync(string messageId)
    {
        var records = await _storage.LoadAsync(_likesFilePath);
        return records.Count(r => r.MessageId == messageId);
    }

    /// <inheritdoc/>
    public async Task<int> CleanupLikesForMessageAsync(string messageId)
    {
        var removedCount = await _storage.RemoveAsync(_likesFilePath, r => r.MessageId == messageId);
        if (removedCount > 0)
        {
            _logger.LogDebug("已清理留言 {MessageId} 的 {Count} 筆按讚記錄", messageId, removedCount);
        }
        return removedCount;
    }

    /// <inheritdoc/>
    public async Task<int> CleanupOldLikesAsync(int retentionHours)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-retentionHours);
        var removedCount = await _storage.RemoveAsync(_likesFilePath, r => r.Timestamp < cutoffTime);
        if (removedCount > 0)
        {
            _logger.LogInformation("已清理 {Count} 筆過期按讚記錄", removedCount);
        }
        return removedCount;
    }

    /// <summary>
    /// 遮罩 Token 用於日誌記錄（隱私保護）
    /// </summary>
    private static string MaskToken(string token)
    {
        if (string.IsNullOrEmpty(token) || token.Length < 8)
        {
            return "***";
        }
        return $"{token[..4]}...{token[^4..]}";
    }
}
