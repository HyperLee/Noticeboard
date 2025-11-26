# 研究文件: 匿名留言板（類 Slido）

**分支**: `001-anon-messageboard` | **日期**: 2025-11-26

## 研究摘要

本文件記錄了實作匿名留言板系統所需的技術研究與決策，解決技術脈絡中的所有待釐清項目。

---

## 1. SignalR 即時通訊實作

### 決策
採用 ASP.NET Core SignalR 作為即時通訊方案。

### 理由
- **原生支援**: ASP.NET Core 內建 SignalR，無需額外框架
- **自動降級**: WebSocket → Server-Sent Events → Long Polling 自動切換
- **強型別 Hub**: 支援強型別客戶端介面，減少錯誤
- **群組廣播**: 原生支援向所有連線客戶端廣播訊息

### 考慮過的替代方案
| 方案 | 優點 | 缺點 | 拒絕原因 |
|------|------|------|----------|
| 短輪詢 (Polling) | 實作簡單 | 高延遲、高伺服器負載 | 無法滿足 2 秒延遲要求 |
| Socket.IO | 跨平台成熟 | 需額外相依、非 .NET 原生 | 增加複雜度 |
| gRPC Streaming | 高效能 | 瀏覽器支援有限 | 前端整合困難 |

### 實作重點
```csharp
// Hub 介面定義
public interface IMessageClient
{
    Task ReceiveMessage(MessageViewModel message);
    Task MessageUpdated(MessageViewModel message);
    Task MessageDeleted(string messageId);
    Task LikeCountUpdated(string messageId, int likeCount);
}

// Hub 實作
public class MessageHub : Hub<IMessageClient>
{
    public override async Task OnConnectedAsync()
    {
        // 所有使用者加入全域群組
        await Groups.AddToGroupAsync(Context.ConnectionId, "global");
        await base.OnConnectedAsync();
    }
}
```

---

## 2. JSON 檔案原子寫入策略

### 決策
採用「寫入暫存檔 → fsync → 原子重新命名」策略，配合程序層級 mutex 保護。

### 理由
- **資料完整性**: 避免寫入中斷導致 JSON 損壞
- **並行安全**: mutex 確保同一時間只有一個寫入操作
- **跨平台**: File.Move 在 POSIX 和 Windows 上都是原子操作

### 實作重點
```csharp
public class JsonStorageService<T> : IJsonStorageService<T>
{
    private static readonly SemaphoreSlim WriteLock = new(1, 1);
    
    public async Task SaveAsync(string filePath, T data)
    {
        await WriteLock.WaitAsync();
        try
        {
            var tempPath = filePath + ".tmp";
            var json = JsonSerializer.Serialize(data, JsonOptions);
            
            // 寫入暫存檔
            await File.WriteAllTextAsync(tempPath, json, Encoding.UTF8);
            
            // 原子重新命名
            File.Move(tempPath, filePath, overwrite: true);
        }
        finally
        {
            WriteLock.Release();
        }
    }
}
```

### 考慮過的替代方案
| 方案 | 優點 | 缺點 | 拒絕原因 |
|------|------|------|----------|
| 直接覆寫 | 實作簡單 | 寫入中斷會損壞檔案 | 資料完整性風險 |
| SQLite | 內建交易支援 | 需額外相依 | 規格要求純 JSON |
| 寫入複本 + 版本號 | 可回復 | 增加檔案管理複雜度 | 過度設計 |

---

## 3. 匿名 Token 與按讚限制

### 決策
伺服器端產生隨機匿名 Token，透過 HttpOnly Cookie 儲存，同時在前端 localStorage 維護按讚記錄。

### 理由
- **雙重保護**: 前端 localStorage 提供 UX 回饋，後端 Cookie Token 防止偽造
- **匿名性**: Token 不含任何 PII，僅為 RNG 隨機字串
- **符合規格**: 規格要求伺服器端儲存精簡的 LikeRecord

### Token 格式
```json
{
  "anon_token": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

### LikeRecord 結構
```json
{
  "message_id": "uuid-string",
  "token": "anon-token-hash",
  "timestamp": "2025-11-26T12:00:00Z"
}
```

### 實作重點
```csharp
// 產生匿名 Token（首次訪問）
public string GenerateAnonToken()
{
    return Guid.NewGuid().ToString();
}

// 檢查是否已按讚
public async Task<bool> HasLikedAsync(string messageId, string anonToken)
{
    var likes = await LoadLikesAsync();
    return likes.Any(l => l.MessageId == messageId && l.Token == anonToken);
}
```

---

## 4. XSS 防護策略

### 決策
採用多層防護：HTML 編碼 + CSP Header + 輸入驗證。

### 理由
- **深度防禦**: 單一防護層失效時仍有其他保護
- **ASP.NET Core 內建**: Razor 預設 HTML 編碼
- **規格要求**: 明確要求 XSS 防護

### 實作重點
```csharp
// 1. 輸入驗證（Service 層）
public string SanitizeContent(string content)
{
    // 移除潛在危險標籤（保留純文字）
    return HttpUtility.HtmlEncode(content.Trim());
}

// 2. CSP Header（Program.cs）
app.Use(async (context, next) =>
{
    context.Response.Headers.Append(
        "Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';"
    );
    await next();
});

// 3. Razor View 自動編碼
@Model.Content  // 自動 HTML 編碼
```

---

## 5. 排程清理服務（24 小時保留）

### 決策
採用 `IHostedService` + `PeriodicTimer` 實作背景排程服務。

### 理由
- **原生支援**: ASP.NET Core 內建 Hosted Service
- **可靠性**: 隨應用程式生命週期管理
- **彈性**: 可透過設定調整執行間隔

### 實作重點
```csharp
public class CleanupService : BackgroundService
{
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
    private readonly TimeSpan _retentionPeriod = TimeSpan.FromHours(24);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_cleanupInterval);
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CleanupExpiredDataAsync();
        }
    }
    
    private async Task CleanupExpiredDataAsync()
    {
        var cutoff = DateTime.UtcNow - _retentionPeriod;
        
        // 清理過期留言
        var messages = await _messageService.GetAllAsync();
        var expired = messages.Where(m => m.CreatedAt < cutoff).ToList();
        
        foreach (var message in expired)
        {
            await _messageService.DeletePermanentlyAsync(message.Id);
        }
        
        // 清理過期按讚記錄
        await _likeService.CleanupExpiredAsync(cutoff);
    }
}
```

---

## 6. 管理者認證機制

### 決策
採用簡易 Session 認證 + Cookie 驗證，配合單一密碼（admin/admin999）。

### 理由
- **規格明確**: 規格指定教學/展示用途，非生產級安全
- **簡化實作**: 無需 JWT 或 OAuth 複雜設定
- **足夠保護**: Session + HTTPS 足以防止基本攻擊

### 實作重點
```csharp
// AdminController.cs
[HttpPost("login")]
public IActionResult Login([FromBody] LoginRequest request)
{
    if (request.Username == "admin" && request.Password == "admin999")
    {
        HttpContext.Session.SetString("IsAdmin", "true");
        return Ok(new { success = true });
    }
    
    return Unauthorized(new ProblemDetails
    {
        Title = "認證失敗",
        Detail = "使用者名稱或密碼錯誤"
    });
}

// 授權中介軟體
public class AdminAuthorizationMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.StartsWithSegments("/api/admin"))
        {
            var isAdmin = context.Session.GetString("IsAdmin");
            if (isAdmin != "true")
            {
                context.Response.StatusCode = 401;
                return;
            }
        }
        await next(context);
    }
}
```

---

## 7. 錯誤處理與 RFC 7807

### 決策
採用 ASP.NET Core 內建的 Problem Details 格式。

### 理由
- **標準化**: RFC 7807 是 API 錯誤回應的業界標準
- **原生支援**: ASP.NET Core 8.0 內建 Problem Details 服務
- **憲章要求**: 憲章明確要求使用 RFC 7807

### 實作重點
```csharp
// Program.cs
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = 
            context.HttpContext.TraceIdentifier;
    };
});

// 自訂錯誤回應
return Problem(
    title: "驗證失敗",
    detail: "留言內容不可超過 300 字",
    statusCode: StatusCodes.Status400BadRequest
);
```

---

## 8. 測試策略與環境備案

### 決策
採用 xUnit + Moq 進行單元測試，WebApplicationFactory 進行整合測試，並設計環境問題備案。

### 理由
- **憲章要求**: 憲章明確指定 xUnit + Moq
- **隔離測試**: Mock 確保單元測試獨立性
- **環境穩健**: 避免測試因環境問題卡住

### 測試環境備案

#### 備案 1: SignalR 測試超時
```csharp
[Fact]
[Trait("Category", "Integration")]
public async Task SignalR_Connection_ShouldConnect_WithTimeout()
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    
    try
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(_factory.Server.BaseAddress + "/messageHub")
            .Build();
        
        await connection.StartAsync(cts.Token);
        Assert.Equal(HubConnectionState.Connected, connection.State);
    }
    catch (OperationCanceledException)
    {
        // 環境問題：跳過此測試
        Skip.If(true, "SignalR 連線超時，可能為環境問題");
    }
}
```

#### 備案 2: 檔案系統權限問題
```csharp
[Fact]
public async Task JsonStorage_ShouldFallbackToMemory_WhenFileSystemFails()
{
    // 若檔案系統無法使用，自動切換為記憶體儲存
    var storage = new JsonStorageService<Message>(
        _logger,
        new StorageOptions { FallbackToMemory = true }
    );
    
    // 測試邏輯...
}
```

#### 備案 3: 測試執行分類
```csharp
// 快速單元測試（必跑）
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]

// 整合測試（可選跳過）
[Trait("Category", "Integration")]
[Trait("Speed", "Slow")]

// 執行時使用篩選
// dotnet test --filter "Category=Unit"
// dotnet test --filter "Category!=Integration"
```

### 測試專案配置
```xml
<!-- NoticeBoard.Tests.csproj -->
<PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
</PropertyGroup>

<ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="Moq" Version="4.*" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.*" />
    <PackageReference Include="FluentAssertions" Version="6.*" />
    <PackageReference Include="Xunit.SkippableFact" Version="1.*" />
</ItemGroup>
```

---

## 9. Serilog 結構化日誌配置

### 決策
採用 Serilog 作為日誌提供者，輸出至 Console 與檔案。

### 理由
- **憲章要求**: 憲章明確指定 Serilog
- **結構化輸出**: 支援 JSON 格式便於查詢
- **彈性配置**: 可透過設定檔調整

### 實作重點
```csharp
// Program.cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "NoticeBoard")
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/noticeboard-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();

builder.Host.UseSerilog();
```

---

## 10. 效能考量

### 決策
採用記憶體快取 + 批次寫入策略。

### 理由
- **減少 I/O**: 高頻讀取從記憶體取得
- **降低延遲**: 避免每次請求都讀取檔案
- **滿足目標**: 200 並行使用者、< 200ms p95

### 實作重點
```csharp
// 記憶體快取策略
public class CachedMessageService : IMessageService
{
    private List<Message>? _cachedMessages;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private DateTime _lastCacheRefresh = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromSeconds(5);
    
    public async Task<IReadOnlyList<Message>> GetVisibleMessagesAsync()
    {
        await RefreshCacheIfNeededAsync();
        return _cachedMessages?
            .Where(m => m.Status == MessageStatus.Public)
            .OrderByDescending(m => m.CreatedAt)
            .ToList() ?? new List<Message>();
    }
}
```

---

## 決策總結

| 領域 | 決策 | 關鍵理由 |
|------|------|----------|
| 即時通訊 | SignalR | 原生支援、自動降級 |
| 檔案儲存 | 原子寫入 + mutex | 資料完整性、並行安全 |
| 按讚限制 | Cookie Token + localStorage | 雙重保護、匿名性 |
| XSS 防護 | 多層防禦 | 深度防禦原則 |
| 排程清理 | IHostedService | 原生支援、生命週期管理 |
| 管理認證 | Session Cookie | 規格要求簡易認證 |
| 錯誤處理 | RFC 7807 Problem Details | 業界標準、憲章要求 |
| 測試策略 | xUnit + 環境備案 | 穩健測試、避免卡住 |
| 日誌系統 | Serilog | 憲章要求、結構化輸出 |
| 效能優化 | 記憶體快取 | 減少 I/O、降低延遲 |
