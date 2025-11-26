# 資料模型: 匿名留言板（類 Slido）

**分支**: `001-anon-messageboard` | **日期**: 2025-11-26

---

## 實體定義

### Message（留言）

代表一則匿名留言。

| 欄位 | 型別 | 必填 | 說明 |
|------|------|------|------|
| `id` | `string` (UUID v4) | ✅ | 唯一識別碼，伺服器端產生 |
| `content` | `string` | ✅ | 留言內容（1-300 字，UTF-8） |
| `nickname` | `string?` | ❌ | 選擇性暱稱（空值顯示為「匿名」） |
| `createdAt` | `DateTime` (ISO 8601) | ✅ | 建立時間（UTC） |
| `likeCount` | `int` | ✅ | 按讚數（預設 0） |
| `status` | `MessageStatus` | ✅ | 留言狀態（Public/Hidden/Deleted） |

#### C# 模型

```csharp
namespace NoticeBoard.Models;

/// <summary>
/// 代表一則匿名留言
/// </summary>
/// <example>
/// <code>
/// var message = new Message
/// {
///     Id = Guid.NewGuid().ToString(),
///     Content = "這是一則測試留言",
///     Nickname = "訪客",
///     CreatedAt = DateTime.UtcNow,
///     LikeCount = 0,
///     Status = MessageStatus.Public
/// };
/// </code>
/// </example>
public class Message
{
    /// <summary>
    /// 唯一識別碼（UUID v4 字串）
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// 留言內容（1-300 字）
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// 選擇性暱稱（null 顯示為「匿名」）
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// 建立時間（UTC）
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 按讚數
    /// </summary>
    public int LikeCount { get; set; }

    /// <summary>
    /// 留言狀態
    /// </summary>
    public MessageStatus Status { get; set; }
}
```

#### JSON 範例

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "content": "這是一則測試留言",
  "nickname": "訪客",
  "createdAt": "2025-11-26T12:00:00Z",
  "likeCount": 5,
  "status": "Public"
}
```

---

### MessageStatus（留言狀態列舉）

定義留言的可見狀態。

| 值 | 說明 |
|----|------|
| `Public` | 公開可見 |
| `Hidden` | 隱藏（管理者可恢復） |
| `Deleted` | 已刪除（不可恢復） |

#### C# 模型

```csharp
namespace NoticeBoard.Models;

/// <summary>
/// 留言狀態列舉
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// 公開可見
    /// </summary>
    Public = 0,

    /// <summary>
    /// 隱藏（管理者可恢復）
    /// </summary>
    Hidden = 1,

    /// <summary>
    /// 已刪除（不可恢復）
    /// </summary>
    Deleted = 2
}
```

---

### LikeRecord（按讚記錄）

伺服器端儲存的精簡按讚記錄，用於防止重複按讚。

| 欄位 | 型別 | 必填 | 說明 |
|------|------|------|------|
| `messageId` | `string` (UUID) | ✅ | 留言識別碼 |
| `token` | `string` | ✅ | 匿名 Token（不含 PII） |
| `timestamp` | `DateTime` (ISO 8601) | ✅ | 按讚時間（UTC） |

#### C# 模型

```csharp
namespace NoticeBoard.Models;

/// <summary>
/// 按讚記錄（伺服器端儲存）
/// </summary>
/// <remarks>
/// Token 為 RNG 產生的匿名識別碼，不含任何個人識別資訊（PII）
/// </remarks>
public class LikeRecord
{
    /// <summary>
    /// 留言識別碼
    /// </summary>
    public required string MessageId { get; set; }

    /// <summary>
    /// 匿名 Token（Cookie 產生的隨機識別碼）
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// 按讚時間（UTC）
    /// </summary>
    public DateTime Timestamp { get; set; }
}
```

#### JSON 範例

```json
{
  "messageId": "550e8400-e29b-41d4-a716-446655440000",
  "token": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "timestamp": "2025-11-26T12:05:00Z"
}
```

---

## 視圖模型 (ViewModels)

### MessageViewModel

前台顯示用的留言視圖模型。

```csharp
namespace NoticeBoard.Models.ViewModels;

/// <summary>
/// 前台留言顯示視圖模型
/// </summary>
public class MessageViewModel
{
    /// <summary>
    /// 留言識別碼
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// 留言內容（已過濾 XSS）
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// 顯示名稱（無暱稱時顯示「匿名」）
    /// </summary>
    public string DisplayName => Nickname ?? "匿名";

    /// <summary>
    /// 原始暱稱
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 按讚數
    /// </summary>
    public int LikeCount { get; set; }

    /// <summary>
    /// 是否已按讚（前端 localStorage 判斷）
    /// </summary>
    public bool HasLiked { get; set; }
}
```

---

### AdminMessageViewModel

後台管理用的留言視圖模型。

```csharp
namespace NoticeBoard.Models.ViewModels;

/// <summary>
/// 後台管理留言視圖模型
/// </summary>
public class AdminMessageViewModel : MessageViewModel
{
    /// <summary>
    /// 留言狀態
    /// </summary>
    public MessageStatus Status { get; set; }

    /// <summary>
    /// 狀態顯示文字
    /// </summary>
    public string StatusText => Status switch
    {
        MessageStatus.Public => "公開",
        MessageStatus.Hidden => "隱藏",
        MessageStatus.Deleted => "已刪除",
        _ => "未知"
    };

    /// <summary>
    /// 是否可隱藏
    /// </summary>
    public bool CanHide => Status == MessageStatus.Public;

    /// <summary>
    /// 是否可恢復
    /// </summary>
    public bool CanRestore => Status == MessageStatus.Hidden;

    /// <summary>
    /// 是否可刪除
    /// </summary>
    public bool CanDelete => Status != MessageStatus.Deleted;
}
```

---

## 請求/回應模型

### CreateMessageRequest

建立留言請求。

```csharp
namespace NoticeBoard.Models.Requests;

/// <summary>
/// 建立留言請求
/// </summary>
public class CreateMessageRequest
{
    /// <summary>
    /// 留言內容（1-300 字）
    /// </summary>
    [Required(ErrorMessage = "留言內容為必填")]
    [StringLength(300, MinimumLength = 1, ErrorMessage = "留言內容須為 1-300 字")]
    public required string Content { get; set; }

    /// <summary>
    /// 選擇性暱稱
    /// </summary>
    [StringLength(50, ErrorMessage = "暱稱不可超過 50 字")]
    public string? Nickname { get; set; }
}
```

---

### LikeRequest

按讚請求。

```csharp
namespace NoticeBoard.Models.Requests;

/// <summary>
/// 按讚請求
/// </summary>
public class LikeRequest
{
    /// <summary>
    /// 留言識別碼
    /// </summary>
    [Required(ErrorMessage = "留言識別碼為必填")]
    public required string MessageId { get; set; }
}
```

---

### AdminLoginRequest

管理者登入請求。

```csharp
namespace NoticeBoard.Models.Requests;

/// <summary>
/// 管理者登入請求
/// </summary>
public class AdminLoginRequest
{
    /// <summary>
    /// 使用者名稱
    /// </summary>
    [Required(ErrorMessage = "使用者名稱為必填")]
    public required string Username { get; set; }

    /// <summary>
    /// 密碼
    /// </summary>
    [Required(ErrorMessage = "密碼為必填")]
    public required string Password { get; set; }
}
```

---

### AdminActionRequest

管理者操作請求（隱藏/刪除/恢復）。

```csharp
namespace NoticeBoard.Models.Requests;

/// <summary>
/// 管理者操作請求
/// </summary>
public class AdminActionRequest
{
    /// <summary>
    /// 留言識別碼
    /// </summary>
    [Required(ErrorMessage = "留言識別碼為必填")]
    public required string MessageId { get; set; }

    /// <summary>
    /// 操作類型
    /// </summary>
    [Required(ErrorMessage = "操作類型為必填")]
    public required AdminAction Action { get; set; }
}

/// <summary>
/// 管理者操作類型
/// </summary>
public enum AdminAction
{
    /// <summary>
    /// 隱藏留言
    /// </summary>
    Hide,

    /// <summary>
    /// 恢復留言
    /// </summary>
    Restore,

    /// <summary>
    /// 刪除留言
    /// </summary>
    Delete
}
```

---

## 驗證規則

| 實體 | 欄位 | 規則 |
|------|------|------|
| Message | `content` | 必填、1-300 字、不可全空白 |
| Message | `nickname` | 選填、最多 50 字 |
| Message | `id` | UUID v4 格式 |
| LikeRecord | `messageId` | 必須對應有效的 Message.id |
| LikeRecord | `token` | 必填、不可為空 |

---

## 狀態轉換

```
┌─────────┐
│ Public  │ ←──────────────┐
└────┬────┘                │
     │ hide()              │ restore()
     ▼                     │
┌─────────┐                │
│ Hidden  │ ───────────────┘
└────┬────┘
     │ delete()
     ▼
┌─────────┐
│ Deleted │ (終態，不可逆)
└─────────┘
```

---

## 資料儲存格式

### messages.json

```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "content": "第一則留言",
    "nickname": null,
    "createdAt": "2025-11-26T12:00:00Z",
    "likeCount": 3,
    "status": "Public"
  },
  {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "content": "第二則留言",
    "nickname": "訪客A",
    "createdAt": "2025-11-26T12:01:00Z",
    "likeCount": 0,
    "status": "Hidden"
  }
]
```

### likes.json

```json
[
  {
    "messageId": "550e8400-e29b-41d4-a716-446655440000",
    "token": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "timestamp": "2025-11-26T12:05:00Z"
  },
  {
    "messageId": "550e8400-e29b-41d4-a716-446655440000",
    "token": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    "timestamp": "2025-11-26T12:06:00Z"
  }
]
```
