using NoticeBoard.Models;

namespace NoticeBoard.Services;

/// <summary>
/// 背景清理服務，定期清除超過保留期限的留言與按讚記錄
/// </summary>
/// <remarks>
/// 此服務實作 <see cref="BackgroundService"/>，在應用程式啟動後以固定間隔執行清理任務。
/// 清理設定（保留時間、間隔）來自 appsettings.json 的 Cleanup 區段。
/// </remarks>
/// <example>
/// 在 appsettings.json 中配置：
/// <code>
/// {
///   "Cleanup": {
///     "Enabled": true,
///     "RetentionHours": 24,
///     "IntervalMinutes": 60
///   }
/// }
/// </code>
/// </example>
public class CleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CleanupService> _logger;
    private readonly bool _enabled;
    private readonly int _retentionHours;
    private readonly TimeSpan _interval;

    /// <summary>
    /// 建立 CleanupService 實例
    /// </summary>
    /// <param name="serviceProvider">服務提供者，用於建立 Scoped 服務</param>
    /// <param name="configuration">應用程式配置</param>
    /// <param name="logger">日誌記錄器</param>
    public CleanupService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<CleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;

        _enabled = _configuration.GetValue("Cleanup:Enabled", true);
        _retentionHours = _configuration.GetValue("Cleanup:RetentionHours", 24);
        var intervalMinutes = _configuration.GetValue("Cleanup:IntervalMinutes", 60);
        _interval = TimeSpan.FromMinutes(intervalMinutes);
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("清理服務已停用");
            return;
        }

        _logger.LogInformation(
            "清理服務已啟動，保留時間: {RetentionHours} 小時，間隔: {IntervalMinutes} 分鐘",
            _retentionHours,
            _interval.TotalMinutes);

        // 初始延遲，避免在應用程式啟動時立即執行
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // 正常停止，不記錄錯誤
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理任務執行時發生錯誤");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // 正常停止
                break;
            }
        }

        _logger.LogInformation("清理服務已停止");
    }

    /// <summary>
    /// 執行清理任務
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("開始執行清理任務...");

        // 建立 Scoped 服務範圍，因為 IMessageService 是 Scoped 服務
        using var scope = _serviceProvider.CreateScope();

        var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
        var likeService = scope.ServiceProvider.GetRequiredService<ILikeService>();

        // 清理過期留言
        var messagesRemoved = await messageService.CleanupOldMessagesAsync(_retentionHours);

        // 清理過期按讚記錄
        var likesRemoved = await likeService.CleanupOldLikesAsync(_retentionHours);

        if (messagesRemoved > 0 || likesRemoved > 0)
        {
            _logger.LogInformation(
                "清理完成：移除 {MessagesCount} 則留言、{LikesCount} 筆按讚記錄",
                messagesRemoved,
                likesRemoved);
        }
        else
        {
            _logger.LogDebug("清理完成：無過期資料需要移除");
        }
    }
}
