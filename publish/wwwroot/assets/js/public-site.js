(function () {
    function formatMoney(value) {
        return new Intl.NumberFormat('vi-VN').format(Number(value || 0));
    }

    function formatCoins(value) {
        return `${formatMoney(value)} xu`;
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

    async function loadPublicRates() {
        const body = document.getElementById('public-rates-body');
        if (!body) return;

        body.innerHTML = '<tr><td colspan="4" class="px-6 py-8 text-center text-slate-400">Đang tải bảng giá...</td></tr>';

        try {
            const response = await fetch('/api/card-rates');
            if (!response.ok) throw new Error('Cannot load rates');
            const rates = await response.json();

            if (!Array.isArray(rates) || rates.length === 0) {
                body.innerHTML = '<tr><td colspan="4" class="px-6 py-8 text-center text-slate-400">Chưa có bảng giá đang hoạt động.</td></tr>';
                return;
            }

            body.innerHTML = rates.map(rate => `
                <tr class="hover:bg-slate-800/30 transition-colors">
                    <td class="px-6 py-4 text-white font-bold">${escapeHtml(rate.provider)}</td>
                    <td class="px-6 py-4 text-right text-slate-300">${formatMoney(rate.faceValue)} VND</td>
                    <td class="px-6 py-4 text-right text-pink-400 font-bold">${formatMoney(rate.discountPercent)}%</td>
                    <td class="px-6 py-4 text-right text-cyan-400 font-bold">${formatCoins(rate.receiveAmount)}</td>
                </tr>
            `).join('');
        } catch {
            body.innerHTML = '<tr><td colspan="4" class="px-6 py-8 text-center text-rose-400">Không thể tải bảng giá. Vui lòng thử lại sau.</td></tr>';
        }
    }

    document.addEventListener('DOMContentLoaded', loadPublicRates);
})();
