# 📝 匿名留言板 (Noticeboard)

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13-239120?style=flat-square&logo=csharp)](https://docs.microsoft.com/dotnet/csharp/)
[![SignalR](https://img.shields.io/badge/SignalR-即時通訊-blue?style=flat-square)](https://docs.microsoft.com/aspnet/core/signalr/)
[![License](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)](LICENSE)

一個類似 Slido 的匿名即時留言與按讚系統。無需登入，即可自由發表想法、互動按讚，並提供管理後台進行內容管控。

![匿名留言板示意](https://img.shields.io/badge/Demo-即時互動-brightgreen?style=for-the-badge)

## 概述

本專案是一個輕量級的匿名留言板系統，專為教學、研討會、即時問答等場景設計。採用 ASP.NET Core 8 MVC 架構，結合 SignalR 實現即時雙向通訊，讓所有使用者能夠即時看到新留言與按讚更新。

### 主要功能

- **匿名留言** - 無需登入，輸入 1-300 字內容即可發表
- **即時同步** - 透過 SignalR WebSocket 即時推送，所有使用者同步看到更新
- **按讚互動** - 對喜歡的留言按讚，以 localStorage 防止重複按讚
- **管理後台** - 管理者可隱藏、刪除或恢復留言
- **自動清理** - 留言與按讚記錄自動於 24 小時後清除
- **隱私保護** - 不儲存任何個人識別資訊 (PII)

## 技術架構

```text
┌─────────────────────────────────────────────────────────────────┐
│                         前端 (Browser)                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │  Bootstrap 5  │  │   jQuery     │  │  SignalR Client      │  │
│  └──────────────┘  └──────────────┘  └──────────────────────┘  │
└───────────────────────────┬─────────────────────────────────────┘
                            │ HTTP / WebSocket
┌───────────────────────────▼─────────────────────────────────────┐
│                    ASP.NET Core 8 MVC                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │  Controllers  │  │  SignalR Hub │  │  Background Service  │  │
│  │  (API + MVC)  │  │  (即時推送)   │  │  (自動清理 24hr)     │  │
│  └──────────────┘  └──────────────┘  └──────────────────────┘  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │  Services     │  │  Middleware  │  │  Serilog 日誌        │  │
│  │  (商業邏輯)    │  │  (Rate Limit)│  │  (結構化記錄)        │  │
│  └──────────────┘  └──────────────┘  └──────────────────────┘  │
└───────────────────────────┬─────────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────┐
│                       JSON 檔案儲存                              │
│  ┌────────────────────┐  ┌────────────────────────────────────┐ │
│  │  messages.json      │  │  likes.json                        │ │
│  │  (留言資料)         │  │  (按讚記錄)                         │ │
│  └────────────────────┘  └────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### 技術重點

| 技術 | 說明 |
|------|------|
| **ASP.NET Core 8 MVC** | 後端框架，提供 API 與頁面渲染 |
| **SignalR** | 即時雙向通訊，實現留言與按讚的即時同步 |
| **JSON 檔案儲存** | 輕量級資料持久化，無需外部資料庫 |
| **Serilog** | 結構化日誌記錄，便於除錯與監控 |
| **RFC 7807 Problem Details** | 標準化 API 錯誤回應格式 |
| **Hosted Service** | 背景服務自動清理過期資料 |

## 快速開始

### 前置需求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Git](https://git-scm.com/downloads)

### 安裝與執行

1. **複製專案**

   ```bash
   git clone https://github.com/HyperLee/Noticeboard.git
   cd Noticeboard
   ```

2. **還原相依套件**

   ```bash
   dotnet restore
   ```

3. **執行應用程式**

   ```bash
   dotnet run --project NoticeBoard/NoticeBoard.csproj
   ```

4. **開啟瀏覽器**

   前台留言板：`http://localhost:5000`  
   管理後台：`http://localhost:5000/admin`

> [!TIP]
> 管理後台預設帳號密碼：`admin` / `admin999`

## 專案結構

```text
Noticeboard/
├── NoticeBoard/                    # 主要應用程式
│   ├── Controllers/                # MVC 與 API 控制器
│   │   ├── Api/                    # RESTful API
│   │   │   ├── MessagesController  # 留言 CRUD API
│   │   │   ├── LikesController     # 按讚 API
│   │   │   └── AdminController     # 管理 API
│   │   ├── HomeController          # 前台頁面
│   │   └── AdminController         # 管理後台頁面
│   ├── Hubs/
│   │   └── MessageHub.cs           # SignalR Hub (即時推送)
│   ├── Services/                   # 商業邏輯層
│   │   ├── MessageService          # 留言服務
│   │   ├── LikeService             # 按讚服務
│   │   ├── JsonStorageService      # JSON 儲存 (原子寫入)
│   │   └── CleanupService          # 24hr 自動清理
│   ├── Models/                     # 資料模型
│   ├── Middleware/                 # 中介軟體
│   │   ├── RateLimitingMiddleware  # 速率限制
│   │   └── AdminAuthorizationMiddleware
│   ├── Views/                      # Razor 視圖
│   └── Data/                       # JSON 資料檔案
│       ├── messages.json
│       └── likes.json
├── tests/
│   └── NoticeBoard.Tests/          # xUnit 單元測試
└── specs/                          # 功能規格文件
```

## API 說明

### 留言 API

| 方法 | 端點 | 說明 |
|------|------|------|
| `GET` | `/api/messages` | 取得所有公開留言 |
| `GET` | `/api/messages/{id}` | 取得單則留言 |
| `POST` | `/api/messages` | 發表新留言 |

### 按讚 API

| 方法 | 端點 | 說明 |
|------|------|------|
| `POST` | `/api/likes` | 按讚 / 取消按讚 |
| `GET` | `/api/likes/{messageId}/status` | 查詢按讚狀態 |

### 管理 API

| 方法 | 端點 | 說明 |
|------|------|------|
| `POST` | `/api/admin/login` | 管理者登入（支援 JSON 與表單格式） |
| `POST` | `/api/admin/logout` | 管理者登出 |
| `PUT` | `/api/admin/messages/{id}/hide` | 隱藏留言 |
| `PUT` | `/api/admin/messages/{id}/restore` | 恢復留言 |
| `DELETE` | `/api/admin/messages/{id}` | 刪除留言 |

> [!NOTE]
> **登入 API 支援兩種格式：**
>
> - `application/json`：回傳 JSON 含 `redirectUrl` 欄位
> - `application/x-www-form-urlencoded`：成功後自動重新導向至 `/Admin`

> [!NOTE]
> 完整 API 規格請參閱 [API Spec](specs/001-anon-messageboard/contracts/api-spec.yaml)

## 即時通訊

本專案使用 SignalR 實現即時雙向通訊，支援以下事件：

| 事件 | 說明 |
|------|------|
| `ReceiveMessage` | 接收新留言 |
| `MessageUpdated` | 留言更新（恢復顯示） |
| `MessageDeleted` | 留言刪除/隱藏 |
| `LikeCountUpdated` | 按讚數更新 |

## 設定

應用程式設定位於 `appsettings.json`：

```json
{
  "Storage": {
    "DataDirectory": "Data",
    "MessagesFile": "messages.json",
    "LikesFile": "likes.json"
  },
  "Admin": {
    "Username": "admin",
    "Password": "admin999",
    "SessionTimeoutMinutes": 30
  },
  "Cleanup": {
    "RetentionHours": 24,
    "IntervalMinutes": 60
  }
}
```

> [!WARNING]
> 本專案為教學/展示用途，管理後台使用單一密碼驗證，不適用於生產環境。

## 安全特性

- **XSS 防護** - 輸入內容自動 HTML 編碼
- **速率限制** - 防止短時間內大量請求
- **黑名單過濾** - 可設定禁用關鍵字
- **Session 認證** - 管理後台使用安全 Cookie
- **無 PII 儲存** - 不儲存 IP、Email 等個人資訊

## 測試

執行單元測試：

```bash
dotnet test
```

## 效能目標

- 支援 200 名同時在線使用者
- 留言發表至顯示 < 10 秒
- 即時訊息同步延遲 < 2 秒 (P95)
- API 回應時間 < 200ms (P95)

## 相關資源

- [ASP.NET Core 文件](https://docs.microsoft.com/aspnet/core/)
- [SignalR 文件](https://docs.microsoft.com/aspnet/core/signalr/)
- [Serilog 文件](https://serilog.net/)
