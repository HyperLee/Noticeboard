using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.Features;

namespace NoticeBoard.Middleware;

/// <summary>
/// 簡易速率限制中介軟體
/// </summary>
/// <remarks>
/// 使用滑動視窗演算法限制每個 IP 在指定時間內的請求數量。
/// 設定來自 appsettings.json 的 RateLimiting 區段。
/// </remarks>
/// <example>
/// 在 appsettings.json 中配置：
/// <code>
/// {
///   "RateLimiting": {
///     "Enabled": true,
///     "RequestsPerMinute": 30,
///     "BurstLimit": 10
///   }
/// }
/// </code>
/// </example>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly bool _enabled;
    private readonly int _requestsPerMinute;
    private readonly int _burstLimit;

    // IP 請求記錄：IP -> (請求時間列表)
    private static readonly ConcurrentDictionary<string, RequestTracker> _requestTrackers = new();

    /// <summary>
    /// 建立 RateLimitingMiddleware 實例
    /// </summary>
    public RateLimitingMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;

        _enabled = _configuration.GetValue("RateLimiting:Enabled", true);
        _requestsPerMinute = _configuration.GetValue("RateLimiting:RequestsPerMinute", 30);
        _burstLimit = _configuration.GetValue("RateLimiting:BurstLimit", 10);
    }

    /// <summary>
    /// 處理 HTTP 請求
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // 如果停用或非 API 請求，直接放行
        if (!_enabled || !context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var clientIp = GetClientIp(context);

        // 取得或建立該 IP 的追蹤器
        var tracker = _requestTrackers.GetOrAdd(clientIp, _ => new RequestTracker());

        // 檢查速率限制
        var now = DateTime.UtcNow;
        var (allowed, retryAfterSeconds) = tracker.TryRequest(now, _requestsPerMinute, _burstLimit);

        if (!allowed)
        {
            _logger.LogWarning(
                "速率限制觸發: IP={ClientIp}, Path={Path}",
                MaskIp(clientIp),
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc6585#section-4",
                title = "Too Many Requests",
                status = 429,
                detail = $"請求過於頻繁，請於 {retryAfterSeconds} 秒後重試。",
                retryAfter = retryAfterSeconds
            });

            return;
        }

        // 加入速率限制標頭
        context.Response.OnStarting(() =>
        {
            var remaining = tracker.GetRemainingRequests(_requestsPerMinute);
            context.Response.Headers["X-RateLimit-Limit"] = _requestsPerMinute.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = tracker.GetResetTime().ToString();
            return Task.CompletedTask;
        });

        await _next(context);

        // 定期清理過期的追蹤器
        CleanupExpiredTrackers();
    }

    /// <summary>
    /// 取得客戶端 IP 位址
    /// </summary>
    private static string GetClientIp(HttpContext context)
    {
        // 嘗試從 X-Forwarded-For 標頭取得（反向代理情境）
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ip = forwardedFor.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
            {
                return ip;
            }
        }

        // 使用連線的遠端 IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// 遮罩 IP 用於日誌記錄（隱私保護）
    /// </summary>
    private static string MaskIp(string ip)
    {
        if (string.IsNullOrEmpty(ip) || ip == "unknown")
        {
            return "***";
        }

        // IPv4: 遮罩最後一段
        if (ip.Contains('.'))
        {
            var parts = ip.Split('.');
            if (parts.Length == 4)
            {
                return $"{parts[0]}.{parts[1]}.{parts[2]}.***";
            }
        }

        // IPv6: 遮罩後半部
        if (ip.Contains(':'))
        {
            var colonIndex = ip.LastIndexOf(':');
            if (colonIndex > 0)
            {
                return $"{ip[..colonIndex]}:***";
            }
        }

        return "***";
    }

    /// <summary>
    /// 清理過期的追蹤器（超過 2 分鐘無請求）
    /// </summary>
    private static void CleanupExpiredTrackers()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-2);
        var expiredKeys = _requestTrackers
            .Where(kvp => kvp.Value.LastRequestTime < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _requestTrackers.TryRemove(key, out _);
        }
    }
}

/// <summary>
/// 請求追蹤器（滑動視窗）
/// </summary>
internal class RequestTracker
{
    private readonly object _lock = new();
    private readonly Queue<DateTime> _requests = new();
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan BurstWindow = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 最後一次請求時間
    /// </summary>
    public DateTime LastRequestTime { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// 嘗試記錄請求，回傳是否允許
    /// </summary>
    public (bool allowed, int retryAfterSeconds) TryRequest(DateTime now, int limit, int burstLimit)
    {
        lock (_lock)
        {
            LastRequestTime = now;

            // 移除過期請求
            while (_requests.Count > 0 && now - _requests.Peek() > Window)
            {
                _requests.Dequeue();
            }

            // 檢查每分鐘限制
            if (_requests.Count >= limit)
            {
                var oldestRequest = _requests.Peek();
                var retryAfter = (int)Math.Ceiling((oldestRequest + Window - now).TotalSeconds);
                return (false, Math.Max(1, retryAfter));
            }

            // 檢查爆發限制（每秒請求數）
            var recentRequests = _requests.Count(r => now - r < BurstWindow);
            if (recentRequests >= burstLimit)
            {
                return (false, 1);
            }

            _requests.Enqueue(now);
            return (true, 0);
        }
    }

    /// <summary>
    /// 取得剩餘請求數
    /// </summary>
    public int GetRemainingRequests(int limit)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var validRequests = _requests.Count(r => now - r <= Window);
            return Math.Max(0, limit - validRequests);
        }
    }

    /// <summary>
    /// 取得重設時間（Unix 時間戳）
    /// </summary>
    public long GetResetTime()
    {
        lock (_lock)
        {
            if (_requests.Count == 0)
            {
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

            var oldestRequest = _requests.Peek();
            var resetTime = oldestRequest + Window;
            return new DateTimeOffset(resetTime).ToUnixTimeSeconds();
        }
    }
}

/// <summary>
/// RateLimitingMiddleware 擴充方法
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    /// <summary>
    /// 使用速率限制中介軟體
    /// </summary>
    /// <param name="builder">應用程式建構器</param>
    /// <returns>應用程式建構器</returns>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
