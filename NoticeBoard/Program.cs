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
            builder.Services.AddProblemDetails();

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

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

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
