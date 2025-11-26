# 快速入門指南: 匿名留言板（類 Slido）

**分支**: `001-anon-messageboard` | **日期**: 2025-11-26

---

## 先決條件

### 必要軟體

| 軟體 | 版本 | 說明 |
|------|------|------|
| .NET SDK | 8.0+ | [下載連結](https://dotnet.microsoft.com/download/dotnet/8.0) |
| VS Code | 最新版 | 建議安裝 C# Dev Kit 擴充功能 |
| Git | 2.x+ | 版本控制 |

### 驗證安裝

```bash
# 確認 .NET SDK 版本
dotnet --version
# 預期輸出: 8.0.x 或更高

# 確認 Git 版本
git --version
# 預期輸出: git version 2.x.x
```

---

## 快速啟動

### 1. 複製專案

```bash
git clone <repository-url>
cd Noticeboard
git checkout 001-anon-messageboard
```

### 2. 還原相依套件

```bash
cd NoticeBoard
dotnet restore
```

### 3. 建置專案

```bash
dotnet build
```

### 4. 執行應用程式

```bash
dotnet run
```

### 5. 開啟瀏覽器

```
前台: http://localhost:5000
後台: http://localhost:5000/admin
```

---

## 專案結構概覽

```text
Noticeboard/
├── NoticeBoard/                 # 主要應用程式
│   ├── Controllers/             # MVC 控制器 + API
│   ├── Hubs/                    # SignalR Hub
│   ├── Models/                  # 資料模型
│   ├── Services/                # 業務邏輯服務
│   ├── Views/                   # Razor 視圖
│   ├── Data/                    # JSON 資料檔案
│   └── wwwroot/                 # 靜態資源
├── tests/
│   └── NoticeBoard.Tests/       # 測試專案
└── specs/
    └── 001-anon-messageboard/   # 功能規格與計畫
```

---

## 主要功能入口

### 前台功能

| 功能 | 路徑 | 說明 |
|------|------|------|
| 留言板首頁 | `/` | 查看與發表留言 |
| 發表留言 | `POST /api/messages` | 送出新留言 |
| 按讚 | `POST /api/likes` | 對留言按讚 |

### 後台功能

| 功能 | 路徑 | 說明 |
|------|------|------|
| 管理後台 | `/admin` | 管理留言列表 |
| 登入 | `POST /api/admin/login` | 管理者認證 |
| 隱藏留言 | `POST /api/admin/messages/{id}/hide` | 隱藏指定留言 |
| 恢復留言 | `POST /api/admin/messages/{id}/restore` | 恢復隱藏留言 |
| 刪除留言 | `DELETE /api/admin/messages/{id}` | 永久刪除留言 |

### 即時通訊

| Hub | 事件 | 說明 |
|-----|------|------|
| `/messageHub` | `ReceiveMessage` | 接收新留言 |
| `/messageHub` | `MessageUpdated` | 留言更新通知 |
| `/messageHub` | `LikeCountUpdated` | 按讚數更新 |

---

## 開發指令

### 常用指令

```bash
# 建置專案
dotnet build

# 執行應用程式
dotnet run --project NoticeBoard

# 執行測試
dotnet test

# 僅執行單元測試
dotnet test --filter "Category=Unit"

# 執行測試並產生覆蓋率報告
dotnet test --collect:"XPlat Code Coverage"

# 監看模式（自動重新載入）
dotnet watch --project NoticeBoard
```

### VS Code 任務

專案包含預設的 VS Code 任務：

- **build**: 建置專案
- **run-no-profile**: 執行應用程式

可透過 `Ctrl+Shift+B` 執行建置任務。

---

## 設定說明

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/noticeboard-.log" } }
    ]
  },
  "Storage": {
    "DataPath": "Data",
    "MessagesFile": "messages.json",
    "LikesFile": "likes.json"
  },
  "Cleanup": {
    "RetentionHours": 24,
    "IntervalMinutes": 60
  },
  "Admin": {
    "Username": "admin",
    "Password": "admin999"
  }
}
```

### 環境變數

| 變數 | 說明 | 預設值 |
|------|------|--------|
| `ASPNETCORE_ENVIRONMENT` | 執行環境 | Development |
| `ASPNETCORE_URLS` | 監聽 URL | http://localhost:5000 |

---

## 測試執行

### 執行所有測試

```bash
cd tests/NoticeBoard.Tests
dotnet test
```

### 執行特定類別測試

```bash
# 僅單元測試
dotnet test --filter "Category=Unit"

# 僅整合測試
dotnet test --filter "Category=Integration"

# 快速測試（跳過慢速測試）
dotnet test --filter "Speed=Fast"
```

### 環境問題備案

若遇到測試環境問題：

```bash
# 1. 跳過整合測試
dotnet test --filter "Category!=Integration"

# 2. 設定超時時間
dotnet test --blame-hang-timeout 30s

# 3. 檢查測試輸出
dotnet test --logger "console;verbosity=detailed"
```

---

## 疑難排解

### 常見問題

#### 1. 埠號已被佔用

```bash
# 檢查埠號使用狀況
lsof -i :5000

# 使用其他埠號
dotnet run --urls "http://localhost:5001"
```

#### 2. JSON 檔案權限問題

```bash
# 確認 Data 資料夾權限
chmod 755 NoticeBoard/Data
chmod 644 NoticeBoard/Data/*.json
```

#### 3. SignalR 連線失敗

- 確認 WebSocket 支援已啟用
- 檢查防火牆設定
- 確認 CORS 設定正確

#### 4. 測試超時

```bash
# 增加測試超時時間
dotnet test --blame-hang-timeout 60s

# 或跳過有問題的測試
dotnet test --filter "FullyQualifiedName!=Namespace.TestClass.TestMethod"
```

---

## API 測試

### 使用 curl 測試

```bash
# 發表留言
curl -X POST http://localhost:5000/api/messages \
  -H "Content-Type: application/json" \
  -d '{"content": "測試留言", "nickname": "訪客"}'

# 取得所有留言
curl http://localhost:5000/api/messages

# 按讚
curl -X POST http://localhost:5000/api/likes \
  -H "Content-Type: application/json" \
  -d '{"messageId": "<message-id>"}'

# 管理者登入
curl -X POST http://localhost:5000/api/admin/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "admin999"}' \
  -c cookies.txt

# 隱藏留言（需帶 session cookie）
curl -X POST http://localhost:5000/api/admin/messages/<id>/hide \
  -b cookies.txt
```

---

## 下一步

1. 閱讀 [規格文件](./spec.md) 了解完整需求
2. 閱讀 [研究文件](./research.md) 了解技術決策
3. 閱讀 [資料模型](./data-model.md) 了解資料結構
4. 閱讀 [API 契約](./contracts/api-spec.yaml) 了解 API 規格
