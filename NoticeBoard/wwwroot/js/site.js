// 匿名留言板 - 前端邏輯
// 使用 SignalR 即時通訊

'use strict';

/**
 * 留言板應用程式主模組
 */
const NoticeBoard = (function () {
    // SignalR 連線
    let connection = null;
    
    // DOM 元素快取
    const elements = {
        messageForm: null,
        contentInput: null,
        nicknameInput: null,
        charCount: null,
        submitBtn: null,
        messagesList: null,
        loadingIndicator: null,
        emptyMessage: null,
        connectionIndicator: null,
        refreshBtn: null,
        messageTemplate: null
    };

    /**
     * 初始化 DOM 元素參考
     */
    function initElements() {
        elements.messageForm = document.getElementById('messageForm');
        elements.contentInput = document.getElementById('contentInput');
        elements.nicknameInput = document.getElementById('nicknameInput');
        elements.charCount = document.getElementById('charCount');
        elements.submitBtn = document.getElementById('submitBtn');
        elements.messagesList = document.getElementById('messagesList');
        elements.loadingIndicator = document.getElementById('loadingIndicator');
        elements.emptyMessage = document.getElementById('emptyMessage');
        elements.connectionIndicator = document.getElementById('connectionIndicator');
        elements.refreshBtn = document.getElementById('refreshBtn');
        elements.messageTemplate = document.getElementById('messageTemplate');
    }

    /**
     * 初始化 SignalR 連線
     */
    async function initSignalR() {
        if (typeof signalR === 'undefined') {
            console.warn('SignalR 函式庫尚未載入');
            showToast('即時更新功能不可用', 'warning');
            return;
        }

        connection = new signalR.HubConnectionBuilder()
            .withUrl('/messageHub')
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: retryContext => {
                    // 漸進式重試延遲：0, 2s, 5s, 10s, 30s, 然後每 60s 重試一次
                    const delays = [0, 2000, 5000, 10000, 30000];
                    if (retryContext.previousRetryCount < delays.length) {
                        return delays[retryContext.previousRetryCount];
                    }
                    return 60000; // 超過後每分鐘重試一次
                }
            })
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // 註冊事件處理器
        connection.on('ReceiveMessage', handleNewMessage);
        connection.on('MessageUpdated', handleMessageUpdated);
        connection.on('MessageDeleted', handleMessageDeleted);
        connection.on('LikeCountUpdated', handleLikeCountUpdated);

        // 連線狀態變更
        connection.onreconnecting((error) => {
            console.warn('SignalR 重新連線中...', error);
            updateConnectionStatus('reconnecting');
            showToast('連線中斷，正在重新連線...', 'warning');
        });

        connection.onreconnected((connectionId) => {
            console.log('SignalR 已重新連線:', connectionId);
            updateConnectionStatus('connected');
            showToast('已重新連線', 'success');
            loadMessages(); // 重連後重新載入留言以同步最新狀態
        });

        connection.onclose((error) => {
            console.error('SignalR 連線已關閉:', error);
            updateConnectionStatus('disconnected');
            showToast('連線已中斷，點擊重新連線', 'error', true);
        });

        // 啟動連線
        await startConnection();
    }

    /**
     * 啟動 SignalR 連線
     * @returns {Promise<boolean>} 連線是否成功
     */
    async function startConnection() {
        if (!connection) return false;

        try {
            await connection.start();
            updateConnectionStatus('connected');
            console.log('SignalR 連線成功');
            return true;
        } catch (err) {
            console.error('SignalR 連線失敗:', err);
            updateConnectionStatus('disconnected');
            return false;
        }
    }

    /**
     * 手動重新連線
     */
    async function reconnect() {
        if (!connection) {
            await initSignalR();
            return;
        }

        if (connection.state === signalR.HubConnectionState.Disconnected) {
            showToast('正在連線...', 'info');
            const success = await startConnection();
            if (success) {
                await loadMessages();
            }
        }
    }

    /**
     * 更新連線狀態指示器
     * @param {string} status - 連線狀態 (connected/reconnecting/disconnected)
     */
    function updateConnectionStatus(status) {
        if (!elements.connectionIndicator) return;

        const statusConfig = {
            connected: { class: 'bg-success', text: '已連線', clickable: false },
            reconnecting: { class: 'bg-warning', text: '重新連線中...', clickable: false },
            disconnected: { class: 'bg-danger', text: '已斷線 - 點擊重連', clickable: true }
        };

        const config = statusConfig[status] || statusConfig.disconnected;
        elements.connectionIndicator.className = `badge ${config.class}`;
        elements.connectionIndicator.innerHTML = `<span class="status-dot"></span> ${config.text}`;
        elements.connectionIndicator.style.cursor = config.clickable ? 'pointer' : 'default';
        
        // 斷線時允許點擊重連
        if (config.clickable) {
            elements.connectionIndicator.onclick = reconnect;
        } else {
            elements.connectionIndicator.onclick = null;
        }
    }

    /**
     * 處理接收到新留言
     * @param {Object} message - 留言物件
     */
    function handleNewMessage(message) {
        console.log('收到新留言:', message);
        prependMessage(message);
        updateEmptyState();
        
        // 顯示視覺提示
        showToast('有新留言！', 'info', false, 2000);
    }

    /**
     * 處理留言更新
     * @param {Object} message - 更新後的留言物件
     */
    function handleMessageUpdated(message) {
        console.log('留言已更新:', message);
        const card = document.querySelector(`[data-message-id="${message.id}"]`);
        if (card) {
            // 加入更新動畫
            card.classList.add('highlight-update');
            updateMessageCard(card, message);
            
            // 移除動畫類別
            setTimeout(() => {
                card.classList.remove('highlight-update');
            }, 1000);
        }
    }

    /**
     * 處理留言刪除/隱藏
     * @param {string} messageId - 留言識別碼
     */
    function handleMessageDeleted(messageId) {
        console.log('留言已刪除:', messageId);
        const card = document.querySelector(`[data-message-id="${messageId}"]`);
        if (card) {
            card.classList.add('fade-out');
            setTimeout(() => {
                card.remove();
                updateEmptyState();
            }, 300);
        }
    }

    /**
     * 處理按讚數更新
     * @param {string} messageId - 留言識別碼
     * @param {number} likeCount - 新的按讚數
     */
    function handleLikeCountUpdated(messageId, likeCount) {
        console.log('按讚數更新:', messageId, likeCount);
        const card = document.querySelector(`[data-message-id="${messageId}"]`);
        if (card) {
            const likeCountEl = card.querySelector('.like-count');
            const likeBtn = card.querySelector('.like-btn');
            
            if (likeCountEl) {
                const oldCount = parseInt(likeCountEl.textContent, 10) || 0;
                likeCountEl.textContent = likeCount;
                
                // 按讚數增加時加入彈跳動畫
                if (likeCount > oldCount && likeBtn) {
                    likeBtn.classList.add('like-bounce');
                    setTimeout(() => {
                        likeBtn.classList.remove('like-bounce');
                    }, 300);
                }
            }
        }
    }

    /**
     * 載入留言列表
     */
    async function loadMessages() {
        showLoading(true);

        try {
            const response = await fetch('/api/messages');
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const messages = await response.json();
            renderMessages(messages);
        } catch (err) {
            console.error('載入留言失敗:', err);
            showError('載入留言失敗，請稍後再試');
        } finally {
            showLoading(false);
        }
    }

    /**
     * 渲染留言列表
     * @param {Array} messages - 留言陣列
     */
    function renderMessages(messages) {
        if (!elements.messagesList) return;

        elements.messagesList.innerHTML = '';
        
        if (messages && messages.length > 0) {
            messages.forEach(message => {
                const card = createMessageCard(message);
                elements.messagesList.appendChild(card);
            });
        }

        updateEmptyState();
    }

    /**
     * 建立留言卡片
     * @param {Object} message - 留言物件
     * @returns {HTMLElement} 留言卡片元素
     */
    function createMessageCard(message) {
        if (!elements.messageTemplate) {
            // 若無範本，建立簡單的卡片
            const card = document.createElement('article');
            card.className = 'message-card card mb-3 shadow-sm';
            card.dataset.messageId = message.id;
            card.innerHTML = `
                <div class="card-body">
                    <div class="message-header d-flex justify-content-between align-items-start mb-2">
                        <strong class="display-name">${escapeHtml(message.displayName)}</strong>
                        <small class="message-time text-muted">${formatTime(message.createdAt)}</small>
                    </div>
                    <p class="card-text mb-2">${escapeHtml(message.content)}</p>
                    <div class="message-footer d-flex justify-content-between align-items-center">
                        <button class="btn btn-outline-danger btn-sm like-btn" title="按讚">
                            <span class="like-icon">🤍</span>
                            <span class="like-count">${message.likeCount}</span>
                        </button>
                    </div>
                </div>
            `;
            return card;
        }

        const template = elements.messageTemplate.content.cloneNode(true);
        const card = template.querySelector('.message-card');
        
        updateMessageCard(card, message);
        
        return card;
    }

    /**
     * 更新留言卡片內容
     * @param {HTMLElement} card - 卡片元素
     * @param {Object} message - 留言物件
     */
    function updateMessageCard(card, message) {
        card.dataset.messageId = message.id;
        
        const displayName = card.querySelector('.display-name');
        const messageTime = card.querySelector('.message-time');
        const cardText = card.querySelector('.card-text');
        const likeCount = card.querySelector('.like-count');

        if (displayName) displayName.textContent = message.displayName;
        if (messageTime) messageTime.textContent = formatTime(message.createdAt);
        if (cardText) cardText.textContent = message.content;
        if (likeCount) likeCount.textContent = message.likeCount;

        // 更新按讚狀態
        const likeBtn = card.querySelector('.like-btn');
        const likeIcon = card.querySelector('.like-icon');
        if (likeBtn && likeIcon) {
            const hasLiked = isLiked(message.id);
            likeIcon.textContent = hasLiked ? '❤️' : '🤍';
            likeBtn.classList.toggle('liked', hasLiked);
        }
    }

    /**
     * 在列表頂端插入新留言
     * @param {Object} message - 留言物件
     */
    function prependMessage(message) {
        if (!elements.messagesList) return;

        // 檢查是否已存在
        if (document.querySelector(`[data-message-id="${message.id}"]`)) {
            return;
        }

        const card = createMessageCard(message);
        card.classList.add('fade-in');
        elements.messagesList.insertBefore(card, elements.messagesList.firstChild);
    }

    /**
     * 顯示/隱藏載入指示器
     * @param {boolean} show - 是否顯示
     */
    function showLoading(show) {
        if (elements.loadingIndicator) {
            elements.loadingIndicator.classList.toggle('d-none', !show);
        }
        if (elements.messagesList) {
            elements.messagesList.classList.toggle('d-none', show);
        }
    }

    /**
     * 更新空狀態顯示
     */
    function updateEmptyState() {
        if (!elements.emptyMessage || !elements.messagesList) return;

        const hasMessages = elements.messagesList.children.length > 0;
        elements.emptyMessage.classList.toggle('d-none', hasMessages);
    }

    /**
     * 顯示 Toast 通知訊息
     * @param {string} message - 訊息內容
     * @param {string} type - 類型 (success/error/warning/info)
     * @param {boolean} persistent - 是否持續顯示直到點擊
     * @param {number} duration - 自動消失時間 (毫秒)
     */
    function showToast(message, type = 'info', persistent = false, duration = 3000) {
        // 建立或取得 Toast 容器
        let container = document.getElementById('toastContainer');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toastContainer';
            container.className = 'toast-container position-fixed top-0 end-0 p-3';
            container.style.zIndex = '1100';
            document.body.appendChild(container);
        }

        const typeConfig = {
            success: { class: 'bg-success text-white', icon: '✓' },
            error: { class: 'bg-danger text-white', icon: '✗' },
            warning: { class: 'bg-warning text-dark', icon: '⚠' },
            info: { class: 'bg-info text-dark', icon: 'ℹ' }
        };

        const config = typeConfig[type] || typeConfig.info;

        const toast = document.createElement('div');
        toast.className = `toast align-items-center ${config.class} border-0`;
        toast.setAttribute('role', 'alert');
        toast.setAttribute('aria-live', 'assertive');
        toast.setAttribute('aria-atomic', 'true');
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    <span class="me-2">${config.icon}</span>
                    ${escapeHtml(message)}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="關閉"></button>
            </div>
        `;

        container.appendChild(toast);

        // 使用 Bootstrap Toast 或自訂動畫
        if (typeof bootstrap !== 'undefined' && bootstrap.Toast) {
            const bsToast = new bootstrap.Toast(toast, {
                autohide: !persistent,
                delay: duration
            });
            bsToast.show();
            
            toast.addEventListener('hidden.bs.toast', () => {
                toast.remove();
            });
        } else {
            // 備用動畫方案
            toast.classList.add('show');
            
            if (!persistent) {
                setTimeout(() => {
                    toast.classList.add('fade-out');
                    setTimeout(() => toast.remove(), 300);
                }, duration);
            }
            
            toast.querySelector('.btn-close')?.addEventListener('click', () => {
                toast.classList.add('fade-out');
                setTimeout(() => toast.remove(), 300);
            });
        }
    }

    /**
     * 顯示錯誤訊息
     * @param {string} message - 錯誤訊息
     */
    function showError(message) {
        showToast(message, 'error', false, 5000);
    }

    /**
     * 送出留言
     * @param {Event} e - 表單提交事件
     */
    async function submitMessage(e) {
        e.preventDefault();

        const content = elements.contentInput.value.trim();
        const nickname = elements.nicknameInput.value.trim();

        // 驗證
        if (!content || content.length < 1 || content.length > 300) {
            elements.contentInput.classList.add('is-invalid');
            return;
        }

        elements.contentInput.classList.remove('is-invalid');
        setSubmitLoading(true);

        try {
            const response = await fetch('/api/messages', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    content: content,
                    nickname: nickname || null
                })
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => null);
                throw new Error(errorData?.detail || `HTTP ${response.status}`);
            }

            // 成功送出，清空表單
            elements.contentInput.value = '';
            elements.nicknameInput.value = '';
            updateCharCount();

            // 留言會透過 SignalR 自動新增，不需手動插入
            console.log('留言送出成功');

        } catch (err) {
            console.error('送出留言失敗:', err);
            showError('送出留言失敗：' + err.message);
        } finally {
            setSubmitLoading(false);
        }
    }

    /**
     * 設定送出按鈕載入狀態
     * @param {boolean} loading - 是否載入中
     */
    function setSubmitLoading(loading) {
        if (!elements.submitBtn) return;

        elements.submitBtn.disabled = loading;
        const textSpan = elements.submitBtn.querySelector('.submit-text');
        const spinnerSpan = elements.submitBtn.querySelector('.spinner-border');

        if (textSpan) textSpan.classList.toggle('d-none', loading);
        if (spinnerSpan) spinnerSpan.classList.toggle('d-none', !loading);
    }

    /**
     * 更新字數計數器
     */
    function updateCharCount() {
        if (!elements.contentInput || !elements.charCount) return;
        
        const count = elements.contentInput.value.length;
        elements.charCount.textContent = count;
        
        // 超過上限時變色警示
        if (count > 300) {
            elements.charCount.classList.add('text-danger');
        } else if (count > 250) {
            elements.charCount.classList.add('text-warning');
            elements.charCount.classList.remove('text-danger');
        } else {
            elements.charCount.classList.remove('text-danger', 'text-warning');
        }
    }

    /**
     * 檢查是否已按讚（localStorage）
     * @param {string} messageId - 留言識別碼
     * @returns {boolean} 是否已按讚
     */
    function isLiked(messageId) {
        const likes = JSON.parse(localStorage.getItem('likedMessages') || '[]');
        return likes.includes(messageId);
    }

    /**
     * 設定按讚狀態（localStorage）
     * @param {string} messageId - 留言識別碼
     * @param {boolean} liked - 是否按讚
     */
    function setLiked(messageId, liked) {
        let likes = JSON.parse(localStorage.getItem('likedMessages') || '[]');
        if (liked && !likes.includes(messageId)) {
            likes.push(messageId);
        } else if (!liked) {
            likes = likes.filter(id => id !== messageId);
        }
        localStorage.setItem('likedMessages', JSON.stringify(likes));
    }

    /**
     * 格式化時間顯示
     * @param {string} dateString - ISO 日期字串
     * @returns {string} 格式化後的時間
     */
    function formatTime(dateString) {
        const date = new Date(dateString);
        const now = new Date();
        const diffMs = now - date;
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);

        if (diffMins < 1) return '剛剛';
        if (diffMins < 60) return `${diffMins} 分鐘前`;
        if (diffHours < 24) return `${diffHours} 小時前`;
        if (diffDays < 7) return `${diffDays} 天前`;

        return date.toLocaleDateString('zh-TW', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    /**
     * HTML 跳脫
     * @param {string} text - 原始文字
     * @returns {string} 跳脫後的文字
     */
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    /**
     * 綁定事件監聽器
     */
    function bindEvents() {
        // 表單提交
        if (elements.messageForm) {
            elements.messageForm.addEventListener('submit', submitMessage);
        }

        // 字數計數
        if (elements.contentInput) {
            elements.contentInput.addEventListener('input', updateCharCount);
        }

        // 重新整理按鈕
        if (elements.refreshBtn) {
            elements.refreshBtn.addEventListener('click', loadMessages);
        }

        // 按讚點擊（事件委派）
        if (elements.messagesList) {
            elements.messagesList.addEventListener('click', (e) => {
                const likeBtn = e.target.closest('.like-btn');
                if (likeBtn) {
                    const card = likeBtn.closest('.message-card');
                    if (card) {
                        handleLikeClick(card.dataset.messageId, likeBtn);
                    }
                }
            });
        }
    }

    /**
     * 處理按讚點擊
     * @param {string} messageId - 留言識別碼
     * @param {HTMLElement} button - 按鈕元素
     */
    async function handleLikeClick(messageId, button) {
        // 防止重複點擊
        if (button.disabled) return;
        button.disabled = true;

        const likeIcon = button.querySelector('.like-icon');
        const likeCount = button.querySelector('.like-count');
        
        const wasLiked = isLiked(messageId);
        const newLiked = !wasLiked;
        const oldCount = parseInt(likeCount?.textContent, 10) || 0;
        
        // 樂觀更新 UI
        setLiked(messageId, newLiked);
        if (likeIcon) likeIcon.textContent = newLiked ? '❤️' : '🤍';
        button.classList.toggle('liked', newLiked);
        if (likeCount) {
            likeCount.textContent = newLiked ? oldCount + 1 : Math.max(0, oldCount - 1);
        }
        
        try {
            // 呼叫 API
            const response = await fetch('/api/likes', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'same-origin', // 確保 Cookie 被傳送
                body: JSON.stringify({
                    messageId: messageId
                })
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const result = await response.json();
            
            // 根據伺服器回應更新狀態（以伺服器為準）
            setLiked(messageId, result.liked);
            if (likeIcon) likeIcon.textContent = result.liked ? '❤️' : '🤍';
            button.classList.toggle('liked', result.liked);
            if (likeCount) likeCount.textContent = result.likeCount;
            
            console.log(`按讚狀態更新: ${messageId} -> ${result.liked}, count: ${result.likeCount}`);

        } catch (err) {
            console.error('按讚操作失敗:', err);
            
            // 回滾 UI 狀態
            setLiked(messageId, wasLiked);
            if (likeIcon) likeIcon.textContent = wasLiked ? '❤️' : '🤍';
            button.classList.toggle('liked', wasLiked);
            if (likeCount) likeCount.textContent = oldCount;
            
            showError('按讚操作失敗，請稍後再試');
        } finally {
            button.disabled = false;
        }
    }

    /**
     * 初始化應用程式
     */
    async function init() {
        initElements();
        bindEvents();
        
        // 只在首頁初始化
        if (elements.messagesList) {
            await initSignalR();
            await loadMessages();
        }
    }

    // 公開 API
    return {
        init: init,
        loadMessages: loadMessages,
        reconnect: reconnect
    };
})();

// DOM 載入完成後初始化
document.addEventListener('DOMContentLoaded', () => {
    NoticeBoard.init();
});
