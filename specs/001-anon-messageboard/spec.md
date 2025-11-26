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
 - 留言為臨時性資料，系統將每日刪除 24 小時以上的留言與 LikeRecord。

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
 - **FR-004**: 系統 MUST 允許匿名使用者對留言按讚，並限制同一使用者對同留言只能按讚一次（使用 localStorage/cookie 記錄；伺服器側應具最小防護以避免濫用）。伺服器端應儲存精簡的 LikeRecord（以防範偽造或重複請求），但不得儲存個人識別資訊。
 - **FR-005**: 系統 MUST 提供簡易後台介面供管理者（admin）以單一密碼登入，能檢視留言、隱藏、刪除與恢復留言。
 - **FR-006**: 後台管理操作（隱藏/刪除/恢復）在前台要立刻反映，且被隱藏的留言不應在前台顯示。
 - **FR-007**: 系統 MUST 以 JSON 檔案模擬資料庫，並確保寫入與讀取的編碼與一致性（UTF-8）。
 - **FR-007a**: 系統 MUST 僅保留留言與按讚記錄不超過 24 小時（每日刪除/retain = 24h）。被刪除的留言必須從 JSON 儲存中移除且不可復原。
 - **FR-008**: 系統 MUST 不儲存個人識別資訊（如 IP、email、device ID），以強化匿名性。必要的紀錄僅保留稽核目的並匿名化。
 - **FR-009**: 系統 MUST 處理 XSS 與輸入過濾（在伺服器或前端 escape HTML），並具備黑名單詞彙過濾（可為簡易關鍵字列表）。
 - **FR-010**: 系統 MUST 能夠容納至少 200 名同時使用者的即時交互（品質目標）。
 - **FR-011**: 系統 MUST 紀錄留言狀態（公開 / 隱藏 / 刪除）並在後台可篩選或查詢。
 - **FR-012**: 系統 MUST NOT 支援多房間（no Room ID）。系統僅支援單一全域房間，API 不應接受或忽略 `roomId` 參數；多房間需求屬未來擴充，須另行規劃。

**Acceptance Criteria for storage & privacy & security**:

- FR-007 Acceptance: 在環境中發表 10 則留言後，重新啟動伺服器，留言仍可被載入且內容正確（相同 id 與 created_at）。
- FR-007 Acceptance: 在環境中發表 10 則留言後，重新啟動伺服器，留言仍可被載入且內容正確（相同 id 與 created_at），且 `id` 為 UUID v4 字串。
- FR-007a Acceptance: 發表留言後，模擬留言 created_at 已超過 24 小時並執行每日清除任務，該留言與相應的 LikeRecord 必須從 JSON 儲存檔中被移除且不可復原。
- FR-008 Acceptance: 送出留言後，檢查 JSON 儲存檔，確定沒有包含 IP、email 或裝置唯一識別等個人資訊欄位。
- FR-009 Acceptance: 向系統送出包含 `<script>` 的內容時，前端／後端應顯示 escape 過後文字或移除危險標籤，且不會執行該腳本。

- FR-004 Acceptance: 若系統在伺服器端儲存 LikeRecord，則使用相同的 anon token (cookie) 再次對同一留言按讚應被伺服器拒絕或視為取消按讚；在 JSON 儲存檔中應能找到 LikeRecord 的最小資料結構 (message_id, token, timestamp)。

- FR-012 Acceptance: 若 API 請求包含 `roomId` 參數，伺服器應回傳 400 Bad Request，並顯示訊息 "multi-room not supported"；系統僅在全域房間儲存留言與統計。

*Example of marking unclear requirements:*

- **FR-006**: System MUST authenticate users via [NEEDS CLARIFICATION: auth method not specified - email/password, SSO, OAuth?]
- **FR-007**: System MUST retain user data for [NEEDS CLARIFICATION: retention period not specified]

### Key Entities *(include if feature involves data)*

- **Message**: 代表一則留言。關鍵屬性：
  - id (UUID string)
  - content (string, UTF-8)
  - nickname (string, optional)
  - created_at (ISO8601 timestamp)
  - like_count (integer)
  - status (public / hidden / deleted)

- **LikeRecord (optional)**: 伺服器可選擇儲存單純的 like 記錄或僅以 message.like_count 與 localStorage 控制。若儲存記錄：
  - message_id
  - token (string, anonymized cookie token or ephemeral anon-id; MUST NOT contain PII)
  - timestamp

> Implementation note: If LikeRecords are stored, use a minimal record: { message_id: UUID, token: string, timestamp: ISO8601 }. The token must be a RNG-based anonymized token set as a cookie by the server on first visit (or created by the client), and must not be linkable to identity.

> Implementation note: LikeRecord writes must use the same atomic-write + mutex flow as messages.json to avoid race conditions and file corruption; consider write-batching for high-frequency likes to reduce disk churn.

- **AdminSession (ephemeral)**: 管理者登入狀態（session）由單一密碼管理，伺服器只需臨時驗證並以 session/cookie 管理後台登入狀態。

> Implementation note: `id` fields MUST be UUID v4 strings generated by the server to avoid concurrent write collisions when using JSON file storage. `created_at` will be used for ordering.

> Implementation note: JSON storage writes MUST be atomic. Server write flow:

> 1. Acquire process-level mutex for write access.
> 2. Serialize in-memory state to a temporary file (e.g., `messages.json.tmp`).
> 3. Flush and fsync the temporary file, then perform atomic rename/move to `messages.json`.
> 4. Release mutex.

> This protects against concurrent write collisions and prevents corrupt JSON on crashes.

> Implementation note: Retention/cleanup flow:
>
> - The server must run a scheduled cleanup job (cron-style) that removes messages and LikeRecords older than 24 hours.
> - Cleanup must follow the same atomic-write + mutex flow (write to a tmp file, fsync, rename) to ensure file integrity.
> - Admin 'delete' operation removes a message immediately from the JSON store; hidden messages still exist until deletion.
> - A manual cleanup API endpoint for admin may be provided to run a cleanup on demand for testing or emergency situations (must require admin authentication).

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
- 系統僅支援單一全域房間（no Room ID）；API 若收到 `roomId` 參數應回傳錯誤（400 Bad Request）或明確忽略，避免產生多房間期待。
 - 系統僅支援單一全域房間（no Room ID）；API 若收到 `roomId` 參數應回傳錯誤（400 Bad Request）或明確忽略，避免產生多房間期待。
 - 資料為臨時型：留言與 LikeRecord 最多保留 24 小時 (每日刪除)，系統在 24 小時後會自動清除過期資料。

## Clarifications

### Session 2025-11-26

- Q: Should the system support multiple rooms (Room ID) or always be single-room? → A: **Single room only**. The system must reject or ignore any roomId parameter and operate on a single global room; multi-room support is out of scope for v1.

- Q: Message ID format: Integer auto-increment or UUID string? → A: **UUID string**. IDs are server-generated UUID v4 strings to avoid concurrent-write collisions in JSON file storage and ensure stability across restarts.

- Q: How should the server ensure JSON file consistency under concurrent writes? → A: **Atomic write + process mutex**. The server must serialize writes using a process-level mutex and write to a temp file then rename to the main JSON file.

- Q: Should the server store LikeRecord server-side or rely on localStorage only? → A: **Server-side minimal LikeRecord persisted** (message_id, anon_token, timestamp). Token must be anonymized and not contain PII. This allows server-side protection against forged repeated likes while still respecting anonymity constraints.

- Q: Retention/cleanup policy: how long to keep messages and LikeRecords? → A: **Daily purge**. The system will delete messages and LikeRecords older than 24 hours; data is ephemeral and not retained.

### Integration changes made from clarifications

- Removed any implicit support for roomId in entities or acceptance criteria; `Message` does not include a `roomId` field.
- Added `FR-012` explicitly forbidding multi-room support and clarified Assumptions to prevent future ambiguity.
- Added Message `id` as UUID v4 string and clarified generation method in Key Entities and Implementation notes.
- Added implementation note describing atomic write + mutex flow to ensure JSON file consistency and avoid corruption.
- Added server-side minimal LikeRecord storage as required (if enabled), including fields and privacy constraints.
- Added retention policy requiring daily purge (24h) and added implementation notes for a scheduled cleanup job and admin immediate delete behavior.


