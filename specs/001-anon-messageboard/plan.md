# 實作計畫: 匿名留言板（類 Slido）

**分支**: `001-anon-messageboard` | **日期**: 2025-11-26 | **規格**: [spec.md](./spec.md)  
**輸入**: 功能規格來自 `/specs/001-anon-messageboard/spec.md`

## 摘要

建立一個匿名、免登入的即時留言與按讚系統，包含：
- **前台功能**: 匿名留言（1-300 字）、查看留言（最新在上）、按讚（localStorage 限制）
- **後台功能**: 管理者登入（admin/admin999）、隱藏/刪除/恢復留言
- **技術方案**: ASP.NET Core 8.0 MVC + SignalR 即時推送 + JSON 檔案儲存 + Serilog 日誌

## 技術脈絡

**語言/版本**: C# 13 / .NET 8.0  
**主要相依**: ASP.NET Core MVC, SignalR, Serilog  
**儲存**: JSON 檔案（messages.json, likes.json）- 無外部資料庫  
**測試**: xUnit + Moq（單元測試）+ WebApplicationFactory（整合測試）  
**目標平台**: 跨平台 Web 應用程式（Linux/Windows/macOS）  
**專案類型**: Web MVC 應用程式（單一專案結構）  
**效能目標**: 200 名同時使用者、p95 回應時間 < 200ms、即時訊息延遲 < 2 秒  
**限制條件**: 無外部 DB、資料保留 24 小時、不儲存 PII  
**規模範圍**: 單一全域房間、簡易管理後台、教學/展示用途

## 憲章合規檢查

*閘門: 必須在 Phase 0 研究前通過。Phase 1 設計後重新檢查。*

### 初次檢查（Phase 0 前）

| 原則 | 狀態 | 說明 |
|------|------|------|
| I. 程式碼品質至上 | ✅ 通過 | 使用 C# 13、檔案範圍命名空間、XML 文件註解 |
| II. 測試優先開發 | ✅ 通過 | xUnit 單元測試 + 整合測試，測試先行 |
| III. 使用者體驗一致性 | ✅ 通過 | Bootstrap 5 回應式設計、RFC 7807 錯誤格式 |
| IV. 效能與延展性 | ✅ 通過 | async/await 模式、SignalR 即時推送 |
| V. 可觀察性與監控 | ✅ 通過 | Serilog 結構化日誌 |
| VI. 安全優先 | ✅ 通過 | XSS 防護、輸入驗證、Session 認證 |

### 設計後重新檢查（Phase 1 後）

| 原則 | 狀態 | 設計驗證 |
|------|------|----------|
| I. 程式碼品質至上 | ✅ 通過 | data-model.md 包含完整 XML 文件註解範例 |
| II. 測試優先開發 | ✅ 通過 | research.md 定義測試策略與環境備案 |
| III. 使用者體驗一致性 | ✅ 通過 | api-spec.yaml 使用 RFC 7807 Problem Details |
| IV. 效能與延展性 | ✅ 通過 | research.md 定義記憶體快取策略 |
| V. 可觀察性與監控 | ✅ 通過 | research.md 定義 Serilog 配置 |
| VI. 安全優先 | ✅ 通過 | research.md 定義 XSS 多層防護策略 |


## 專案結構

### 文件結構（此功能）

```text
specs/001-anon-messageboard/
├── plan.md              # 此文件（/speckit.plan 指令輸出）
├── research.md          # Phase 0 輸出
├── data-model.md        # Phase 1 輸出
├── quickstart.md        # Phase 1 輸出
├── contracts/           # Phase 1 輸出（API 契約）
│   └── api-spec.yaml    # OpenAPI 規格
└── tasks.md             # Phase 2 輸出（/speckit.tasks 指令 - 非本指令建立）
```

### 原始碼結構（專案根目錄）

```text
NoticeBoard/
├── Controllers/
│   ├── HomeController.cs          # 前台首頁
│   └── Api/
│       ├── MessagesController.cs  # 留言 API
│       ├── LikesController.cs     # 按讚 API
│       └── AdminController.cs     # 管理後台 API
├── Hubs/
│   └── MessageHub.cs              # SignalR Hub（即時推送）
├── Models/
│   ├── Message.cs                 # 留言實體
│   ├── LikeRecord.cs              # 按讚記錄
│   ├── MessageStatus.cs           # 留言狀態列舉
│   └── ViewModels/
│       ├── MessageViewModel.cs    # 前台留言展示
│       └── AdminViewModel.cs      # 後台管理展示
├── Services/
│   ├── IMessageService.cs         # 留言服務介面
│   ├── MessageService.cs          # 留言服務實作
│   ├── ILikeService.cs            # 按讚服務介面
│   ├── LikeService.cs             # 按讚服務實作
│   ├── IJsonStorageService.cs     # JSON 儲存介面
│   ├── JsonStorageService.cs      # JSON 儲存實作（原子寫入 + mutex）
│   └── CleanupService.cs          # 24 小時資料清理服務
├── Data/
│   ├── messages.json              # 留言資料
│   └── likes.json                 # 按讚記錄
├── Views/
│   ├── Home/
│   │   └── Index.cshtml           # 前台留言板頁面
│   └── Admin/
│       └── Index.cshtml           # 後台管理頁面
├── wwwroot/
│   ├── css/
│   │   └── site.css
│   └── js/
│       └── site.js                # 前端 SignalR 連接 + 互動邏輯
├── Program.cs
├── appsettings.json
└── appsettings.Development.json

tests/NoticeBoard.Tests/
├── Unit/
│   ├── Services/
│   │   ├── MessageServiceTests.cs
│   │   ├── LikeServiceTests.cs
│   │   └── JsonStorageServiceTests.cs
│   └── Controllers/
│       └── MessagesControllerTests.cs
└── Integration/
    ├── ApiTests/
    │   ├── MessagesApiTests.cs
    │   ├── LikesApiTests.cs
    │   └── AdminApiTests.cs
    └── HubTests/
        └── MessageHubTests.cs
```

**結構決策**: 採用 ASP.NET Core MVC 單一專案結構，Services 層處理業務邏輯，Controllers/Api 處理 HTTP 請求，Hubs 處理 SignalR 即時通訊。JSON 檔案儲存於 Data 資料夾，測試專案獨立於 tests/ 資料夾。

## 複雜度追蹤

> **僅在憲章檢查有需要說明的違規時填寫**

| 違規項目 | 為何需要 | 拒絕更簡單替代方案的原因 |
|----------|----------|--------------------------|
| 無 | - | - |
