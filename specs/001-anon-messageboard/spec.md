# Feature Specification: 匿名留言板（類 Slido）

**Feature Branch**: `001-anon-messageboard`  
**Created**: 2025-11-26  
**Status**: Draft  
**Input**: User description: "匿名留言板系統（類 Slido）：提供一個匿名、免登入的即時留言與按讚系統，包含前台留言/查看/按讚以及後台管理（隱藏/刪除/恢復），資料儲存在 JSON 檔以簡化部署與教學用途。"

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
  
  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - 發表留言（Priority: P1）

作為一位匿名使用者（Anonymous User），我想要在前台輸入文字並送出留言，系統應接受合法內容並立即顯示在留言列表上供其他使用者看到。

**Why this priority**: 發表留言是系統的核心互動，沒有此功能則無法達成主要目的。

**Independent Test**: 前端開發者可以模擬 HTTP POST 請求或使用 UI 表單送出留言，驗證資料寫入後立刻透過 WebSocket 或輪詢被前端接收顯示。

**Acceptance Scenarios**:

1. **Given** 使用者在留言欄輸入 1~300 字且非空白內容，**When** 使用者按下送出，**Then** API 回傳成功並於前端即時顯示新留言（包含時間與按讚數）。
2. **Given** 使用者輸入空白或超過 300 字的內容，**When** 使用者按下送出，**Then** 前端顯示錯誤並阻止傳送。
3. **Given** 當前端與伺服器斷線，**When** 使用者送出留言，**Then** 常態上顯示錯誤提示並嘗試重試/緩存（依 UI 行為定義）。

---

### User Story 2 - 查看留言並即時更新（Priority: P2）

作為一位訪客，我希望能在頁面看到最新留言（最新在上）與留言的按讚數，且系統會自動更新顯示其他使用者新增的留言或變更。

**Why this priority**: 即時性是留言板使用體驗的關鍵，讓互動發生。

**Independent Test**: 使用兩台或多個瀏覽器/裝置開啟同一房間，於一處發表留言或按讚，其他端應在 2 秒內顯示更新（WebSocket 測試）或在短時間內透過輪詢顯示。

**Acceptance Scenarios**:

1. **Given** 有多個使用者在相同房間，**When** 任一使用者發表留言，**Then** 其他使用者在 UI 上自動收到該留言並顯示。
2. **Given** 某則留言的按讚數被更新，**When** 使用者按讚/取消讚，**Then** 所有已連線的使用者可在短時間內看到按讚數變更。

---

### User Story 3 - 按讚（Priority: P2）

作為匿名使用者，我希望可以按讚某則留言，系統應限制每位使用者對同則留言只能按一次（靠 localStorage/cookie 標記），按讚後立即更新按讚數。

**Why this priority**: 按讚是鼓勵互動的次要但重要功能，影響後續像熱門留言排序之擴充功能。

**Independent Test**: 使用單一瀏覽器連續多次按讚，驗證按讚次數只增一次；清除 localStorage 或換瀏覽器則可再次按讚。

**Acceptance Scenarios**:

1. **Given** 使用者尚未對某留言按讚，**When** 使用者按讚按鈕，**Then** 按讚數 +1，並在 localStorage 標記已按讚。
2. **Given** 使用者已按讚，**When** 使用者再次按讚或取消，**Then** UI 顯示已按讚狀態（或允許取消贊取）並更新按讚數。
[Add more user stories as needed, each with an assigned priority]

### User Story 4 - 管理者後台（Priority: P1）

作為管理者，我希望能看到一個留言列表、對留言進行隱藏/刪除/恢復操作，並以單一密碼驗證（admin/admin999）登入管理後台。

**Why this priority**: 管理後台用來處理不當留言，屬於系統維運與內容品質控管核心。

**Independent Test**: 以管理者身分登入後台，對留言執行隱藏/刪除/恢復，並檢查前台的展示是否正確反應狀態變化。

**Acceptance Scenarios**:

1. **Given** 管理者登入後台，**When** 管理者點選隱藏某則留言，**Then** 前台不再顯示該留言（但後台仍可搜尋與恢復）。
2. **Given** 管理者刪除留言，**When** 管理者確定刪除，**Then** 留言標記為刪除且不可在前台或後台操作（僅做記錄）。
3. **Given** 管理者已隱藏留言，**When** 管理者恢復該留言，**Then** 前台再次顯示該留言。

---

[Add more user stories as needed, each with an assigned priority]

### Edge Cases

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right edge cases.
-->

 - 留言超過 300 字：系統於前端即時驗證並阻止傳送；API 也應驗證並回傳錯誤。
 - 空白或只含空白的留言：系統阻止送出並顯示錯誤。
 - 使用者嘗試重複按讚：系統應依 localStorage/cookie 限制，伺服器端也應具備保護（例如同一 cookie/session 對同留言只可計一次），以抵擋偽造請求。
 - XSS 或惡意 JavaScript：系統在後端/前端都需過濾或 escape HTML，避免腳本注入。
 - 特殊字元或非 UTF-8 編碼導致亂碼：儲存 JSON 與網頁顯示時要確認編碼為 UTF-8，正確處理中文。
 - 大量速率送出（spam/bot）：需設計簡易 rate-limiting/honeypot 或 throttle，避免單位 IP/裝置短時間內大量留言。
 - 無法連線或 WebSocket 掉線：前端應顯示錯誤、重試或用輪詢作為 fallback。

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

### Functional Requirements

 - **FR-001**: 系統 MUST 允許匿名使用者輸入並送出留言（字數 1 ~ 300 字），且阻止空白留言或超長留言。
 - **FR-002**: 系統 MUST 支援選擇性暱稱欄位（可留空，以匿名顯示）。
 - **FR-003**: 系統 MUST 立即將成功的留言顯示在前台留言列表（以 WebSocket 或輪詢實現即時更新）。
 - **FR-004**: 系統 MUST 允許匿名使用者對留言按讚，並限制同一使用者對同留言只能按讚一次（使用 localStorage/cookie 記錄；伺服器側應具最小防護以避免濫用）。
 - **FR-005**: 系統 MUST 提供簡易後台介面供管理者（admin）以單一密碼登入，能檢視留言、隱藏、刪除與恢復留言。
 - **FR-006**: 後台管理操作（隱藏/刪除/恢復）在前台要立刻反映，且被隱藏的留言不應在前台顯示。
 - **FR-007**: 系統 MUST 以 JSON 檔案模擬資料庫，並確保寫入與讀取的編碼與一致性（UTF-8）。
 - **FR-008**: 系統 MUST 不儲存個人識別資訊（如 IP、email、device ID），以強化匿名性。必要的紀錄僅保留稽核目的並匿名化。
 - **FR-009**: 系統 MUST 處理 XSS 與輸入過濾（在伺服器或前端 escape HTML），並具備黑名單詞彙過濾（可為簡易關鍵字列表）。
 - **FR-010**: 系統 MUST 能夠容納至少 200 名同時使用者的即時交互（品質目標）。
 - **FR-011**: 系統 MUST 紀錄留言狀態（公開 / 隱藏 / 刪除）並在後台可篩選或查詢。

**Acceptance Criteria for storage & privacy & security**:

- FR-007 Acceptance: 在環境中發表 10 則留言後，重新啟動伺服器，留言仍可被載入且內容正確（相同 id 與 created_at）。
- FR-008 Acceptance: 送出留言後，檢查 JSON 儲存檔，確定沒有包含 IP、email 或裝置唯一識別等個人資訊欄位。
- FR-009 Acceptance: 向系統送出包含 `<script>` 的內容時，前端／後端應顯示 escape 過後文字或移除危險標籤，且不會執行該腳本。

*Example of marking unclear requirements:*

- **FR-006**: System MUST authenticate users via [NEEDS CLARIFICATION: auth method not specified - email/password, SSO, OAuth?]
- **FR-007**: System MUST retain user data for [NEEDS CLARIFICATION: retention period not specified]

### Key Entities *(include if feature involves data)*

- **Message**: 代表一則留言。關鍵屬性：
  - id (string 或 integer)
  - content (string, UTF-8)
  - nickname (string, optional)
  - created_at (ISO8601 timestamp)
  - like_count (integer)
  - status (public / hidden / deleted)

- **LikeRecord (optional)**: 伺服器可選擇儲存單純的 like 記錄或僅以 message.like_count 與 localStorage 控制。若儲存記錄：
  - message_id
  - token/cookie/anon-id（若使用）
  - timestamp

- **AdminSession (ephemeral)**: 管理者登入狀態（session）由單一密碼管理，伺服器只需臨時驗證並以 session/cookie 管理後台登入狀態。

## Success Criteria *(mandatory)*

<!--
  ACTION REQUIRED: Define measurable success criteria.
  These must be technology-agnostic and measurable.
-->

### Measurable Outcomes

- **SC-001**: 使用者能在 10 秒內成功發表留言（從按下送出到留言顯示在前端）。
- **SC-002**: 同一房間至少支援 200 名同時使用者進行閱讀與按讚（不包含管理後台操作）。
- **SC-003**: 95% 的留言與按讚事件會在 2 秒內同步到所有已連線的客戶端（WebSocket latency）。
- **SC-004**: 每則留言預設僅能由同一台瀏覽器/裝置按讚一次（localStorage），99% 檢測到此限制在常見使用情境下有效。
- **SC-005**: 管理者對留言的隱藏/刪除/恢復操作在 2 秒內於前台同步反映。

## Assumptions (doc business/implementation choices used in the spec)

- 使用 JSON 檔案模擬資料庫（server-side file store），不使用外部 DB
- Admin 驗證採用單一密碼（admin/admin999）供練習或 demo 使用，不做生產級安全機制
- 即時同步首選 WebSocket，若環境不支援則以短輪詢做為 fallback
- Client 端使用 localStorage 或 cookie 控制按讚限制，伺服端可以補強檢查但不依賴其為唯一保護

