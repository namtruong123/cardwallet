(function () {
    const apiBase = '/api';
    let cardRates = [];
    let cardTransactions = [];
    let pollInterval = null;

    function getUserToken() {
        const raw = localStorage.getItem('authData');
        if (raw) {
            try {
                const auth = JSON.parse(raw);
                return auth.accessToken || auth.token || null;
            } catch {
                return null;
            }
        }

        return localStorage.getItem('token') || localStorage.getItem('accessToken');
    }

    function clearUserToken() {
        localStorage.removeItem('authData');
        localStorage.removeItem('token');
        localStorage.removeItem('accessToken');
    }

    async function userApiFetch(url, options = {}) {
        const token = getUserToken();
        const headers = new Headers(options.headers || {});

        if (!headers.has('Content-Type') && options.body) {
            headers.set('Content-Type', 'application/json');
        }

        if (token) {
            headers.set('Authorization', `Bearer ${token}`);
        }

        try {
            const response = await fetch(url, { ...options, headers });
            if (response.status === 401) {
                clearUserToken();
                window.location.href = '/Login';
                return null;
            }

            return response;
        } catch {
            showToast('Khong ket noi duoc may chu', 'error');
            return null;
        }
    }

    function formatMoney(value) {
        return new Intl.NumberFormat('vi-VN').format(Number(value || 0));
    }

    function formatCoins(value) {
        return `${formatMoney(value)} XU`;
    }

    function formatDate(value) {
        if (!value) return '';
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) return '';
        return date.toLocaleString('vi-VN');
    }

    function renderTransactionStatus(status) {
        const normalized = String(status || '').toLowerCase();
        const map = {
            pending: 'bg-amber-500/10 text-amber-400 border-amber-500/20',
            processing: 'bg-blue-500/10 text-blue-400 border-blue-500/20',
            success: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20',
            completed: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20',
            failed: 'bg-rose-500/10 text-rose-400 border-rose-500/20'
        };
        const cls = map[normalized] || 'bg-slate-500/10 text-slate-300 border-slate-500/20';
        return `<span class="px-2.5 py-1 text-[10px] font-bold uppercase rounded border ${cls}">${escapeHtml(status || 'Unknown')}</span>`;
    }

    function escapeHtml(value) {
        return String(value ?? '').replace(/[&<>"']/g, char => ({
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#39;'
        })[char]);
    }

    async function readJson(response, fallback = null) {
        if (!response) return fallback;
        try {
            return await response.json();
        } catch {
            return fallback;
        }
    }

    function showToast(message, type = 'success') {
        const container = document.getElementById('toast-container');
        if (!container) return;

        const toast = document.createElement('div');
        const tone = type === 'error'
            ? 'bg-rose-500/10 border-rose-500/20 text-rose-300'
            : 'bg-emerald-500/10 border-emerald-500/20 text-emerald-300';
        toast.className = `max-w-sm rounded-xl border px-4 py-3 shadow-xl backdrop-blur ${tone}`;
        toast.textContent = message;
        container.appendChild(toast);
        setTimeout(() => toast.remove(), 3000);
    }

    async function logoutUser() {
        const raw = localStorage.getItem('authData');
        if (raw) {
            try {
                const auth = JSON.parse(raw);
                if (auth.refreshToken) {
                    await userApiFetch(`${apiBase}/auth/logout`, {
                        method: 'POST',
                        body: JSON.stringify({ refreshToken: auth.refreshToken })
                    });
                }
            } catch {
                // Local logout must still succeed even if stored auth data is malformed.
            }
        }

        clearUserToken();
        window.location.href = '/Login';
    }

    function setupAuthForms() {
        document.getElementById('user-login-form')?.addEventListener('submit', async event => {
            event.preventDefault();
            const form = event.currentTarget;
            const response = await fetch(`${apiBase}/auth/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ login: form.login.value, password: form.password.value })
            });

            if (!response.ok) {
                showToast('Dang nhap that bai', 'error');
                return;
            }

            const auth = await response.json();
            localStorage.setItem('authData', JSON.stringify({
                accessToken: auth.accessToken,
                refreshToken: auth.refreshToken
            }));
            window.location.href = '/User/Exchange';
        });

        document.getElementById('user-register-form')?.addEventListener('submit', async event => {
            event.preventDefault();
            const form = event.currentTarget;
            if (form.password.value !== form.confirmPassword.value) {
                showToast('Mat khau khong khop', 'error');
                return;
            }

            const response = await fetch(`${apiBase}/auth/register`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    fullName: form.fullName.value,
                    phoneNumber: form.phoneNumber.value,
                    email: form.email.value,
                    password: form.password.value
                })
            });

            if (!response.ok) {
                showToast('Dang ky that bai', 'error');
                return;
            }

            const auth = await response.json();
            localStorage.setItem('authData', JSON.stringify({
                accessToken: auth.accessToken,
                refreshToken: auth.refreshToken
            }));
            window.location.href = '/User/Exchange';
        });
    }

    async function loadUserProfile() {
        const response = await userApiFetch(`${apiBase}/users/me`);
        if (!response || !response.ok) return null;

        const user = await readJson(response, {});
        setText('layout-username', user.fullName || user.email || 'User');
        setText('prof-name', user.fullName || '...');
        setText('prof-id', user.id || '...');
        setText('field-name', user.fullName || '...');
        setText('field-email', user.email || '...');
        setText('field-phone', user.phoneNumber || '...');
        setText('dash-name', `Xin chao, ${user.fullName || 'ban'}!`);

        const verified = '<span class="px-2 py-1 text-[10px] rounded bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 font-bold">Verified</span>';
        const unverified = '<span class="px-2 py-1 text-[10px] rounded bg-amber-500/10 text-amber-400 border border-amber-500/20 font-bold">Unverified</span>';
        setHtml('badge-email', user.isEmailVerified ? verified : unverified);
        setHtml('badge-phone', user.isPhoneVerified ? verified : unverified);

        if (user.status) {
            setText('layout-status', user.status);
            setText('prof-status', user.status);
        }

        return user;
    }

    async function loadWalletBalance() {
        const response = await userApiFetch(`${apiBase}/wallets/me/balance`);
        if (!response || !response.ok) return null;

        const wallet = await readJson(response, {});
        const total = Number(wallet.balance || 0);
        const locked = Number(wallet.lockedBalance || 0);
        const available = total - locked;

        setText('layout-balance', formatMoney(available));
        setText('dash-balance-avail', formatMoney(available));
        setText('dash-balance-locked', formatMoney(locked));
        setText('dash-balance-total', formatMoney(total));
        setText('wallet-avail', formatMoney(available));
        setText('wallet-locked', formatMoney(locked));
        setText('wallet-total', formatMoney(total));
        return wallet;
    }

    async function loadCardRates() {
        const response = await userApiFetch(`${apiBase}/card-rates`);
        if (!response || !response.ok) return [];

        cardRates = await readJson(response, []);
        renderRatesTable();
        setupExchangeRates();
        return cardRates;
    }

    function renderRatesTable(filter = '') {
        const body = document.getElementById('rates-body');
        if (!body) return;

        const rows = cardRates
            .filter(rate => !filter || String(rate.provider || '').toLowerCase().includes(filter.toLowerCase()))
            .map(rate => `
                <tr class="hover:bg-slate-800/30 transition-colors">
                    <td class="px-6 py-4 text-white font-bold">${escapeHtml(rate.provider)}</td>
                    <td class="px-6 py-4 text-right text-slate-300">${formatMoney(rate.faceValue)}</td>
                    <td class="px-6 py-4 text-right text-pink-400 font-bold">${formatMoney(rate.discountPercent)}%</td>
                    <td class="px-6 py-4 text-right text-cyan-400 font-bold">${formatCoins(rate.receiveAmount)}</td>
                </tr>
            `);

        body.innerHTML = rows.join('') || '<tr><td colspan="4" class="p-8 text-center text-slate-500">Khong co bang gia</td></tr>';
    }

    function setupExchangeRates() {
        const providerSelect = document.getElementById('ex-provider');
        const valueSelect = document.getElementById('ex-facevalue');
        if (!providerSelect || !valueSelect) return;

        const providers = [...new Set(cardRates.filter(rate => rate.isActive !== false).map(rate => rate.provider).filter(Boolean))];
        providerSelect.innerHTML = '<option value="">Chon nha mang</option>' + providers.map(provider => `<option value="${escapeHtml(provider)}">${escapeHtml(provider)}</option>`).join('');
        valueSelect.innerHTML = '<option value="">Chon nha mang truoc</option>';

        providerSelect.onchange = () => {
            const values = cardRates
                .filter(rate => rate.provider === providerSelect.value && rate.isActive !== false)
                .sort((a, b) => Number(a.faceValue || 0) - Number(b.faceValue || 0));
            valueSelect.disabled = !providerSelect.value;
            valueSelect.innerHTML = '<option value="">Chon menh gia</option>' + values.map(rate => `<option value="${Number(rate.faceValue || 0)}">${formatMoney(rate.faceValue)}</option>`).join('');
            updateExchangePreview();
        };

        valueSelect.onchange = updateExchangePreview;
        updateExchangePreview();
    }

    function updateExchangePreview() {
        const provider = document.getElementById('ex-provider')?.value;
        const faceValue = Number(document.getElementById('ex-facevalue')?.value || 0);
        const rate = cardRates.find(item => item.provider === provider && Number(item.faceValue) === faceValue);
        setText('ex-discount', rate ? `${formatMoney(rate.discountPercent)}%` : '0%');
        setText('ex-receive', rate ? formatCoins(rate.receiveAmount) : '0 XU');
    }

    async function submitCardExchange(event) {
        event?.preventDefault();
        const button = document.getElementById('ex-submit-btn');
        if (button) button.disabled = true;

        const payload = {
            provider: document.getElementById('ex-provider')?.value,
            faceValue: Number(document.getElementById('ex-facevalue')?.value || 0),
            serial: document.getElementById('ex-serial')?.value.trim(),
            cardCode: document.getElementById('ex-code')?.value.trim()
        };

        const response = await userApiFetch(`${apiBase}/card-exchange/submit`, {
            method: 'POST',
            body: JSON.stringify(payload)
        });

        if (button) button.disabled = false;
        if (!response || !response.ok) {
            showToast('Gui the that bai', 'error');
            return;
        }

        const transaction = await readJson(response, {});
        document.getElementById('exchange-form')?.reset();
        updateExchangePreview();
        renderExchangeResult(transaction);
        pollCardExchangeStatus(transaction.id);
        showToast('Da gui the len he thong');
    }

    function renderExchangeResult(transaction) {
        const target = document.getElementById('exchange-result');
        if (!target) return;
        target.classList.remove('hidden');
        target.innerHTML = `
            <div class="bg-cardbg border border-cyan-500/30 rounded-2xl p-6 shadow-lg">
                <div class="flex justify-between items-center mb-4">
                    <h4 class="text-white font-bold text-lg">Ket qua giao dich</h4>
                    ${renderTransactionStatus(transaction.status)}
                </div>
                <div class="space-y-2 text-sm">
                    <p class="text-slate-400">Ma GD: <span class="text-slate-200 font-mono">${escapeHtml(transaction.id)}</span></p>
                    <p class="text-slate-400">Du kien nhan: <span class="text-cyan-400 font-bold">${formatCoins(transaction.expectedReceiveAmount || transaction.actualReceiveAmount)}</span></p>
                    <p class="text-slate-400">${escapeHtml(transaction.message || transaction.failureReason || '')}</p>
                </div>
            </div>`;
    }

    function pollCardExchangeStatus(id) {
        if (!id) return;
        if (pollInterval) clearInterval(pollInterval);
        pollInterval = setInterval(async () => {
            const response = await userApiFetch(`${apiBase}/card-exchange/${id}/status`);
            if (!response || !response.ok) return;
            const status = await readJson(response, {});
            renderExchangeResult(status);
            if (status.status === 'Success' || status.status === 'Failed') {
                clearInterval(pollInterval);
                loadWalletBalance();
            }
        }, 5000);
    }

    async function loadCardExchangeTransactions() {
        const response = await userApiFetch(`${apiBase}/card-exchange/my-transactions`);
        if (!response || !response.ok) return [];

        cardTransactions = await readJson(response, []);
        setText('dash-tx-total', String(cardTransactions.length));
        renderCardTransactions();
        renderRecentCardTransactions();
        return cardTransactions;
    }

    function renderRecentCardTransactions() {
        const body = document.getElementById('dash-recent-txs');
        if (!body) return;

        body.innerHTML = cardTransactions.slice(0, 5).map(transaction => `
            <tr class="hover:bg-slate-800/30 transition-colors">
                <td class="px-6 py-4"><p class="text-white font-bold text-sm">${escapeHtml(transaction.provider)}</p><p class="text-xs text-slate-500 font-mono">ID: ${escapeHtml(String(transaction.id || '').slice(0, 8))}</p></td>
                <td class="px-6 py-4 text-right text-slate-300 font-medium text-sm">${formatMoney(transaction.faceValue)}</td>
                <td class="px-6 py-4 text-center">${renderTransactionStatus(transaction.status)}</td>
                <td class="px-6 py-4 text-right text-xs text-slate-400">${formatDate(transaction.createdAt)}</td>
            </tr>`).join('') || '<tr><td class="p-8 text-center text-slate-500">Chua co giao dich</td></tr>';
    }

    function renderCardTransactions() {
        const body = document.getElementById('card-txs-body');
        if (!body) return;

        const query = document.getElementById('tx-search')?.value.toLowerCase() || '';
        const status = document.getElementById('tx-status')?.value || '';
        const filtered = cardTransactions.filter(transaction => {
            const searchable = `${transaction.provider || ''} ${transaction.serial || ''} ${transaction.cardCode || ''}`.toLowerCase();
            return (!query || searchable.includes(query)) && (!status || transaction.status === status);
        });

        body.innerHTML = filtered.map(transaction => `
            <tr class="hover:bg-slate-800/30 transition-colors">
                <td class="px-6 py-4"><p class="text-white font-bold text-sm">${escapeHtml(transaction.provider)}</p><p class="text-xs text-slate-500 font-mono">S: ${escapeHtml(transaction.serial)} | P: ${escapeHtml(transaction.cardCode)}</p></td>
                <td class="px-6 py-4 text-right text-slate-300 font-medium text-sm">${formatMoney(transaction.faceValue)}</td>
                <td class="px-6 py-4 text-right text-cyan-400 font-bold text-sm">+${formatMoney(transaction.expectedReceiveAmount || transaction.actualReceiveAmount)}</td>
                <td class="px-6 py-4 text-center">${renderTransactionStatus(transaction.status)}</td>
                <td class="px-6 py-4 text-right text-xs text-slate-400">${formatDate(transaction.createdAt)}</td>
            </tr>`).join('') || '<tr><td class="p-8 text-center text-slate-500">Khong co giao dich doi the</td></tr>';
    }

    async function loadWalletTransactions() {
        const body = document.getElementById('wallet-txs-body');
        if (!body) return [];

        const response = await userApiFetch(`${apiBase}/wallets/me/transactions`);
        if (!response || response.status === 404) {
            body.innerHTML = '<tr><td colspan="5" class="p-8 text-center text-slate-500">Chua co API /api/wallets/me/transactions, khong fake du lieu.</td></tr>';
            return [];
        }

        if (!response.ok) {
            body.innerHTML = '<tr><td colspan="5" class="p-8 text-center text-slate-500">Khong tai duoc lich su vi.</td></tr>';
            return [];
        }

        const transactions = await readJson(response, []);
        body.innerHTML = transactions.map(transaction => `
            <tr class="hover:bg-slate-800/30 transition-colors">
                <td class="px-6 py-4 text-xs font-mono text-slate-500">${escapeHtml(String(transaction.id || '').slice(0, 8))}</td>
                <td class="px-6 py-4 text-xs font-bold text-slate-300">${escapeHtml(transaction.type)}</td>
                <td class="px-6 py-4 text-right font-bold text-sm ${Number(transaction.amount) >= 0 ? 'text-emerald-400' : 'text-rose-400'}">${formatMoney(transaction.amount)}</td>
                <td class="px-6 py-4 text-xs text-slate-400">${escapeHtml(transaction.description)}</td>
                <td class="px-6 py-4 text-right text-xs text-slate-500">${formatDate(transaction.createdAt)}</td>
            </tr>`).join('') || '<tr><td colspan="5" class="p-8 text-center text-slate-500">Chua co giao dich vi</td></tr>';
        return transactions;
    }

    function setText(id, value) {
        const element = document.getElementById(id);
        if (element) element.textContent = value;
    }

    function setHtml(id, value) {
        const element = document.getElementById(id);
        if (element) element.innerHTML = value;
    }

    function wirePage() {
        const path = window.location.pathname.toLowerCase();
        document.querySelectorAll('#user-sidebar-menu a[href]').forEach(link => {
            const href = link.getAttribute('href').toLowerCase();
            if (href === path || (path === '/user/' && href === '/user')) {
                link.classList.add('bg-slate-800/80', 'text-white');
                link.classList.remove('text-slate-400');
            }
        });

        if (path.includes('/login') || path.includes('/register')) {
            setupAuthForms();
            return;
        }

        if (!path.startsWith('/user')) return;
        if (!getUserToken()) {
            window.location.href = '/Login';
            return;
        }

        document.getElementById('user-logout-btn')?.addEventListener('click', logoutUser);
        loadUserProfile();
        loadWalletBalance();

        if (path === '/user' || path === '/user/') {
            loadCardExchangeTransactions();
        } else if (path.includes('/exchange')) {
            loadCardRates();
            document.getElementById('exchange-form')?.addEventListener('submit', submitCardExchange);
        } else if (path.includes('/cardrates')) {
            loadCardRates();
            document.getElementById('rate-search')?.addEventListener('input', event => renderRatesTable(event.target.value));
        } else if (path.includes('/wallet')) {
            loadWalletTransactions();
        } else if (path.includes('/transactions')) {
            loadCardExchangeTransactions();
            loadWalletTransactions();
            document.getElementById('tx-search')?.addEventListener('input', renderCardTransactions);
            document.getElementById('tx-status')?.addEventListener('change', renderCardTransactions);
        }
    }

    window.getUserToken = getUserToken;
    window.userApiFetch = userApiFetch;
    window.formatMoney = formatMoney;
    window.formatCoins = formatCoins;
    window.renderTransactionStatus = renderTransactionStatus;
    window.loadUserProfile = loadUserProfile;
    window.loadWalletBalance = loadWalletBalance;
    window.loadCardRates = loadCardRates;
    window.submitCardExchange = submitCardExchange;
    window.loadCardExchangeTransactions = loadCardExchangeTransactions;
    window.loadWalletTransactions = loadWalletTransactions;

    document.addEventListener('DOMContentLoaded', wirePage);
})();
