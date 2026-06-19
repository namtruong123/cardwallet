(function () {
    const apiBase = '/api/admin';
    const TOTAL_SUPPLY_POINTS = 50000000;

    function showToast(message, type = 'success') {
        const container = document.getElementById('toast-container');
        if (!container) return;
        const toast = document.createElement('div');
        const bgColor = type === 'success' ? 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400' : 'bg-rose-500/10 border-rose-500/20 text-rose-400';
        const icon = type === 'success' ? 'fa-check-circle' : 'fa-exclamation-circle';
        toast.className = `flex items-center w-full max-w-xs p-3.5 mb-2 border rounded-xl shadow-lg backdrop-blur-md transform transition-all duration-300 translate-x-full ${bgColor}`;
        toast.innerHTML = `
            <div class="inline-flex items-center justify-center flex-shrink-0 w-8 h-8 rounded-lg bg-darkbg/50">
                <i class="fas ${icon} text-lg"></i>
            </div>
            <div class="ms-3 text-sm font-medium pr-2">${message}</div>
            <button type="button" class="ms-auto -mx-1.5 -my-1.5 rounded-lg focus:ring-2 focus:ring-slate-400 p-1.5 inline-flex items-center justify-center h-8 w-8 opacity-70 hover:opacity-100 text-slate-400 hover:text-white transition-colors" onclick="this.parentElement.remove()">
                <i class="fas fa-times"></i>
            </button>
        `;
        container.appendChild(toast);
        setTimeout(() => toast.classList.remove('translate-x-full'), 10);
        setTimeout(() => {
            toast.classList.add('translate-x-full');
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }

    function redirectToMainLogin() {
        window.location.href = '/login';
    }

    function getAdminSessionCookie() {
        const match = document.cookie.match(/(?:^|;\s*)adminAccessToken=([^;]+)/);
        return match ? decodeURIComponent(match[1]) : '';
    }

    function setAdminSessionCookie(token) {
        const secure = window.location.protocol === 'https:' ? '; Secure' : '';
        document.cookie = `adminAccessToken=${encodeURIComponent(token)}; Path=/; SameSite=Lax${secure}`;
    }

    function hasActiveAdminTabSession() {
        return sessionStorage.getItem('adminSessionActive') === '1';
    }

    function clearAdminSession() {
        sessionStorage.removeItem('adminSessionActive');
        localStorage.removeItem('adminToken');
        localStorage.removeItem('adminUser');
        document.cookie = 'adminAccessToken=; Path=/; Max-Age=0; SameSite=Lax';
    }

    async function apiFetch(url, options = {}) {
        if (!hasActiveAdminTabSession()) {
            clearAdminSession();
            redirectToMainLogin();
            return null;
        }

        const token = localStorage.getItem('adminToken') || getAdminSessionCookie();
        if (token && !localStorage.getItem('adminToken')) {
            localStorage.setItem('adminToken', token);
        }
        if (!options.headers) options.headers = {};
        if (!options.headers['Content-Type'] && !(options.body instanceof FormData)) {
            options.headers['Content-Type'] = 'application/json';
        }
        if (token) options.headers['Authorization'] = `Bearer ${token}`;
        try {
            const response = await fetch(url, options);
            if (response.status === 401 && !url.includes('/api/auth/login')) {
                clearAdminSession();
                redirectToMainLogin();
                return null;
            }
            return response;
        } catch (error) {
            showToast('Lỗi kết nối tới máy chủ', 'error');
            return null;
        }
    }

    function formatVN(amount) {
        return new Intl.NumberFormat('vi-VN').format(amount);
    }

    function formatShortDate(dateStr) {
        if (!dateStr) return '';
        const d = new Date(dateStr);
        return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')} ${d.getDate().toString().padStart(2, '0')}/${(d.getMonth()+1).toString().padStart(2, '0')}`;
    }

    function getCurrentAdmin() {
        try {
            return JSON.parse(localStorage.getItem('adminUser') || 'null');
        } catch {
            return null;
        }
    }

    function hydrateCurrentAdminHeader() {
        const admin = getCurrentAdmin();
        if (!admin) return;

        const displayName = admin.fullName || admin.email || 'Admin';
        const role = admin.role || 'Admin';
        const avatar = document.getElementById('admin-current-avatar');
        const name = document.getElementById('admin-current-name');
        const roleEl = document.getElementById('admin-current-role');

        if (avatar) {
            avatar.src = `https://ui-avatars.com/api/?name=${encodeURIComponent(displayName)}&background=0D8ABC&color=fff`;
            avatar.alt = displayName;
        }
        if (name) name.textContent = displayName;
        if (roleEl) roleEl.textContent = role;
    }

    async function readApiError(response, fallback) {
        if (!response) return fallback;

        try {
            const contentType = response.headers.get('content-type') || '';
            if (contentType.includes('application/json')) {
                const body = await response.json();
                if (body?.message) return body.message;
                if (body?.title) return body.title;
                if (body?.errors) {
                    const firstErrors = Object.values(body.errors).flat();
                    if (firstErrors.length > 0) return firstErrors[0];
                }
            }

            const text = await response.text();
            return text || fallback;
        } catch {
            return fallback;
        }
    }

    function validateAdminUserForm(form, isUpdate) {
        const fullName = form.fullName.value.trim();
        const email = form.email.value.trim().toLowerCase();
        const phoneNumber = form.phoneNumber.value.trim();
        const password = form.password.value;
        const parentUserId = form.parentUserId?.value.trim() || '';

        if (!fullName) return 'Họ tên không được để trống';
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) return 'Email không hợp lệ';
        if (!/^\d{9,11}$/.test(phoneNumber)) return 'Số điện thoại phải có 9-11 chữ số';
        if (!isUpdate && (!password || password.length < 6)) return 'Mật khẩu phải có tối thiểu 6 ký tự';
        if (parentUserId && !/^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(parentUserId)) return 'User ID quản lý trực tiếp không hợp lệ';

        return '';
    }

    const roleLabels = {
        Admin: 'ADMIN - full quyền',
        CentralManager: 'Nhóm 1 - QL tập trung',
        PartnerOrg: 'Nhóm 2 - Đối tác/Tổ chức',
        Collaborator: 'Nhóm 3 - CTV',
        Customer: 'CTV'
    };

    const rolePermissionPresets = {
        Admin: ['canManageUsers', 'canManageTasks', 'canApproveTasks', 'canApproveKycWithdraw', 'canTransferPoints', 'canManageBlog', 'canExportReports'],
        CentralManager: ['canManageUsers', 'canManageTasks', 'canApproveTasks', 'canTransferPoints', 'canManageBlog'],
        PartnerOrg: ['canManageUsers', 'canManageTasks', 'canApproveTasks', 'canTransferPoints', 'canManageBlog'],
        Collaborator: [],
        Customer: []
    };

    function applyRolePreset(form) {
        const role = form.role?.value || 'Customer';
        const preset = rolePermissionPresets[role] || [];
        form.querySelectorAll('.permission-checkbox').forEach(input => {
            input.checked = preset.includes(input.name);
        });
    }

    function setActiveMenu() {
        const path = window.location.pathname.toLowerCase();
        document.querySelectorAll('#sidebar-menu a').forEach(link => {
            const href = link.getAttribute('href').toLowerCase();
            link.classList.remove('bg-slate-800/80', 'text-cyan-400', 'border-r-2', 'border-cyan-400');
            if (path === href || (href !== '/' && href !== '/admin' && path.startsWith(href))) {
                link.classList.add('bg-slate-800/80', 'text-cyan-400', 'border-r-2', 'border-cyan-400');
                link.classList.remove('text-slate-400');
                const icon = link.querySelector('i');
                if(icon) { icon.classList.remove('text-slate-500'); icon.classList.add('text-cyan-400'); }
            }
        });
    }

    window.closeModal = function(id) {
        document.getElementById(id)?.classList.add('hidden');
    }

    // Users
    let usersState = { page: 1, pageSize: 20, keyword: '', status: '', isPhoneVerified: '', isEmailVerified: '', totalPages: 1, items: [] };
    
    async function fetchUsers() {
        const tableBody = document.getElementById('users-table-body');
        if (!tableBody) return;
        
        tableBody.innerHTML = '<tr><td colspan="7" class="px-5 py-8 text-center text-slate-500"><i class="fas fa-spinner fa-spin mr-2"></i> Đang tải...</td></tr>';
        
        const q = new URLSearchParams({
            page: usersState.page,
            pageSize: usersState.pageSize
        });
        if (usersState.keyword) q.set('keyword', usersState.keyword);
        if (usersState.status) q.set('status', usersState.status);
        if (usersState.isPhoneVerified !== '') q.set('phoneVerified', usersState.isPhoneVerified);
        if (usersState.isEmailVerified !== '') q.set('emailVerified', usersState.isEmailVerified);

        const response = await apiFetch(`${apiBase}/users?${q.toString()}`);
        if (response && response.ok) {
            const data = await response.json();
            usersState.totalPages = data.totalPages || 1;
            renderUsersTable(data.items, data.totalCount);
            renderUserPagination();
        } else {
            tableBody.innerHTML = '<tr><td colspan="7" class="px-5 py-8 text-center text-rose-400">Lỗi lấy dữ liệu</td></tr>';
        }
    }

    function renderUsersTable(users, totalCount) {
        const tableBody = document.getElementById('users-table-body');
        closeUserActionMenu();
        if (!users || users.length === 0) {
            usersState.items = [];
            tableBody.innerHTML = '<tr><td colspan="7" class="px-5 py-8 text-center text-slate-500">Không tìm thấy dữ liệu.</td></tr>';
            document.getElementById('user-pagination-info').textContent = 'Hiển thị 0 - 0 của 0';
            return;
        }
        usersState.items = users;
        
        const start = (usersState.page - 1) * usersState.pageSize + 1;
        const end = start + users.length - 1;
        document.getElementById('user-pagination-info').textContent = `Hiển thị ${start} - ${end} của ${totalCount}`;
        const currentAdminId = getCurrentAdmin()?.userId;

        tableBody.innerHTML = users.map(u => {
            let sBadge = '<span class="px-2 py-1 text-[10px] uppercase rounded bg-slate-800 text-slate-400 border border-slate-700">Inactive</span>';
            if (u.status === 'Active') sBadge = '<span class="px-2 py-1 text-[10px] uppercase rounded bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">Active</span>';
            if (u.status === 'Locked') sBadge = '<span class="px-2 py-1 text-[10px] uppercase rounded bg-rose-500/10 text-rose-400 border border-rose-500/20">Locked</span>';

            const pIcon = u.isPhoneVerified ? '<i class="fas fa-check-circle text-emerald-500" title="Đã XN SĐT"></i>' : '<i class="fas fa-exclamation-circle text-amber-500" title="Chưa XN"></i>';
            const eIcon = u.isEmailVerified ? '<i class="fas fa-check-circle text-emerald-500" title="Đã XN Email"></i>' : '<i class="fas fa-exclamation-circle text-amber-500" title="Chưa XN"></i>';
            const parentLabel = getParentDisplayLabel(u);
            const isCurrentAdmin = currentAdminId && u.id === currentAdminId;
            const rowClass = isCurrentAdmin
                ? 'bg-cyan-500/10 ring-1 ring-inset ring-cyan-400/40'
                : 'hover:bg-slate-800/30';

            return `
            <tr class="${rowClass} transition-colors border-b border-borderbg/50 last:border-0">
                <td class="px-5 py-3">
                    <div class="font-medium text-slate-200 text-sm">${u.fullName}</div>
                    <div class="font-mono text-[11px] text-slate-500 mt-0.5" title="${u.id}">${u.id.substring(0,8)}...</div>
                </td>
                <td class="px-5 py-3 text-[13px] text-slate-400">
                    <div class="flex items-center gap-2">${pIcon} ${u.phoneNumber}</div>
                    <div class="flex items-center gap-2 mt-1">${eIcon} ${u.email}</div>
                </td>
                <td class="px-5 py-3 text-[12px] text-slate-300">
                    <div class="font-semibold text-cyan-300">${roleLabels[u.role] || u.role || 'CTV'}</div>
                    <div class="text-[11px] text-slate-500 mt-1">${parentLabel}</div>
                </td>
                <td class="px-5 py-3 text-right font-bold text-cyan-400 text-sm">${formatVN(u.walletBalance)}</td>
                <td class="px-5 py-3 text-center">${sBadge}</td>
                <td class="px-5 py-3 text-[12px] text-slate-400">${formatShortDate(u.createdAt)}</td>
                <td class="px-5 py-3 text-right text-sm">
                    <button type="button" onclick="toggleUserActionMenu(event, '${u.id}')" class="inline-flex h-8 w-8 items-center justify-center rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors" aria-label="Mở menu hành động">
                        <i class="fas fa-ellipsis-v"></i>
                    </button>
                </td>
            </tr>`;
        }).join('');
    }

    function getParentDisplayLabel(user) {
        if (!user.parentUserId) return 'Không có quản lý trực tiếp';
        if (!user.parentFullName) return 'Đã gán quản lý nhưng chưa tìm thấy tài khoản';

        const role = user.parentRole || '';
        let prefix = 'QL';
        if (role === 'PartnerOrg') prefix = 'Đối tác';
        if (role === 'Organization') prefix = 'Tổ chức';
        if (role === 'CentralManager') prefix = 'QL';

        return `${prefix}: ${user.parentFullName}`;
    }

    function getUserActionMenu() {
        let menu = document.getElementById('user-action-menu');
        if (menu) return menu;

        menu = document.createElement('div');
        menu.id = 'user-action-menu';
        menu.className = 'fixed z-[9999] hidden w-44 overflow-hidden rounded-lg border border-borderbg bg-cardbg shadow-2xl shadow-black/40';
        document.body.appendChild(menu);
        return menu;
    }

    function closeUserActionMenu() {
        const menu = document.getElementById('user-action-menu');
        if (menu) menu.classList.add('hidden');
    }

    function positionUserActionMenu(button, menu) {
        const rect = button.getBoundingClientRect();
        const menuWidth = 176;
        const menuHeight = menu.offsetHeight || 230;
        const gap = 8;
        const viewportPadding = 12;

        let top = rect.bottom + gap;
        if (top + menuHeight > window.innerHeight - viewportPadding) {
            top = Math.max(viewportPadding, rect.top - menuHeight - gap);
        }

        let left = rect.right - menuWidth;
        left = Math.max(viewportPadding, Math.min(left, window.innerWidth - menuWidth - viewportPadding));

        menu.style.top = `${top}px`;
        menu.style.left = `${left}px`;
    }

    window.toggleUserActionMenu = function(event, userId) {
        event.preventDefault();
        event.stopPropagation();

        const user = usersState.items.find(x => x.id === userId);
        if (!user) return;

        const menu = getUserActionMenu();
        const lockButton = user.status === 'Locked'
            ? `<button onclick="closeUserActionMenu(); actionUser('${user.id}', 'unlock')" class="w-full text-left px-3 py-2 text-xs text-slate-300 hover:bg-slate-800 hover:text-emerald-400"><i class="fas fa-unlock w-4"></i> Mở khóa</button>`
            : `<button onclick="closeUserActionMenu(); actionUser('${user.id}', 'lock')" class="w-full text-left px-3 py-2 text-xs text-slate-300 hover:bg-slate-800 hover:text-rose-400"><i class="fas fa-lock w-4"></i> Khóa account</button>`;

        menu.innerHTML = `
            <button onclick="closeUserActionMenu(); openUserDetail('${user.id}')" class="w-full text-left px-3 py-2 text-xs text-slate-300 hover:bg-slate-800 hover:text-cyan-400"><i class="fas fa-eye w-4"></i> Chi tiết</button>
            <button id="user-action-edit" class="w-full text-left px-3 py-2 text-xs text-slate-300 hover:bg-slate-800 hover:text-blue-400"><i class="fas fa-edit w-4"></i> Sửa</button>
            <button onclick="closeUserActionMenu(); openResetPwdModal('${user.id}', '${String(user.fullName || '').replace(/'/g, "\\'")}')" class="w-full text-left px-3 py-2 text-xs text-slate-300 hover:bg-slate-800 hover:text-amber-400"><i class="fas fa-key w-4"></i> Reset mật khẩu</button>
            ${lockButton}
            <div class="border-t border-borderbg my-1"></div>
            <button onclick="closeUserActionMenu(); actionUser('${user.id}', 'delete')" class="w-full text-left px-3 py-2 text-xs text-rose-400 hover:bg-rose-500/20"><i class="fas fa-ban w-4"></i> Vô hiệu hóa</button>
        `;
        menu.querySelector('#user-action-edit')?.addEventListener('click', () => {
            closeUserActionMenu();
            openUserModal(user);
        });

        menu.classList.remove('hidden');
        positionUserActionMenu(event.currentTarget, menu);
    };

    window.closeUserActionMenu = closeUserActionMenu;

    document.addEventListener('click', closeUserActionMenu);
    window.addEventListener('resize', closeUserActionMenu);
    window.addEventListener('scroll', closeUserActionMenu, true);

    function renderUserPagination() {
        let html = '';
        if(usersState.page > 1) html += `<button onclick="changeUserPage(${usersState.page - 1})" class="px-2.5 py-1 bg-slate-800 text-slate-300 hover:bg-slate-700 rounded text-xs"><i class="fas fa-chevron-left"></i></button>`;
        html += `<span class="px-3 py-1 bg-slate-800 text-white rounded text-xs font-medium border border-borderbg">${usersState.page} / ${usersState.totalPages}</span>`;
        if(usersState.page < usersState.totalPages) html += `<button onclick="changeUserPage(${usersState.page + 1})" class="px-2.5 py-1 bg-slate-800 text-slate-300 hover:bg-slate-700 rounded text-xs"><i class="fas fa-chevron-right"></i></button>`;
        document.getElementById('user-pagination-controls').innerHTML = html;
    }

    window.changeUserPage = function(p) { usersState.page = p; fetchUsers(); };

    window.openUserModal = function(u = null) {
        const f = document.getElementById('admin-user-form');
        f.reset(); f.id.value = '';
        document.getElementById('user-modal-title').textContent = u ? 'Cập Nhật Tài Khoản' : 'Thêm Tài Khoản Mới';
        const pwdGroup = document.getElementById('password-group');
        if(u) {
            f.id.value = u.id; f.fullName.value = u.fullName; f.email.value = u.email; f.phoneNumber.value = u.phoneNumber;
            f.status.value = u.status; f.isPhoneVerified.checked = u.isPhoneVerified; f.isEmailVerified.checked = u.isEmailVerified;
            f.role.value = u.role || 'Customer';
            f.parentUserId.value = u.parentUserId || '';
            f.canManageUsers.checked = !!u.canManageUsers;
            f.canManageTasks.checked = !!u.canManageTasks;
            f.canApproveTasks.checked = !!u.canApproveTasks;
            f.canApproveKycWithdraw.checked = !!u.canApproveKycWithdraw;
            f.canTransferPoints.checked = !!u.canTransferPoints;
            f.canManageBlog.checked = !!u.canManageBlog;
            f.canExportReports.checked = !!u.canExportReports;
            pwdGroup.classList.add('hidden'); f.password.removeAttribute('required');
        } else {
            f.role.value = 'Customer';
            f.parentUserId.value = '';
            applyRolePreset(f);
            pwdGroup.classList.remove('hidden'); f.password.setAttribute('required', 'true');
        }
        document.getElementById('user-modal').classList.remove('hidden');
    };

    window.openResetPwdModal = function(id, name) {
        document.getElementById('reset-pwd-form').reset();
        document.getElementById('reset-pwd-userid').value = id;
        document.getElementById('reset-pwd-username').textContent = name;
        document.getElementById('reset-pwd-modal').classList.remove('hidden');
    };

    window.openUserDetail = async function(id) {
        const res = await apiFetch(`${apiBase}/users/${id}`);
        if(res && res.ok) {
            const u = await res.json();
            document.getElementById('detail-modal-content').innerHTML = `
                <div class="grid grid-cols-2 gap-y-3 gap-x-4">
                    <div><span class="text-slate-500 text-xs">Họ Tên:</span><br><span class="text-slate-200">${u.fullName}</span></div>
                    <div><span class="text-slate-500 text-xs">Wallet ID:</span><br><span class="text-slate-200 font-mono text-[11px]">${u.walletId||'N/A'}</span></div>
                    <div><span class="text-slate-500 text-xs">SĐT:</span><br><span class="text-slate-200">${u.phoneNumber}</span></div>
                    <div><span class="text-slate-500 text-xs">Email:</span><br><span class="text-slate-200">${u.email}</span></div>
                    <div><span class="text-slate-500 text-xs">Số dư:</span><br><span class="text-cyan-400 font-bold">${formatVN(u.walletBalance)}</span></div>
                    <div><span class="text-slate-500 text-xs">Tạm giữ:</span><br><span class="text-amber-400 font-bold">${formatVN(u.lockedBalance)}</span></div>
                    <div><span class="text-slate-500 text-xs">Fail Logins:</span><br><span class="text-rose-400">${u.failedLoginAttempts}</span></div>
                    <div><span class="text-slate-500 text-xs">Ngày Tạo:</span><br><span class="text-slate-200">${new Date(u.createdAt).toLocaleString('vi-VN')}</span></div>
                </div>`;
            document.getElementById('detail-modal').classList.remove('hidden');
        }
    };

    window.actionUser = async function(id, action) {
        let url = `${apiBase}/users/${id}`; let method = 'POST'; let msg = '';
        if (action === 'lock') { url += '/lock'; msg = 'Khóa user này?'; }
        else if (action === 'unlock') { url += '/unlock'; msg = 'Mở khóa user này?'; }
        else if (action === 'delete') { method = 'DELETE'; msg = 'Vô hiệu hóa tài khoản này? Tài khoản sẽ không bị xóa khỏi dữ liệu.'; }
        
        if (!confirm(msg)) return;
        const res = await apiFetch(url, { method });
        if(res && res.ok) { showToast('Thao tác thành công'); fetchUsers(); }
        else showToast('Lỗi khi thực hiện', 'error');
    };

    async function initUsersPage() {
        const filterForm = document.getElementById('user-filter-form');
        const userForm = document.getElementById('admin-user-form');
        userForm?.role?.addEventListener('change', () => applyRolePreset(userForm));

        if (filterForm) {
            filterForm.addEventListener('submit', e => {
                e.preventDefault();
                usersState.keyword = filterForm.keyword.value.trim(); usersState.status = filterForm.status.value;
                usersState.isPhoneVerified = filterForm.isPhoneVerified.value; usersState.isEmailVerified = filterForm.isEmailVerified?.value || '';
                usersState.pageSize = parseInt(filterForm.pageSize.value); usersState.page = 1;
                fetchUsers();
            });
        }

        const form = document.getElementById('admin-user-form');
        if (form) {
            form.addEventListener('submit', async (e) => {
                e.preventDefault();
                const isUpdate = !!form.id.value;
                const validationError = validateAdminUserForm(form, isUpdate);
                if (validationError) {
                    showToast(validationError, 'error');
                    return;
                }

                const data = {
                    fullName: form.fullName.value.trim(),
                    email: form.email.value.trim().toLowerCase(),
                    phoneNumber: form.phoneNumber.value.trim(),
                    status: form.status.value,
                    role: form.role.value,
                    parentUserId: form.parentUserId.value.trim() || null,
                    canManageUsers: form.canManageUsers.checked,
                    canManageTasks: form.canManageTasks.checked,
                    canApproveTasks: form.canApproveTasks.checked,
                    canApproveKycWithdraw: form.canApproveKycWithdraw.checked,
                    canTransferPoints: form.canTransferPoints.checked,
                    canManageBlog: form.canManageBlog.checked,
                    canExportReports: form.canExportReports.checked,
                    isPhoneVerified: form.isPhoneVerified.checked,
                    isEmailVerified: form.isEmailVerified.checked
                };
                if(!isUpdate) data.password = form.password.value;

                const url = isUpdate ? `${apiBase}/users/${form.id.value}` : `${apiBase}/users`;
                const submitButton = form.querySelector('button[type="submit"]');
                if (submitButton) submitButton.disabled = true;
                const res = await apiFetch(url, { method: isUpdate ? 'PUT' : 'POST', body: JSON.stringify(data) });
                if (submitButton) submitButton.disabled = false;
                
                if(res && res.ok) {
                    showToast(isUpdate ? 'Cập nhật thành công' : 'Thêm user thành công');
                    closeModal('user-modal'); fetchUsers();
                } else {
                    const message = await readApiError(res, isUpdate ? 'Không thể cập nhật tài khoản' : 'Không thể tạo tài khoản');
                    showToast(message, 'error');
                }
            });
        }

        const resetForm = document.getElementById('reset-pwd-form');
        if (resetForm) {
            resetForm.addEventListener('submit', async e => {
                e.preventDefault();
                const id = document.getElementById('reset-pwd-userid').value;
                const newPassword = document.getElementById('new-password').value;
                const res = await apiFetch(`${apiBase}/users/${id}/reset-password`, { method: 'POST', body: JSON.stringify({ newPassword }) });
                if(res && res.ok) { showToast('Đổi mật khẩu thành công'); closeModal('reset-pwd-modal'); }
                else showToast('Lỗi đổi mật khẩu', 'error');
            });
        }

        fetchUsers();
    }

    // Wallets
    let allWallets = [];
    async function fetchWallets() {
        const tableBody = document.getElementById('wallets-table-body');
        if (!tableBody) return;
        const res = await apiFetch(`${apiBase}/wallets`);
        if(res && res.ok) { allWallets = await res.json(); renderWallets(); }
        else { tableBody.innerHTML = '<tr><td colspan="5" class="px-6 py-8 text-center text-rose-400">Không thể tải dữ liệu ví.</td></tr>'; }
    }

    function renderWallets() {
        const tableBody = document.getElementById('wallets-table-body');
        const search = document.getElementById('wallet-search')?.value.toLowerCase() || '';
        const filtered = allWallets.filter(w => w.userId.toLowerCase().includes(search) || w.walletId.toLowerCase().includes(search) || w.userName?.toLowerCase().includes(search));
        
        if(filtered.length === 0) { tableBody.innerHTML = '<tr><td colspan="5" class="px-6 py-8 text-center text-slate-500">Không tìm thấy ví.</td></tr>'; return; }

        tableBody.innerHTML = filtered.map(w => `
            <tr class="hover:bg-slate-800/30 transition-colors">
                <td class="px-6 py-3 font-medium text-slate-200"><div>${w.userName}</div><div class="text-xs font-mono text-slate-500 mt-0.5" title="${w.userId}">${w.userId.substring(0,8)}...</div></td>
                <td class="px-6 py-3 text-right font-bold text-blue-400">${formatVN(w.balance)} điểm</td>
                <td class="px-6 py-3 text-right font-medium text-amber-500/80">${formatVN(w.lockedBalance || 0)} điểm</td>
                <td class="px-6 py-3 text-right font-bold text-emerald-400">${formatVN(w.balance - (w.lockedBalance || 0))} điểm</td>
                <td class="px-6 py-3 text-right">
                    <button class="text-xs bg-amber-500/10 text-amber-400 hover:bg-amber-500/20 px-2.5 py-1.5 rounded transition-colors" onclick="openWalletModal('${w.userId}')"><i class="fas fa-right-left mr-1"></i> Chuyển điểm</button>
                </td>
            </tr>`).join('');
    }

    async function initWalletsPage() {
        const form = document.getElementById('wallet-adjust-form');
        if(form) {
            form.addEventListener('submit', async (e) => {
                e.preventDefault();
                const data = { amount: Math.abs(Number(form.amount.value)), reason: form.reason.value };
                const userId = form.userId.value;
                const res = await apiFetch(`${apiBase}/users/${userId}/points/adjust`, { method: 'POST', body: JSON.stringify(data) });
                if(res && res.ok) {
                    showToast('Chuyển điểm thành công');
                    document.getElementById('wallet-modal').classList.add('hidden');
                    form.reset(); fetchWallets();
                } else {
                    const err = await res?.text() || 'Lỗi';
                    showToast(err, 'error');
                }
            });
        }
        document.getElementById('wallet-search')?.addEventListener('input', renderWallets);
        fetchWallets();
    }

    // CardRates
    async function fetchCardRates() {
        const tableBody = document.getElementById('cardrates-table-body');
        if (!tableBody) return;
        const res = await apiFetch(`${apiBase}/cardrates`);
        if(res && res.ok) {
            const rates = await res.json();
            if(rates.length===0){ tableBody.innerHTML='<tr><td colspan="5" class="px-5 py-8 text-center text-slate-500">Chưa có bảng giá nào.</td></tr>'; return; }
            tableBody.innerHTML = rates.map(r => `
            <tr class="hover:bg-slate-800/30 transition-colors">
                <td class="px-5 py-3 font-bold text-slate-200 tracking-wider">${r.provider}</td>
                <td class="px-5 py-3 text-right font-medium text-blue-400">${formatVN(r.faceValue)}</td>
                <td class="px-5 py-3 text-right font-bold text-emerald-400">${r.discountPercent}%</td>
                <td class="px-5 py-3 text-center">
                    ${r.isActive ? '<span class="px-2 py-1 text-xs rounded bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">Active</span>' : '<span class="px-2 py-1 text-xs rounded bg-slate-800 text-slate-400 border border-slate-700">Inactive</span>'}
                </td>
                <td class="px-5 py-3 text-right space-x-1">
                    <button type="button" class="px-2.5 py-1.5 text-xs rounded bg-cyan-500/10 text-cyan-400 hover:bg-cyan-500/20 transition-colors edit-rate-btn" data-rate='${JSON.stringify(r)}'><i class="fas fa-pen"></i></button>
                    <button type="button" class="px-2.5 py-1.5 text-xs rounded bg-rose-500/10 text-rose-400 hover:bg-rose-500/20 transition-colors delete-rate-btn" data-rate-id="${r.id}"><i class="fas fa-trash"></i></button>
                </td>
            </tr>`).join('');

            tableBody.querySelectorAll('.edit-rate-btn').forEach(b => b.addEventListener('click', () => {
                const r = JSON.parse(b.getAttribute('data-rate'));
                const f = document.getElementById('cardrate-form');
                f.provider.value = r.provider; f.faceValue.value = r.faceValue; f.discountPercent.value = r.discountPercent; f.isActive.checked = r.isActive;
                f.setAttribute('data-editing-id', r.id);
                f.querySelector('button[type="submit"] span').textContent = 'Cập Nhật Bảng Giá';
                document.getElementById('cardrate-cancel').classList.remove('hidden');
                f.faceValue.dispatchEvent(new Event('input'));
            }));
            tableBody.querySelectorAll('.delete-rate-btn').forEach(b => b.addEventListener('click', async () => {
                if(!confirm('Xóa bảng giá này?')) return;
                const rId = b.getAttribute('data-rate-id');
                const dres = await apiFetch(`${apiBase}/cardrates/${rId}`, {method:'DELETE'});
                if(dres && dres.ok) { showToast('Xóa thành công'); fetchCardRates(); }
            }));
        }
    }

    function calculateExpected() {
        const fv = Number(document.querySelector('[name="faceValue"]')?.value) || 0;
        const dp = Number(document.querySelector('[name="discountPercent"]')?.value) || 0;
        const expectedAmountEl = document.getElementById('expected-receive-amount');
        if(expectedAmountEl) expectedAmountEl.textContent = formatVN(fv - (fv * dp / 100));
    }

    async function initCardRatesPage() {
        const form = document.getElementById('cardrate-form');
        if(form) {
            form.faceValue.addEventListener('input', calculateExpected);
            form.discountPercent.addEventListener('input', calculateExpected);
            document.getElementById('cardrate-cancel')?.addEventListener('click', () => {
                form.reset(); form.removeAttribute('data-editing-id');
                form.querySelector('button[type="submit"] span').textContent = 'Lưu Bảng Giá';
                document.getElementById('cardrate-cancel').classList.add('hidden');
                calculateExpected();
            });
            form.addEventListener('submit', async (e) => {
                e.preventDefault();
                const data = { provider: form.provider.value, faceValue: Number(form.faceValue.value), discountPercent: Number(form.discountPercent.value), isActive: form.isActive.checked };
                const editId = form.getAttribute('data-editing-id');
                const url = editId ? `${apiBase}/cardrates/${editId}` : `${apiBase}/cardrates`;
                const method = editId ? 'PUT' : 'POST';
                const res = await apiFetch(url, {method, body: JSON.stringify(data)});
                if(res && res.ok) {
                    showToast('Đã lưu cấu hình bảng giá');
                    document.getElementById('cardrate-cancel').click();
                    fetchCardRates();
                } else {
                    showToast('Lỗi khi lưu bảng giá', 'error');
                }
            });
        }
        fetchCardRates();
    }

    // Transactions
    async function fetchTransactions() {
        const tableBody = document.getElementById('transactions-table-body');
        if (!tableBody) return;
        const search = document.getElementById('transaction-search')?.value || '';
        const status = document.getElementById('transaction-status')?.value || 'all';
        const type = document.getElementById('transaction-type')?.value || 'all';
        
        // Try to check if search is UUID, but in dashboard we might just use query params if API supports it.
        let url = `${apiBase}/transactions`;
        const parts = [];
        if(search.length === 36) parts.push(`userId=${search}`);
        if(status !== 'all') parts.push(`status=${status}`);
        if(type !== 'all') parts.push(`type=${type}`);
        if(parts.length > 0) url += `?${parts.join('&')}`;

        const res = await apiFetch(url);
        if(res && res.ok) {
            const txs = await res.json();
            // Local filter for quick search if not UUID
            const filtered = txs.filter(t => (search.length < 36 && search.length > 0 ? (t.userName?.includes(search) || t.id.includes(search)) : true));
            if(filtered.length===0){ tableBody.innerHTML='<tr><td colspan="6" class="px-5 py-8 text-center text-slate-500">Không tìm thấy giao dịch.</td></tr>'; return; }
            tableBody.innerHTML = filtered.map(t => {
                let bg = 'bg-slate-800 text-slate-300';
                if(t.status==='Success'||t.status==='Completed') bg = 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400';
                else if(t.status==='Failed') bg = 'bg-rose-500/10 border-rose-500/20 text-rose-400';
                else if(t.status==='Pending'||t.status==='Processing') bg = 'bg-amber-500/10 border-amber-500/20 text-amber-400';
                
                let typeTxt = t.type === 'Deposit_Card' ? 'Nạp thẻ' :
                    (t.type === 'AdminTransferOut' ? 'Admin chuyển ra' :
                    (t.type === 'AdminTransferIn' ? 'Admin chuyển vào' :
                    (t.type === 'Deposit' ? 'Nạp xu' : 'Trừ xu')));
                let typeIcon = t.amount >= 0 ? '<i class="fas fa-arrow-down text-emerald-500 mr-1"></i>' : '<i class="fas fa-arrow-up text-rose-500 mr-1"></i>';
                
                return `
                <tr class="hover:bg-slate-800/30 transition-colors">
                    <td class="px-5 py-3 font-mono text-xs text-slate-500" title="${t.id}">${t.id.substring(0,8)}...</td>
                    <td class="px-5 py-3 text-slate-200"><div>${t.userName||'Unknown'}</div><div class="text-xs text-slate-500">${t.userId.substring(0,8)}...</div></td>
                    <td class="px-5 py-3 text-xs font-semibold text-slate-300">${typeIcon} ${typeTxt}</td>
                    <td class="px-5 py-3 text-right font-bold ${t.amount>=0?'text-emerald-400':'text-rose-400'}">${t.amount>=0?'+':''}${formatVN(t.amount)}</td>
                    <td class="px-5 py-3 text-center"><span class="px-2 py-1 text-[11px] uppercase tracking-wider rounded border ${bg}">${t.status}</span></td>
                    <td class="px-5 py-3 text-right text-xs text-slate-400">${formatShortDate(t.createdAt)}</td>
                </tr>`;
            }).join('');
        } else {
            tableBody.innerHTML = '<tr><td colspan="6" class="px-5 py-8 text-center text-rose-400">Lỗi lấy dữ liệu</td></tr>';
        }
    }

    async function initTransactionsPage() {
        document.getElementById('transaction-filter')?.addEventListener('click', fetchTransactions);
        fetchTransactions();
    }

    async function initSettingsPage() {
        const form = document.getElementById('admin-settings-form');
        if (!form) return;

        const response = await apiFetch(`${apiBase}/settings`);
        if (response?.ok) {
            const settings = await response.json();
            settings.forEach(item => {
                const input = form.elements[item.key];
                if (input) input.value = item.value || '';
            });
        }

        form.addEventListener('submit', async (event) => {
            event.preventDefault();
            const entries = Array.from(new FormData(form).entries());
            for (const [key, value] of entries) {
                const saveResponse = await apiFetch(`${apiBase}/settings/${encodeURIComponent(key)}`, {
                    method: 'PUT',
                    body: JSON.stringify({ value, description: 'Cập nhật từ admin settings' })
                });
                if (!saveResponse?.ok) {
                    showToast(await readApiError(saveResponse, `Không lưu được ${key}`), 'error');
                    return;
                }
            }
            showToast('Đã lưu cấu hình hệ thống');
        });
    }

    // Dashboard
    async function initDashboard() {
        try {
            const [usersRes, walletsRes, txRes] = await Promise.all([apiFetch(`${apiBase}/users`), apiFetch(`${apiBase}/wallets`), apiFetch(`${apiBase}/transactions`)]);
            if(usersRes && usersRes.ok) {
                const users = await usersRes.json();
                const totalUsers = Array.isArray(users) ? users.length : (users.totalCount || users.items?.length || 0);
                if(document.getElementById('stat-total-users')) document.getElementById('stat-total-users').textContent = totalUsers;
            }
                const totalBalEl = document.getElementById('stat-total-balance');
                if(totalBalEl) totalBalEl.textContent = formatVN(TOTAL_SUPPLY_POINTS) + ' điểm';
            if(txRes && txRes.ok) {
                const txs = await txRes.json();
                if(document.getElementById('stat-total-tx')) document.getElementById('stat-total-tx').textContent = `(${txs.length} Tổng)`;
                const success = txs.filter(t=>t.status==='Success'||t.status==='Completed').length;
                const pending = txs.filter(t=>t.status==='Pending'||t.status==='Processing').length;
                if(document.getElementById('stat-success-tx')) document.getElementById('stat-success-tx').textContent = success;
                if(document.getElementById('stat-pending-tx')) document.getElementById('stat-pending-tx').textContent = pending;
                
                const rbody = document.getElementById('recent-transactions-body');
                if(rbody) {
                    rbody.innerHTML = txs.slice(0, 5).map(t => {
                        let bg = 'bg-slate-800 text-slate-300';
                        if(t.status==='Success'||t.status==='Completed') bg = 'bg-emerald-500/10 text-emerald-400';
                        else if(t.status==='Failed') bg = 'bg-rose-500/10 text-rose-400';
                        else if(t.status==='Pending'||t.status==='Processing') bg = 'bg-amber-500/10 text-amber-400';
                        return `<tr class="hover:bg-slate-800/30">
                            <td class="px-5 py-3 font-mono text-xs text-slate-500">${t.id.substring(0,8)}</td>
                            <td class="px-5 py-3 text-slate-300">${t.userName||'Unknown'}</td>
                            <td class="px-5 py-3 text-xs text-slate-400">${t.type}</td>
                            <td class="px-5 py-3 font-bold ${t.amount>=0?'text-emerald-400':'text-rose-400'}">${formatVN(t.amount)} điểm</td>
                            <td class="px-5 py-3"><span class="px-2 py-1 text-[11px] rounded ${bg}">${t.status}</span></td>
                        </tr>`;
                    }).join('');
                }
            }
        } catch(err) {
            console.error('Error loading dashboard stats', err);
        }
    }

    // KYC Manual Review
    let kycActiveStatus = "Pending";
    let kycRequests = [];

    async function fetchKycRequests() {
        const tableBody = document.getElementById('kyc-table-body');
        if (!tableBody) return;
        
        tableBody.innerHTML = '<tr><td colspan="5" class="px-6 py-8 text-center text-slate-500"><i class="fas fa-spinner fa-spin mr-2"></i> Đang tải dữ liệu hồ sơ KYC...</td></tr>';
        
        const response = await apiFetch(`${apiBase}/kyc/requests?status=${kycActiveStatus}`);
        if (response && response.ok) {
            kycRequests = await response.json();
            renderKycTable(kycRequests);
            updateKycStats();
        } else {
            tableBody.innerHTML = '<tr><td colspan="5" class="px-6 py-8 text-center text-rose-400">Lỗi khi tải danh sách hồ sơ KYC.</td></tr>';
        }
    }

    function renderKycTable(requests) {
        const tableBody = document.getElementById('kyc-table-body');
        if (!tableBody) return;

        if (!requests || requests.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="5" class="px-6 py-12 text-center text-slate-500">Không có hồ sơ nào phù hợp.</td></tr>';
            return;
        }

        tableBody.innerHTML = requests.map(r => {
            let statusBadge = '';
            if (r.status === 'Approved') {
                statusBadge = '<span class="px-2 py-0.5 text-[10px] font-bold uppercase rounded bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">Approved</span>';
            } else if (r.status === 'Rejected') {
                statusBadge = '<span class="px-2 py-0.5 text-[10px] font-bold uppercase rounded bg-rose-500/10 text-rose-400 border border-rose-500/20">Rejected</span>';
            } else {
                statusBadge = '<span class="px-2 py-0.5 text-[10px] font-bold uppercase rounded bg-amber-500/10 text-amber-400 border border-amber-500/20">Pending</span>';
            }

            return `
            <tr class="hover:bg-slate-800/30 transition-colors border-b border-borderbg/50 last:border-0">
                <td class="px-6 py-3.5">
                    <div class="font-medium text-slate-200 text-sm">${escapeHtml(r.userName || r.email)}</div>
                    <div class="font-mono text-[11px] text-slate-500 mt-0.5" title="${r.userId}">${r.userId.substring(0,8)}...</div>
                </td>
                <td class="px-6 py-3.5 text-sm text-slate-400">
                    <div>SĐT: ${escapeHtml(r.phoneNumber || 'N/A')}</div>
                    <div class="text-xs text-slate-500 mt-0.5">${escapeHtml(r.email)}</div>
                </td>
                <td class="px-6 py-3.5 text-xs text-slate-400">
                    ${formatShortDate(r.createdAt)}
                </td>
                <td class="px-6 py-3.5">
                    ${statusBadge}
                </td>
                <td class="px-6 py-3.5 text-right">
                    <button type="button" onclick="openKycReviewModal('${r.id}')" class="px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-200 hover:text-white rounded-lg text-xs font-semibold border border-borderbg transition-colors">
                        Chi Tiết & Duyệt
                    </button>
                </td>
            </tr>`;
        }).join('');
    }

    function updateKycStats() {
        apiFetch(`${apiBase}/kyc/requests?status=All`).then(async res => {
            if (res && res.ok) {
                const all = await res.json();
                const pending = all.filter(x => x.status === 'Pending').length;
                const approved = all.filter(x => x.status === 'Approved').length;
                const rejected = all.filter(x => x.status === 'Rejected').length;
                
                setText('stat-kyc-pending', String(pending));
                setText('stat-kyc-approved', String(approved));
                setText('stat-kyc-rejected', String(rejected));
            }
        }).catch(err => console.error(err));
    }

    let activeReviewKycId = null;

    window.openKycReviewModal = function(id) {
        const r = kycRequests.find(x => x.id === id);
        if (!r) return;

        activeReviewKycId = id;
        
        setText('kyc-modal-name', r.userName || 'N/A');
        setText('kyc-modal-phone', r.phoneNumber || 'N/A');
        setText('kyc-modal-email', r.email || 'N/A');
        setText('kyc-modal-date', new Date(r.createdAt).toLocaleString('vi-VN'));

        document.getElementById('kyc-img-front').src = r.frontIdImagePath || '';
        document.getElementById('kyc-img-back').src = r.backIdImagePath || '';
        document.getElementById('kyc-img-selfie').src = r.selfieImagePath || '';

        document.getElementById('reject-reason-container').classList.add('hidden');
        document.getElementById('reject-reason').value = '';
        document.getElementById('btn-kyc-reject-mode').classList.remove('hidden');
        document.getElementById('btn-kyc-confirm-reject').classList.add('hidden');

        const actionBtns = document.getElementById('kyc-action-buttons');
        const reviewerInfo = document.getElementById('kyc-modal-reviewer-info');
        
        if (r.status === 'Pending') {
            actionBtns.classList.remove('hidden');
            reviewerInfo.textContent = '';
        } else {
            actionBtns.classList.add('hidden');
            let reviewTxt = `Trạng thái: <b>${r.status}</b>`;
            if (r.reviewedAt) {
                reviewTxt += ` vào lúc ${new Date(r.reviewedAt).toLocaleString('vi-VN')}`;
            }
            if (r.rejectReason) {
                reviewTxt += `<br>Lý do từ chối: <span class="text-rose-400 font-medium">${escapeHtml(r.rejectReason)}</span>`;
            }
            reviewerInfo.innerHTML = reviewTxt;
        }

        document.getElementById('kyc-modal').classList.remove('hidden');
    };

    async function submitKycApprove() {
        if (!activeReviewKycId) return;
        if (!confirm("Xác nhận phê duyệt hồ sơ KYC này?")) return;

        const res = await apiFetch(`${apiBase}/kyc/requests/${activeReviewKycId}/approve`, {
            method: 'POST'
        });

        if (res && res.ok) {
            showToast("Đã phê duyệt hồ sơ KYC thành công.");
            document.getElementById('kyc-modal').classList.add('hidden');
            fetchKycRequests();
        } else {
            showToast("Thao tác thất bại.", "error");
        }
    }

    async function submitKycReject() {
        if (!activeReviewKycId) return;
        const reason = document.getElementById('reject-reason').value.trim();
        if (!reason) {
            showToast("Vui lòng nhập lý do từ chối.", "error");
            return;
        }

        const res = await apiFetch(`${apiBase}/kyc/requests/${activeReviewKycId}/reject`, {
            method: 'POST',
            body: JSON.stringify({ reason })
        });

        if (res && res.ok) {
            showToast("Đã từ chối hồ sơ KYC.");
            document.getElementById('kyc-modal').classList.add('hidden');
            fetchKycRequests();
        } else {
            showToast("Thao tác thất bại.", "error");
        }
    }

    function initKycPage() {
        const tabButtons = document.querySelectorAll('#kyc-tabs button');
        tabButtons.forEach(btn => {
            btn.addEventListener('click', () => {
                tabButtons.forEach(b => {
                    b.className = 'px-4 py-2 text-xs font-bold rounded-lg text-slate-400 hover:bg-slate-800 transition-colors';
                });
                btn.className = 'px-4 py-2 text-xs font-bold rounded-lg bg-cyan-600 text-white transition-colors';
                kycActiveStatus = btn.getAttribute('data-status');
                fetchKycRequests();
            });
        });

        document.getElementById('btn-kyc-approve')?.addEventListener('click', submitKycApprove);
        
        document.getElementById('btn-kyc-reject-mode')?.addEventListener('click', () => {
            document.getElementById('reject-reason-container').classList.remove('hidden');
            document.getElementById('btn-kyc-reject-mode').classList.add('hidden');
            document.getElementById('btn-kyc-confirm-reject').classList.remove('hidden');
        });
        
        document.getElementById('btn-kyc-confirm-reject')?.addEventListener('click', submitKycReject);

        document.querySelectorAll('.img-lightbox-trigger').forEach(img => {
            img.addEventListener('click', () => {
                const lightbox = document.getElementById('lightbox-modal');
                const lightboxImg = document.getElementById('lightbox-img');
                if (lightbox && lightboxImg) {
                    lightboxImg.src = img.src;
                    lightbox.classList.remove('hidden');
                }
            });
        });

        fetchKycRequests();
    }

    // Login
    async function initLoginPage() {
        const form = document.getElementById('admin-login-form');
        const message = document.getElementById('login-message');
        clearAdminSession();
        if (window.location.search && /(?:^|[?&])password=/i.test(window.location.search)) {
            window.history.replaceState({}, document.title, window.location.pathname + window.location.hash);
        }
        if(form) {
            form.addEventListener('submit', async (e) => {
                e.preventDefault();
                form.setAttribute('method', 'post');
                form.setAttribute('action', '/login');
                message.classList.add('hidden');
                const button = form.querySelector('button[type="submit"]');
                if (button) button.disabled = true;
                const res = await fetch('/api/auth/login', {
                    method: 'POST', headers:{'Content-Type': 'application/json'},
                    body: JSON.stringify({ login: form.login.value.trim(), password: form.password.value })
                });
                if (button) button.disabled = false;
                if(res.ok) {
                    const data = await res.json();
                    if (data.role !== 'Admin') {
                        message.textContent = 'Tài khoản không có quyền truy cập quản trị.';
                        message.classList.remove('hidden');
                        return;
                    }
                    localStorage.setItem('adminToken', data.accessToken);
                    localStorage.setItem('adminUser', JSON.stringify({
                        userId: data.userId,
                        fullName: data.fullName,
                        email: data.email,
                        phoneNumber: data.phoneNumber,
                        role: data.role
                    }));
                    sessionStorage.setItem('adminSessionActive', '1');
                    setAdminSessionCookie(data.accessToken);
                    window.location.href = '/';
                } else {
                    let errorMessage = 'Thông tin đăng nhập không chính xác.';
                    try {
                        const error = await res.json();
                        errorMessage = error?.message || error?.title || errorMessage;
                    } catch {
                        // Keep the generic message when the API does not return JSON.
                    }
                    message.textContent = errorMessage;
                    message.classList.remove('hidden');
                }
            });
        }
    }

    // Init
    const path = window.location.pathname.toLowerCase();
    const isAuthPage = path.includes('/login') || path.includes('/auth-sso');
    const token = hasActiveAdminTabSession()
        ? (localStorage.getItem('adminToken') || getAdminSessionCookie())
        : '';
    if (!isAuthPage && token && !localStorage.getItem('adminToken')) {
        localStorage.setItem('adminToken', token);
    }
    
    if ((!hasActiveAdminTabSession() || !token) && !isAuthPage) {
        clearAdminSession();
        redirectToMainLogin(); return;
    }

    window.fetchKycRequests = fetchKycRequests;

    document.addEventListener('DOMContentLoaded', () => {
        setActiveMenu();
        hydrateCurrentAdminHeader();
        if (path.includes('/login')) initLoginPage();
        else if (path.includes('/users')) initUsersPage();
        else if (path.includes('/wallets')) initWalletsPage();
        else if (path.includes('/cardrates')) initCardRatesPage();
        else if (path.includes('/transactions')) initTransactionsPage();
        else if (path.includes('/settings')) initSettingsPage();
        else if (path.includes('/kyc')) initKycPage();
        else initDashboard();
    });

    const logoutBtn = document.getElementById('admin-logout-btn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', () => {
            clearAdminSession();
            redirectToMainLogin();
        });
    }
})();
