/**
 * NoticeBoard 管理後台 JavaScript
 * 處理留言管理操作（隱藏/恢復/刪除）及即時更新
 */

(function () {
    'use strict';

    // API 端點
    const API = {
        messages: '/api/admin/messages',
        hide: (id) => `/api/admin/messages/${id}/hide`,
        restore: (id) => `/api/admin/messages/${id}/restore`,
        delete: (id) => `/api/admin/messages/${id}`,
        cleanup: '/api/admin/cleanup'
    };

    // DOM 元素
    let toastEl, toast, confirmModal, confirmModalBtn;
    let pendingAction = null;

    /**
     * 初始化
     */
    function init() {
        // 初始化 Toast
        toastEl = document.getElementById('adminToast');
        if (toastEl) {
            toast = new bootstrap.Toast(toastEl, { delay: 3000 });
        }

        // 初始化確認對話框
        const confirmModalEl = document.getElementById('confirmModal');
        if (confirmModalEl) {
            confirmModal = new bootstrap.Modal(confirmModalEl);
            confirmModalBtn = document.getElementById('confirmModalBtn');
            confirmModalBtn?.addEventListener('click', handleConfirm);
        }

        // 綁定事件
        bindEvents();
    }

    /**
     * 綁定事件處理器
     */
    function bindEvents() {
        // 操作按鈕（隱藏/恢復/刪除）
        document.querySelectorAll('.action-btn').forEach(btn => {
            btn.addEventListener('click', handleActionClick);
        });

        // 篩選器按鈕
        document.querySelectorAll('.filter-btn').forEach(btn => {
            btn.addEventListener('click', handleFilterClick);
        });

        // 清理按鈕
        document.getElementById('cleanupBtn')?.addEventListener('click', handleCleanupClick);
    }

    /**
     * 處理操作按鈕點擊
     */
    function handleActionClick(e) {
        const btn = e.currentTarget;
        const action = btn.dataset.action;
        const id = btn.dataset.id;

        // 設定待執行操作
        pendingAction = { action, id };

        // 顯示確認對話框
        const messages = {
            hide: '確定要隱藏這則留言嗎？隱藏後前台將不再顯示。',
            restore: '確定要恢復這則留言嗎？恢復後將在前台顯示。',
            delete: '確定要刪除這則留言嗎？此操作無法復原！'
        };

        document.getElementById('confirmModalTitle').textContent = getActionTitle(action);
        document.getElementById('confirmModalBody').textContent = messages[action];
        document.getElementById('confirmModalBtn').className = `btn btn-${action === 'delete' ? 'danger' : action === 'hide' ? 'warning' : 'success'}`;

        confirmModal.show();
    }

    /**
     * 處理確認按鈕點擊
     */
    async function handleConfirm() {
        if (!pendingAction) return;

        const { action, id } = pendingAction;
        confirmModal.hide();

        try {
            let response;
            switch (action) {
                case 'hide':
                    response = await fetch(API.hide(id), { method: 'POST' });
                    break;
                case 'restore':
                    response = await fetch(API.restore(id), { method: 'POST' });
                    break;
                case 'delete':
                    response = await fetch(API.delete(id), { method: 'DELETE' });
                    break;
            }

            if (response.ok) {
                showToast('success', `${getActionTitle(action)}成功`);
                // 重新載入頁面以更新列表
                setTimeout(() => location.reload(), 1000);
            } else {
                const error = await response.json();
                showToast('danger', error.detail || '操作失敗');
            }
        } catch (err) {
            console.error('操作錯誤:', err);
            showToast('danger', '操作失敗，請稍後再試');
        }

        pendingAction = null;
    }

    /**
     * 處理篩選器點擊
     */
    function handleFilterClick(e) {
        const btn = e.currentTarget;
        const status = btn.dataset.status;

        // 更新按鈕狀態
        document.querySelectorAll('.filter-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');

        // 篩選留言卡片
        document.querySelectorAll('.message-card').forEach(card => {
            if (status === 'all' || card.dataset.status === status) {
                card.style.display = '';
            } else {
                card.style.display = 'none';
            }
        });
    }

    /**
     * 處理清理按鈕點擊
     */
    async function handleCleanupClick() {
        pendingAction = { action: 'cleanup' };

        document.getElementById('confirmModalTitle').textContent = '清理過期資料';
        document.getElementById('confirmModalBody').textContent = '確定要清理 24 小時以上的過期資料嗎？';
        document.getElementById('confirmModalBtn').className = 'btn btn-warning';

        // 重新綁定確認按鈕事件
        const oldBtn = document.getElementById('confirmModalBtn');
        const newBtn = oldBtn.cloneNode(true);
        oldBtn.parentNode.replaceChild(newBtn, oldBtn);
        newBtn.addEventListener('click', handleCleanupConfirm);

        confirmModal.show();
    }

    /**
     * 處理清理確認
     */
    async function handleCleanupConfirm() {
        confirmModal.hide();

        try {
            const response = await fetch(API.cleanup, { method: 'POST' });

            if (response.ok) {
                const result = await response.json();
                showToast('success', `清理完成：刪除 ${result.messagesDeleted} 則留言、${result.likesDeleted} 則按讚記錄`);
                setTimeout(() => location.reload(), 2000);
            } else {
                const error = await response.json();
                showToast('danger', error.detail || '清理失敗');
            }
        } catch (err) {
            console.error('清理錯誤:', err);
            showToast('danger', '清理失敗，請稍後再試');
        }

        // 重新綁定原本的確認事件
        const oldBtn = document.getElementById('confirmModalBtn');
        const newBtn = oldBtn.cloneNode(true);
        oldBtn.parentNode.replaceChild(newBtn, oldBtn);
        document.getElementById('confirmModalBtn').addEventListener('click', handleConfirm);
    }

    /**
     * 取得操作標題
     */
    function getActionTitle(action) {
        const titles = {
            hide: '隱藏留言',
            restore: '恢復留言',
            delete: '刪除留言',
            cleanup: '清理資料'
        };
        return titles[action] || '操作';
    }

    /**
     * 顯示 Toast 通知
     */
    function showToast(type, message) {
        if (!toast) return;

        const toastEl = document.getElementById('adminToast');
        toastEl.className = `toast bg-${type} text-white`;
        document.getElementById('toastTitle').textContent = type === 'success' ? '成功' : '錯誤';
        document.getElementById('toastBody').textContent = message;
        toast.show();
    }

    // 頁面載入後初始化
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
