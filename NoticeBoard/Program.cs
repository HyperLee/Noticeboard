using NoticeBoard.Hubs;
using NoticeBoard.Middleware;
using NoticeBoard.Models;
using NoticeBoard.Services;
using Serilog;

namespace NoticeBoard;

public class Program
{
    public static void Main(string[] args)
    {
        // 配置 Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .Build())
            .CreateLogger();

        try
        {
            Log.Information("正在啟動應用程式...");

            var builder = WebApplication.CreateBuilder(args);

            // 使用 Serilog
            builder.Host.UseSerilog();

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // 配置 Problem Details (RFC 7807)
            builder.Services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    // 加入追蹤識別碼
                    context.ProblemDetails.Instance = $"urn:noticeboard:error:{context.HttpContext.TraceIdentifier}";

                    // 加入時間戳記
                    context.ProblemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("o");

                    // 確保 type 欄位有值
                    if (string.IsNullOrEmpty(context.ProblemDetails.Type))
                    {
                        context.ProblemDetails.Type = context.ProblemDetails.Status switch
                        {
                            400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                            401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
                            403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                            404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                            405 => "https://tools.ietf.org/html/rfc7231#section-6.5.5",
                            409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                            422 => "https://tools.ietf.org/html/rfc4918#section-11.2",
                            429 => "https://tools.ietf.org/html/rfc6585#section-4",
                            500 => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                        };
                    }
                };
            });

            // 配置 Session
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                var sessionTimeout = builder.Configuration.GetValue<int>("Admin:SessionTimeoutMinutes", 30);
                options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeout);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            // 配置 SignalR
            builder.Services.AddSignalR();

            // 註冊 JSON 儲存服務
            builder.Services.AddSingleton(typeof(IJsonStorageService<>), typeof(JsonStorageService<>));

            // 註冊應用服務
            builder.Services.AddScoped<IMessageService, MessageService>();
            builder.Services.AddScoped<ILikeService, LikeService>();

            // 註冊背景清理服務
            builder.Services.AddHostedService<CleanupService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // API 例外處理（回傳 RFC 7807 Problem Details）
            app.UseExceptionHandler(exceptionApp =>
            {
                exceptionApp.Run(async context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        context.Response.ContentType = "application/problem+json";

                        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                        {
                            Status = StatusCodes.Status500InternalServerError,
                            Title = "Internal Server Error",
                            Detail = "發生內部錯誤，請稍後再試。",
                            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                            Instance = $"urn:noticeboard:error:{context.TraceIdentifier}"
                        };
                        problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("o");

                        await context.Response.WriteAsJsonAsync(problemDetails);
                    }
                });
            });

            // 啟用 Status Code Pages（回傳 Problem Details）
            app.UseStatusCodePages(async context =>
            {
                if (context.HttpContext.Request.Path.StartsWithSegments("/api"))
                {
                    context.HttpContext.Response.ContentType = "application/problem+json";

                    var statusCode = context.HttpContext.Response.StatusCode;
                    var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Status = statusCode,
                        Title = statusCode switch
                        {
                            400 => "Bad Request",
                            401 => "Unauthorized",
                            403 => "Forbidden",
                            404 => "Not Found",
                            405 => "Method Not Allowed",
                            _ => "Error"
                        },
                        Type = statusCode switch
                        {
                            400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                            401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
                            403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                            404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                            405 => "https://tools.ietf.org/html/rfc7231#section-6.5.5",
                            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                        },
                        Instance = $"urn:noticeboard:error:{context.HttpContext.TraceIdentifier}"
                    };
                    problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("o");

                    await context.HttpContext.Response.WriteAsJsonAsync(problemDetails);
                }
            });

            // CSP Header 中介軟體
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append("Content-Security-Policy",
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdnjs.cloudflare.com; " +
                    "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; " +
                    "font-src 'self' https://cdnjs.cloudflare.com; " +
                    "img-src 'self' data:; " +
                    "connect-src 'self' ws: wss:;");
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                await next();
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // 速率限制中介軟體
            app.UseRateLimiting();

            // 啟用 Session
            app.UseSession();

            // 管理者授權中介軟體（檢查 /Admin/* MVC 路由）
            app.UseAdminAuthorization();

            app.UseAuthorization();

            // 配置 SignalR Hub 端點
            app.MapHub<MessageHub>("/messageHub");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "應用程式啟動失敗");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
