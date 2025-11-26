namespace NoticeBoard.Middleware;

/// <summary>
/// 管理者授權中介軟體
/// </summary>
/// <remarks>
/// 此中介軟體檢查對 /Admin/* MVC 路由的請求是否已通過管理者認證。
/// 若未認證，將重新導向至登入頁面。
/// 注意：此中介軟體不影響 /api/admin/* 端點，API 端點由控制器自行處理認證。
/// </remarks>
public class AdminAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AdminAuthorizationMiddleware> _logger;

    private const string AdminSessionKey = "IsAdmin";
    private const string AdminBasePath = "/admin";
    private const string AdminLoginPath = "/admin/login";
    private const string ApiAdminPath = "/api/admin";

    /// <summary>
    /// 建立管理者授權中介軟體
    /// </summary>
    public AdminAuthorizationMiddleware(RequestDelegate next, ILogger<AdminAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 處理 HTTP 請求
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // 不處理 API 端點（由控制器自行處理認證）
        if (path.StartsWith(ApiAdminPath, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // 檢查是否為管理後台路由（但非登入頁面）
        if (path.StartsWith(AdminBasePath, StringComparison.OrdinalIgnoreCase) &&
            !path.Equals(AdminLoginPath, StringComparison.OrdinalIgnoreCase))
        {
            var isAdmin = context.Session.GetString(AdminSessionKey);

            if (isAdmin != "true")
            {
                _logger.LogDebug("未認證的管理後台存取嘗試，重新導向至登入頁面");
                context.Response.Redirect(AdminLoginPath);
                return;
            }
        }

        await _next(context);
    }
}

/// <summary>
/// 管理者授權中介軟體擴充方法
/// </summary>
public static class AdminAuthorizationMiddlewareExtensions
{
    /// <summary>
    /// 使用管理者授權中介軟體
    /// </summary>
    public static IApplicationBuilder UseAdminAuthorization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AdminAuthorizationMiddleware>();
    }
}
