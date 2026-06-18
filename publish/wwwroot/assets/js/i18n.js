const translations = {
    vi: {
        dashboard: "Tổng Quan",
        users: "Người Dùng",
        points: "Điểm Thưởng",
        totalPoints: "Tổng Điểm",
        availablePoints: "Điểm Khả Dụng",
        lockedPoints: "Điểm Đang Khóa",
        pointHistory: "Lịch Sử Điểm",
        cardRates: "Bảng Giá Thẻ",
        exchangeCard: "Đổi Thẻ Cào",
        transactions: "Giao Dịch",
        cardTransactions: "Lịch Sử Đổi Thẻ",
        profile: "Hồ Sơ",
        login: "Đăng Nhập",
        logout: "Đăng Xuất",
        submit: "Gửi Đi",
        cancel: "Hủy",
        save: "Lưu",
        search: "Tìm kiếm",
        status: "Trạng thái",
        success: "Thành công",
        failed: "Thất bại",
        pending: "Chờ xử lý",
        processing: "Đang xử lý",
        adminAdjustPoints: "Cộng/Trừ Điểm",
        reason: "Lý do",
        amount: "Số điểm",
        note: "Ghi chú",
        confirm: "Xác nhận",
        myPoints: "Điểm Của Tôi",
        balanceManagement: "Quản Lý Điểm",
        pointLedger: "Sổ Giao Dịch Điểm",
        youWillReceive: "Bạn sẽ nhận",
        received: "Thực nhận",
        point: "điểm",
        pointsUnit: "Điểm",
        cardExchangeSuccess: "Bạn nhận được {amount} điểm tích lũy",
        adjustPointWarning: "Thao tác điểm sẽ được ghi log và không nên chỉnh sửa thủ công nếu không có lý do.",
    },
    en: {
        dashboard: "Dashboard",
        users: "Users",
        points: "Points",
        totalPoints: "Total Points",
        availablePoints: "Available Points",
        lockedPoints: "Locked Points",
        pointHistory: "Point History",
        cardRates: "Card Rates",
        exchangeCard: "Card Exchange",
        transactions: "Transactions",
        cardTransactions: "Card Exchange History",
        profile: "Profile",
        login: "Login",
        logout: "Logout",
        submit: "Submit",
        cancel: "Cancel",
        save: "Save",
        search: "Search",
        status: "Status",
        success: "Success",
        failed: "Failed",
        pending: "Pending",
        processing: "Processing",
        adminAdjustPoints: "Adjust Points",
        reason: "Reason",
        amount: "Amount",
        note: "Note",
        confirm: "Confirm",
        myPoints: "My Points",
        balanceManagement: "Point Management",
        pointLedger: "Point Ledger",
        youWillReceive: "You will receive",
        received: "Received",
        point: "points",
        pointsUnit: "Points",
        cardExchangeSuccess: "You received {amount} loyalty points",
        adjustPointWarning: "Point adjustments are logged and should not be made without a valid reason.",
    },
    zh: {
        dashboard: "仪表板",
        users: "用户",
        points: "积分",
        totalPoints: "总积分",
        availablePoints: "可用积分",
        lockedPoints: "锁定积分",
        pointHistory: "积分历史",
        cardRates: "卡费率",
        exchangeCard: "兑换卡",
        transactions: "交易",
        cardTransactions: "兑换历史",
        profile: "个人资料",
        login: "登录",
        logout: "登出",
        submit: "提交",
        cancel: "取消",
        save: "保存",
        search: "搜索",
        status: "状态",
        success: "成功",
        failed: "失败",
        pending: "待处理",
        processing: "处理中",
        adminAdjustPoints: "调整积分",
        reason: "原因",
        amount: "数量",
        note: "笔记",
        confirm: "确认",
        myPoints: "我的积分",
        balanceManagement: "积分管理",
        pointLedger: "积分分类帐",
        youWillReceive: "您将收到",
        received: "已收到",
        point: "积分",
        pointsUnit: "积分",
        cardExchangeSuccess: "您收到了 {amount} 忠诚度积分",
        adjustPointWarning: "积分调整会被记录，没有正当理由不应进行。",
    }
};

let currentLanguage = localStorage.getItem('language') || 'vi';

function setLanguage(lang) {
    currentLanguage = lang;
    localStorage.setItem('language', lang);
    // Here you would re-render the UI or reload the page
    // For simplicity in this refactor, we can assume a page reload or manual re-rendering calls.
    // A more advanced SPA would update the DOM directly.
    window.location.reload(); 
}

function _(key, replacements = {}) {
    let translation = translations[currentLanguage]?.[key] || key;
    Object.keys(replacements).forEach(rKey => {
        translation = translation.replace(`{${rKey}}`, replacements[rKey]);
    });
    return translation;
}

async function detectAndSetLanguage() {
    if (!localStorage.getItem('language')) {
        try {
            const response = await fetch('/api/localization/detect');
            if (response.ok) {
                const data = await response.json();
                currentLanguage = data.language || 'vi';
                localStorage.setItem('language', currentLanguage);
            }
        } catch (e) {
            console.error("Language detection failed, defaulting to 'vi'.", e);
            currentLanguage = 'vi';
            localStorage.setItem('language', 'vi');
        }
    }
}

// Run detection on script load
detectAndSetLanguage();