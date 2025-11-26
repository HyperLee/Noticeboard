# Tasks: 匿名留言板（類 Slido）

**Input**: Design documents from `/specs/001-anon-messageboard/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: 依憲章「測試優先開發」原則，測試程式碼應隨各實作任務一併完成（紅-綠-重構週期）。測試檔案位於 `tests/NoticeBoard.Tests/`，包含單元測試與整合測試。

**Organization**: 任務依 User Story 分組，每個 Story 可獨立實作與測試。

---

## Format: `[ID] [Story?] Description`

- **[Story]**: 所屬 User Story（例如 US1、US2、US3、US4）
- 所有路徑皆包含確切檔案位置

## Path Conventions

- **主專案**: `NoticeBoard/`（ASP.NET Core MVC）
- **測試專案**: `tests/NoticeBoard.Tests/`
- **資料儲存**: `NoticeBoard/Data/`

---

## Phase 1: Setup（專案初始化）

**Purpose**: 專案基礎設定與相依套件配置

- [X] T001 更新 `NoticeBoard/NoticeBoard.csproj` 加入 SignalR、Serilog 相依套件
- [X] T002 配置 `NoticeBoard/appsettings.json` 加入 Storage、Cleanup、Admin 設定區塊
- [X] T003 配置 `NoticeBoard/appsettings.Development.json` 開發環境設定
- [X] T004 建立資料夾結構 `NoticeBoard/Models/Requests/`、`NoticeBoard/Models/ViewModels/`

---

## Phase 2: Foundational（基礎建設 - 阻塞性先決條件）

**Purpose**: 所有 User Story 開始前必須完成的核心基礎設施

**⚠️ 重要**: 此階段完成前，無法開始任何 User Story 實作

### 資料模型

- [X] T005 建立 `MessageStatus` 列舉在 `NoticeBoard/Models/MessageStatus.cs`
- [X] T006 建立 `Message` 實體模型在 `NoticeBoard/Models/Message.cs`
- [X] T007 建立 `LikeRecord` 實體模型在 `NoticeBoard/Models/LikeRecord.cs`

### 視圖模型

- [X] T008 建立 `MessageViewModel` 在 `NoticeBoard/Models/ViewModels/MessageViewModel.cs`
- [X] T009 建立 `AdminMessageViewModel` 在 `NoticeBoard/Models/ViewModels/AdminMessageViewModel.cs`

### 請求模型

- [X] T010 建立 `CreateMessageRequest` 在 `NoticeBoard/Models/Requests/CreateMessageRequest.cs`
- [X] T011 建立 `LikeRequest` 在 `NoticeBoard/Models/Requests/LikeRequest.cs`
- [X] T012 建立 `AdminLoginRequest` 在 `NoticeBoard/Models/Requests/AdminLoginRequest.cs`

### JSON 儲存服務（核心基礎設施）

- [X] T013 建立 `IJsonStorageService<T>` 介面在 `NoticeBoard/Services/IJsonStorageService.cs`
- [X] T014 實作 `JsonStorageService<T>` 在 `NoticeBoard/Services/JsonStorageService.cs`（原子寫入 + mutex）

### 留言服務（核心基礎設施）

- [X] T015 建立 `IMessageService` 介面在 `NoticeBoard/Services/IMessageService.cs`
- [X] T016 實作 `MessageService` 在 `NoticeBoard/Services/MessageService.cs`

### 按讚服務（核心基礎設施）

- [X] T017 建立 `ILikeService` 介面在 `NoticeBoard/Services/ILikeService.cs`
- [X] T018 實作 `LikeService` 在 `NoticeBoard/Services/LikeService.cs`

### SignalR Hub（即時通訊基礎設施）

- [X] T019 建立 `IMessageClient` 介面在 `NoticeBoard/Hubs/IMessageClient.cs`
- [X] T020 實作 `MessageHub` 在 `NoticeBoard/Hubs/MessageHub.cs`

### 程式進入點配置

- [X] T021 更新 `NoticeBoard/Program.cs` 配置 DI、Session、SignalR、Serilog、CSP Header

**Checkpoint**: 基礎設施就緒 - User Story 實作可以開始

---

## Phase 3: User Story 1 - 發表留言（Priority: P1）🎯 MVP

**Goal**: 匿名使用者可在前台輸入文字並送出留言，系統接受合法內容並立即顯示在留言列表

**Independent Test**: 使用 UI 表單或 HTTP POST 送出留言，驗證資料寫入後透過 SignalR 即時顯示

### Implementation for User Story 1

- [ ] T022 [US1] 建立 `MessagesController` 在 `NoticeBoard/Controllers/Api/MessagesController.cs`
- [ ] T023 [US1] 實作 `POST /api/messages` 端點（建立留言、觸發 SignalR 廣播）
- [ ] T024 [US1] 實作 `GET /api/messages` 端點（取得公開留言列表）
- [ ] T025 [US1] 實作 `GET /api/messages/{id}` 端點（取得單則留言）
- [ ] T026 [US1] 更新 `NoticeBoard/Views/Home/Index.cshtml` 加入留言表單 UI
- [ ] T027 [US1] 更新 `NoticeBoard/wwwroot/js/site.js` 加入留言送出邏輯與 SignalR 連接
- [ ] T028 [US1] 更新 `NoticeBoard/wwwroot/css/site.css` 加入留言板樣式

**Checkpoint**: 發表留言功能完整可用，可獨立測試

---

## Phase 4: User Story 2 - 查看留言並即時更新（Priority: P2）

**Goal**: 訪客可看到最新留言（最新在上）與按讚數，系統自動更新顯示其他使用者新增的留言或變更

**Independent Test**: 使用兩個瀏覽器視窗，於一處發表留言，另一端應在 2 秒內顯示更新

### Implementation for User Story 2

- [ ] T029 [US2] 加強 `NoticeBoard/Hubs/MessageHub.cs` 實作 `ReceiveMessage` 廣播
- [ ] T030 [US2] 加強 `NoticeBoard/Hubs/MessageHub.cs` 實作 `MessageUpdated` 廣播
- [ ] T031 [US2] 加強 `NoticeBoard/Hubs/MessageHub.cs` 實作 `LikeCountUpdated` 廣播
- [ ] T032 [US2] 更新 `NoticeBoard/wwwroot/js/site.js` 處理 SignalR 事件並即時更新 DOM
- [ ] T033 [US2] 更新 `NoticeBoard/wwwroot/js/site.js` 加入斷線重連與錯誤處理邏輯

**Checkpoint**: 即時更新功能完整可用，可獨立測試

---

## Phase 5: User Story 3 - 按讚（Priority: P2）

**Goal**: 匿名使用者可按讚留言，系統限制同一使用者對同則留言只能按一次

**Independent Test**: 使用單一瀏覽器連續多次按讚，驗證按讚次數只增一次

### Implementation for User Story 3

- [ ] T034 [US3] 建立 `LikeResponse` 在 `NoticeBoard/Models/Responses/LikeResponse.cs`
- [ ] T035 [US3] 建立 `LikeStatusResponse` 在 `NoticeBoard/Models/Responses/LikeStatusResponse.cs`
- [ ] T036 [US3] 建立 `LikesController` 在 `NoticeBoard/Controllers/Api/LikesController.cs`
- [ ] T037 [US3] 實作 `POST /api/likes` 端點（按讚/取消按讚 toggle）
- [ ] T038 [US3] 實作 `GET /api/likes/{messageId}` 端點（檢查按讚狀態）
- [ ] T039 [US3] 實作匿名 Token Cookie 產生與驗證邏輯在 `LikesController`
- [ ] T040 [US3] 更新 `NoticeBoard/wwwroot/js/site.js` 加入按讚 UI 互動與 localStorage 標記
- [ ] T041 [US3] 加強 `LikeService` 實作按讚記錄儲存與重複檢查

**Checkpoint**: 按讚功能完整可用，可獨立測試

---

## Phase 6: User Story 4 - 管理者後台（Priority: P1）

**Goal**: 管理者可登入後台，對留言進行隱藏/刪除/恢復操作

**Independent Test**: 以 admin/admin999 登入後台，對留言執行隱藏/刪除/恢復，檢查前台展示正確反應

### Implementation for User Story 4

- [ ] T042 [US4] 建立 `LoginResponse` 在 `NoticeBoard/Models/Responses/LoginResponse.cs`
- [ ] T043 [US4] 建立 `CleanupResponse` 在 `NoticeBoard/Models/Responses/CleanupResponse.cs`
- [ ] T044 [US4] 建立 `AdminController` 在 `NoticeBoard/Controllers/Api/AdminController.cs`
- [ ] T045 [US4] 實作 `POST /api/admin/login` 端點（Session 認證）
- [ ] T046 [US4] 實作 `POST /api/admin/logout` 端點
- [ ] T047 [US4] 實作 `GET /api/admin/messages` 端點（取得所有留言含隱藏）
- [ ] T048 [US4] 實作 `POST /api/admin/messages/{id}/hide` 端點
- [ ] T049 [US4] 實作 `POST /api/admin/messages/{id}/restore` 端點
- [ ] T050 [US4] 實作 `DELETE /api/admin/messages/{id}` 端點
- [ ] T051 [US4] 實作 `POST /api/admin/cleanup` 端點（手動清理）
- [ ] T052 [US4] 建立 `AdminAuthorizationMiddleware` 在 `NoticeBoard/Middleware/AdminAuthorizationMiddleware.cs`
- [ ] T053 [US4] 建立後台 MVC Controller 在 `NoticeBoard/Controllers/AdminController.cs`
- [ ] T054 [US4] 建立後台視圖在 `NoticeBoard/Views/Admin/Index.cshtml`
- [ ] T055 [US4] 建立後台登入視圖在 `NoticeBoard/Views/Admin/Login.cshtml`
- [ ] T056 [US4] 建立後台 JavaScript 在 `NoticeBoard/wwwroot/js/admin.js`
- [ ] T057 [US4] 建立後台 CSS 在 `NoticeBoard/wwwroot/css/admin.css`
- [ ] T058 [US4] 更新 `NoticeBoard/Program.cs` 註冊 AdminAuthorizationMiddleware

**Checkpoint**: 管理者後台功能完整可用，可獨立測試

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: 跨 User Story 的改進與清理任務

- [ ] T059 實作 `CleanupService` 背景服務在 `NoticeBoard/Services/CleanupService.cs`（24 小時自動清理）
- [ ] T060 更新 `NoticeBoard/Program.cs` 註冊 CleanupService 為 HostedService
- [ ] T061 建立空白 JSON 檔案 `NoticeBoard/Data/messages.json` 與 `NoticeBoard/Data/likes.json`
- [ ] T062 更新 `NoticeBoard/Views/Shared/_Layout.cshtml` 加入 SignalR client script 引用
- [ ] T063 實作輸入驗證、XSS 過濾與黑名單詞彙過濾邏輯在 `MessageService.SanitizeContent()`（黑名單詞彙列表定義於 appsettings.json）
- [ ] T064 實作簡易 Rate Limiting 中介軟體在 `NoticeBoard/Middleware/RateLimitingMiddleware.cs`
- [ ] T065 更新 `NoticeBoard/Program.cs` 配置 RFC 7807 Problem Details
- [ ] T066 程式碼清理與 XML 文件註解完善
- [ ] T067 執行 `quickstart.md` 驗證流程確認功能正常運作

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 無相依 - 可立即開始
- **Foundational (Phase 2)**: 相依於 Setup 完成 - **阻塞所有 User Stories**
- **User Stories (Phase 3-6)**: 相依於 Foundational 完成
  - US1 (Phase 3) 與 US4 (Phase 6) 為 P1 優先級，建議優先實作
  - US2 (Phase 4) 與 US3 (Phase 5) 為 P2 優先級
  - User Stories 之間可平行進行（若有多人開發）
- **Polish (Phase 7)**: 相依於所有 User Stories 完成

### User Story Dependencies

- **User Story 1 (P1)**: Foundational 完成後可開始 - 無其他 Story 相依
- **User Story 2 (P2)**: Foundational 完成後可開始 - 依賴 US1 的留言顯示基礎
- **User Story 3 (P2)**: Foundational 完成後可開始 - 依賴 US1 的留言存在
- **User Story 4 (P1)**: Foundational 完成後可開始 - 無其他 Story 相依

### Within Each User Story

- 模型/請求類別優先
- 服務層次於控制器
- 控制器次於視圖
- 視圖次於 JavaScript 互動

---

## Implementation Strategy

### MVP First（僅 User Story 1）

1. 完成 Phase 1: Setup
2. 完成 Phase 2: Foundational（**關鍵 - 阻塞所有 Stories**）
3. 完成 Phase 3: User Story 1（發表留言）
4. **停止並驗證**: 獨立測試 User Story 1
5. 若需要可先部署/展示 MVP

### Incremental Delivery（增量交付）

1. 完成 Setup + Foundational → 基礎就緒
2. 加入 User Story 1 → 獨立測試 → 部署/展示（MVP！）
3. 加入 User Story 4 → 獨立測試 → 部署/展示（有後台管理的 MVP）
4. 加入 User Story 2 → 獨立測試 → 部署/展示（有即時更新）
5. 加入 User Story 3 → 獨立測試 → 部署/展示（有按讚功能）
6. 完成 Polish → 最終交付

### Parallel Team Strategy

多人開發時：

1. 團隊共同完成 Setup + Foundational
2. Foundational 完成後：
   - 開發者 A: User Story 1（發表留言）
   - 開發者 B: User Story 4（管理後台）
3. 接著：
   - 開發者 A: User Story 2（即時更新）
   - 開發者 B: User Story 3（按讚）
4. Stories 完成後獨立整合

---

## Notes

- [Story] 標籤將任務對應至特定 User Story 以便追蹤
- 每個 User Story 應可獨立完成與測試
- 每個任務或邏輯群組完成後提交 commit
- 可在任何 Checkpoint 停止以獨立驗證 Story
- 避免：模糊任務、同檔案衝突、破壞獨立性的跨 Story 相依

