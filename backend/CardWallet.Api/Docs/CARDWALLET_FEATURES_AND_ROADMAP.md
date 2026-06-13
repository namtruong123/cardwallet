# CardWallet - Tổng Hợp Chức Năng Và Roadmap Nâng Cấp

## 1. Mục Tiêu Sản Phẩm

CardWallet là nền tảng đổi thẻ cào sang ví xu nội bộ. Người dùng đăng ký tài khoản, xem bảng giá chiết khấu, gửi thẻ cào sang API trang mẹ, nhận đúng số xu thực nhận, xác minh KYC và rút xu về tài khoản ngân hàng sau khi admin duyệt.

Đơn vị tiền tệ nội bộ:

```text
1.000 VND = 1 xu
```

Tỷ lệ này cần được đưa vào cấu hình hệ thống, không nên hard-code rải rác trong UI hoặc service.

Tổng cung điểm ban đầu:

```text
50.000.000 điểm
```

Nguyên tắc bắt buộc:

- Tổng cung không tự tăng khi admin chuyển điểm.
- Admin chuyển điểm cho user/quản lý/bất kỳ tài khoản nào thì ví admin giảm và ví người nhận tăng.
- Tổng số dư toàn hệ thống sau chuyển vẫn bằng tổng cung, trừ các nghiệp vụ nạp vốn/đốt điểm/rút điểm đã được ghi ledger rõ ràng.
- Không dùng chức năng "cộng điểm" tự do vì sẽ gây lạm phát điểm, sai dòng tiền và làm giảm lợi nhuận.

## 2. Cấu Trúc Giao Diện Hiện Tại

Public:

```text
/                         -> Pages/Index.cshtml
/Login                    -> Pages/Login.cshtml
/Register                 -> Pages/Register.cshtml
/CardRates                -> Pages/CardRates.cshtml
/Guide                    -> Pages/Guide.cshtml
```

User:

```text
/User                     -> Tổng quan user
/User/Exchange            -> Đổi thẻ cào
/User/CardRates           -> Bảng giá user
/User/Wallet              -> Ví xu
/User/Transactions        -> Lịch sử giao dịch
/User/Withdraw            -> Rút xu
/User/Profile             -> Hồ sơ/KYC
```

Admin:

```text
/Admin                    -> Tổng quan quản trị
/Admin/Login              -> Đăng nhập admin
/Admin/Users              -> Quản lý người dùng
/Admin/Wallets            -> Quản lý ví xu
/Admin/CardRates          -> Quản lý bảng giá
/Admin/Transactions       -> Đối soát giao dịch
/Admin/Kyc                -> Khung duyệt KYC
/Admin/Withdrawals        -> Khung duyệt rút xu
/Admin/Deposits           -> Khung nạp xu
/Admin/Referrals          -> Khung link REF
/Admin/Commissions        -> Khung hoa hồng
```

Layout chính:

```text
Pages/Shared/_Layout.cshtml
Pages/Shared/_PublicLayout.cshtml
Pages/Shared/_UserDashboardLayout.cshtml
Pages/Shared/_ModernAdminLayout.cshtml
```

JavaScript chính:

```text
wwwroot/assets/js/public-site.js
wwwroot/assets/js/user-dashboard.js
wwwroot/assets/js/admin.js
```

## 3. Chức Năng Đã Có Và Đang Hoạt Động

### 3.1. Đăng ký user

Đã có:

- Form `/Register`.
- Bắt buộc họ tên, số điện thoại, email, mật khẩu và nhập lại mật khẩu.
- Gọi API:

```text
POST /api/auth/register
```

- Tạo user kèm ví mặc định.
- Lưu token vào `localStorage.authData`.
- Đăng ký xong chuyển sang `/User/Exchange`.

Cần nâng cấp:

- OTP xác thực số điện thoại.
- Xác thực email bằng mã hoặc link.
- Chặn đổi thẻ/rút xu nếu chưa xác thực theo cấu hình.

### 3.2. Đăng nhập user

Đã có:

- Form `/Login`.
- Đăng nhập bằng email hoặc số điện thoại.
- Gọi API:

```text
POST /api/auth/login
POST /api/auth/refresh-token
POST /api/auth/logout
```

- Lưu access token và refresh token.
- Đăng nhập xong chuyển sang `/User/Exchange`.

Cần nâng cấp:

- Điều hướng theo role:
  - Admin -> `/Admin`
  - PartnerOrg -> dashboard đối tác
  - Customer/CTV -> `/User/Exchange`
- Hiển thị lỗi chi tiết hơn cho tài khoản khóa, chưa xác thực, sai mật khẩu.

### 3.3. Đăng nhập admin

Đã có:

- Form `/Admin/Login`.
- Dùng chung API auth.
- Token admin lưu trong `localStorage.adminToken`.
- Tài khoản admin dev được seed khi môi trường là Development.

Tài khoản dev:

```text
Email: admin@example.com
Phone: 0900000000
Password: AdminPass123
```

Đã sửa lỗi:

- MySQL chưa chạy sẽ trả lỗi rõ ràng:

```text
Không kết nối được MySQL. Vui lòng kiểm tra database/connection string rồi thử đăng nhập lại.
```

Cần nâng cấp:

- Bắt buộc admin role khi vào admin API.
- Không cho user thường đăng nhập vào khu vực admin.
- Thêm audit log đăng nhập admin.

### 3.4. Bảng giá thẻ cào

Đã có:

- Public `/CardRates`.
- User `/User/CardRates`.
- Admin `/Admin/CardRates`.
- API:

```text
GET  /api/card-rates
GET  /api/admin/cardrates
POST /api/admin/cardrates
PUT  /api/admin/cardrates/{id}
DELETE /api/admin/cardrates/{id}
```

Hiển thị:

- Nhà mạng.
- Mệnh giá.
- % chiết khấu.
- Số xu/thực nhận dự kiến.

Cần nâng cấp:

- Worker đồng bộ bảng giá từ trang mẹ.
- Log lần sync cuối.
- Healthcheck API trang mẹ.
- Cảnh báo khi giá từ trang mẹ lỗi hoặc quá hạn.

### 3.5. Đổi thẻ cào

Đã có nền tảng:

```text
POST /api/card-exchange/submit
GET  /api/card-exchange/my-transactions
GET  /api/card-exchange/{id}/status
```

Đã có worker:

- `CardExchangeWorker`
- `CardTransactionReconciliationWorker`

Development đang tắt worker để tránh spam lỗi khi MySQL chưa chạy.

Cần nâng cấp:

- Kết nối API trang mẹ thật.
- Lưu raw request/response đầy đủ.
- Cộng ví theo số tiền thực nhận từ trang mẹ, không cộng theo số dự kiến nếu kết quả khác.
- Idempotency key chống cộng trùng.
- Reconcile giao dịch pending/need reconcile.
- Cảnh báo user khi chọn sai loại thẻ, sai mệnh giá, sai mã thẻ hoặc serial.

### 3.6. Ví xu

Đã có:

- Ví tự tạo khi đăng ký (default wallet).
- Admin xem ví tại `/Admin/Wallets`.
- Admin chuyển điểm từ ví admin sang ví người nhận.
- Chuyển điểm ghi hai bút toán:
  - `AdminTransferOut`: ví admin giảm.
  - `AdminTransferIn`: ví người nhận tăng.
- API balance user:

```text
GET /api/wallets/me/balance
```

- API Admin quản lý ví:

```text
GET  /api/admin/wallets
POST /api/admin/wallets/adjust
```

- Wallet balance service với repository pattern.
- Entity: User, Wallet, WalletTransaction.

Cần nâng cấp:

- API lịch sử ví user với phân trang:

```text
GET /api/wallets/me/transactions?page=1&limit=20
GET /api/wallets/me/summary
```

- Audit log riêng cho admin cộng/trừ xu (type: `AdminAdjustment`).
- Audit log riêng cho admin chuyển điểm nội bộ (type: `InternalTransfer`).
- Cơ chế khóa số dư khi rút xu (WalletLock entity).
- Cấu hình đơn vị tiền tệ trong admin settings.
- Màn reconcile tổng cung để phát hiện dữ liệu cũ từng bị mint sai.
- Filter transaction history theo loại (AdminTransferIn, AdminTransferOut, Withdrawal, CardExchange, etc.).

Đã khóa:

```text
POST /api/admin/wallets/adjust  (tạm khóa trong API nhưng logic vẫn còn)
POST /api/wallets/deposit      (tạm khóa)
```

Các endpoint này từng cho nạp/điều chỉnh ví trực tiếp và có thể làm tăng tổng cung nếu không có nguồn tiền đối ứng. Hiện đã bị khóa để tránh lạm phát điểm. Chỉ admin được phép adjust ví qua route riêng sau khi có audit log.

### 3.7. Quản lý user và phân quyền

Đã có:

- Admin `/Admin/Users`.
- Tạo user (POST /api/admin/users).
- Sửa user (PUT /api/admin/users/{id}).
- Xem chi tiết user (GET /api/admin/users/{id}).
- Danh sách user (GET /api/admin/users).
- Khóa/mở khóa (POST /api/admin/users/{id}/lock, /unlock).
- Vô hiệu hóa mềm (soft delete).
- Reset mật khẩu (POST /api/admin/users/{id}/reset-password).
- Xác thực điện thoại (POST /api/admin/users/{id}/verify-phone).
- Xác thực email (POST /api/admin/users/{id}/verify-email).
- Xóa user (DELETE /api/admin/users/{id}).
- API user lấy profile cá nhân:

```text
GET /api/users/me
```

- Entity: User, Role, UserRole, Permission, RolePermission, UserAuditLog.
- Role enum:

```text
Admin
CentralManager
PartnerOrg
Collaborator
Customer
```

- Permission enum:

```text
CanManageUsers
CanManageTasks
CanApproveTasks
CanApproveKycWithdraw
CanTransferPoints
CanManageBlog
CanExportReports
CanManageSettings
CanViewAuditLogs
CanManageAdmins
```

Cần nâng cấp:

- Gắn [Authorize(Policy="AdminOnly")] vào từng admin API.
- Thêm role-based authorize: [Authorize(Roles="Admin,CentralManager")].
- Ẩn/hiện menu admin theo quyền (FE logic).
- Audit log khi admin thay đổi quyền (log: user X changed permission for user Y).
- Audit log khi admin tạo/khóa/xóa user.
- Ràng buộc PartnerOrg chỉ quản lý CTV thuộc nhóm mình.
- Bảo vệ route /api/admin/** với role validation.
- Session tracking (IP, user-agent, login time) cho security.

### 3.8. Đối soát giao dịch

Đã có:

- Admin `/Admin/Transactions`.
- Lọc theo user ID/mã giao dịch, trạng thái, loại giao dịch.
- Entity: CardTransaction, WalletTransaction.
- Worker: CardTransactionReconciliationWorker (tự động đối soát theo thời gian).
- API:

```text
GET /api/admin/transactions
GET /api/admin/card-exchange/transactions
```

Cần nâng cấp:

- Tách rõ giao dịch ví (WalletTransaction) và giao dịch thẻ (CardTransaction) trong UI.
- Hiển thị raw JSON response từ trang mẹ (parent-card API).
- Lưu trữ request/response đầy đủ cho audit trail.
- Bộ lọc ngày/tháng/năm với date range picker.
- Export CSV/Excel với định dạng tiêu chuẩn (id, user, amount, type, date, status).
- View lịch sử đối soát (reconciliation history) cho từng transaction.
- Search by transaction ID hoặc reference code.
- Stats: total in/out by type and date range.
- Alert nếu transaction vẫn pending sau N ngày.

### 3.9. Cấu hình hệ thống

Đã có:

- Admin `/Admin/Settings` (khung giao diện).
- API:

```text
GET /api/admin/settings
PUT /api/admin/settings/{key}
```

- Entity: SystemSetting (key, value, type, description).
- Service: AdminSettingsService.

Cấu hình hiện tại đang sử dụng:

- appsettings.json (static).
- Cần chuyển sang database động (SystemSetting).

Cái cần thêm:

```text
VND_TO_XU_RATE: 1000 (mặc định)
MIN_WITHDRAWAL_AMOUNT: 100000 (100k VND)
MAX_WITHDRAWAL_PER_MONTH: 2
TELEGRAM_ADMIN_CHAT_ID: <id>
TELEGRAM_BOT_TOKEN: <token>
PARENT_API_ENDPOINT: <url trang mẹ>
PARENT_API_KEY: <api key>
OTP_EXPIRY_MINUTES: 5
EMAIL_SENDER: noreply@cardwallet.com
SMTP_SERVER: <smtp server>
SMTP_PORT: 587
TIMEZONE: Asia/Ho_Chi_Minh
```

Cần nâng cấp:

- Lấy cấu hình từ database thay vì appsettings.json.
- Cache settings với TTL 1 giờ.
- Audit log khi admin thay đổi settings.
- Validate input trước khi save (number, email, URL, etc.).
- Reload ứng dụng hoặc cache khi cập nhật cấu hình quan trọng.
- UI phân group settings (Payment, Auth, System, Integration).

### 3.10. Tìm kiếm từ khóa thẻ cào

Đã có:

- Admin `/Admin/SearchAliases` (khung giao diện).
- Service: AdminSearchAliasService.
- API:

```text
GET  /api/admin/search-aliases
POST /api/admin/search-aliases
DELETE /api/admin/search-aliases/{id}
```

- Entity: SearchAlias (keyword, carrier, denomination, alias).
- Dùng để map từ khóa user nhập vào loại thẻ/mệnh giá chuẩn.

Usecase:

- User gõ "vina 20k" -> map sang carrier=Vina, denomination=20000.
- User gõ "vinaphone 20" -> map sang carrier=Vina, denomination=20000.
- User gõ "viettel 50" -> map sang carrier=Viettel, denomination=50000.

Cần nâng cấp:

- Add/edit/delete từ khóa trong admin UI.
- Tìm kiếm/lọc theo carrier, denomination, keyword.
- Bulk import keywords từ file CSV.
- Test matching trước khi save.
- Version control cho search alias changes (audit trail).

## 4. Chức Năng Đã Có Khung Giao Diện Nhưng Chưa Có Backend Đầy Đủ

### 4.1. KYC cấp 2

Đã có khung admin:

```text
/Admin/Kyc
```

Yêu cầu cần làm:

- User upload CCCD mặt trước.
- User upload CCCD mặt sau.
- User chụp ảnh khuôn mặt.
- Có link mobile để làm KYC trên điện thoại.
- Admin duyệt thủ công.
- User phải KYC approved mới được rút xu.

Entity cần thêm:

```text
KycRequest
KycDocument
KycReviewLog
KycMobileSession
```

API cần thêm:

```text
POST /api/kyc/requests
POST /api/kyc/requests/{id}/front-id
POST /api/kyc/requests/{id}/back-id
POST /api/kyc/requests/{id}/face
POST /api/kyc/requests/{id}/submit
POST /api/kyc/mobile-link
GET  /api/admin/kyc/requests
POST /api/admin/kyc/requests/{id}/approve
POST /api/admin/kyc/requests/{id}/reject
```

### 4.2. Rút xu

Đã có khung admin:

```text
/Admin/Withdrawals
```

Yêu cầu:

- User tạo đơn rút xu.
- Bắt buộc đã KYC.
- Rút tối thiểu 100.000 VND.
- Tối đa 2 lần/tháng.
- Tên bank phải trùng tên đăng ký/KYC.
- Admin duyệt thủ công.
- Khi tạo đơn phải khóa số dư.

Entity cần thêm:

```text
WithdrawalRequest
WithdrawalReviewLog
BankAccount
WalletLock
```

API cần thêm:

```text
POST /api/withdrawals
GET  /api/withdrawals/my
GET  /api/admin/withdrawals
POST /api/admin/withdrawals/{id}/approve
POST /api/admin/withdrawals/{id}/reject
POST /api/admin/withdrawals/{id}/mark-paid
```

### 4.3. Nạp xu

Đã có khung admin:

```text
/Admin/Deposits
```

Yêu cầu:

- User ấn nạp tiền sẽ được chuyển đến giao diện liên hệ Telegram admin.
- Admin nhận bằng chứng thanh toán.
- Admin duyệt và cộng xu.

Entity cần thêm:

```text
DepositRequest
DepositReviewLog
AdminContactConfig
```

API cần thêm:

```text
GET  /api/deposits/config
POST /api/deposits
GET  /api/deposits/my
GET  /api/admin/deposits
POST /api/admin/deposits/{id}/approve
POST /api/admin/deposits/{id}/reject
PUT  /api/admin/deposits/contact-config
```

### 4.4. Link REF

Đã có khung admin:

```text
/Admin/Referrals
```

Yêu cầu:

- Đối tác/Tổ chức có link REF riêng.
- User đăng ký qua link ref sẽ thuộc nhóm của đối tác/tổ chức.
- Link có thể custom theo thương hiệu.
- Mã mời cuối link là 4 số ngẫu nhiên hoặc mã custom.
- CTV bị kick khỏi tổ chức sẽ chuyển về nhóm CTV tập trung.

Entity cần thêm:

```text
PartnerGroup
ReferralCode
ReferralClick
ReferralAssignment
ReferralAuditLog
```

API cần thêm:

```text
GET  /api/referrals/resolve?code=6789
POST /api/referrals/track-click
GET  /api/admin/referrals/groups
POST /api/admin/referrals/groups
GET  /api/admin/referrals/codes
POST /api/admin/referrals/codes
PUT  /api/admin/referrals/codes/{id}
POST /api/admin/referrals/ctv/{userId}/remove-from-group
```

### 4.5. Hoa hồng

Đã có khung admin:

```text
/Admin/Commissions
```

Rank đề xuất:

```text
Mặc định: hơn 5 CTV dưới link REF  -> 2%
Hạng Bạc: hơn 10 CTV dưới link REF -> 3%
Hạng Vàng: hơn 50 CTV dưới link REF -> 5%
Hạng Kim cương: hơn 100 CTV dưới link REF -> 10%
```

Entity cần thêm:

```text
CommissionPolicy
ReferralCommission
CommissionRun
CommissionPayout
```

API cần thêm:

```text
GET  /api/admin/commissions/policies
POST /api/admin/commissions/policies
PUT  /api/admin/commissions/policies/{id}
GET  /api/admin/commissions
POST /api/admin/commissions/run
POST /api/admin/commissions/{id}/approve
POST /api/admin/commissions/{id}/pay
```

## 5. Tính Năng Bảo Mật Dữ Liệu Nhạy Cảm

### 5.1. Mã hóa dữ liệu nhạy cảm

Cần thêm:

- Mã hóa số CCCD/CMND trước khi lưu database.
- Mã hóa số tài khoản ngân hàng.
- Mã hóa tên chủ tài khoản.
- Chỉ giải mã khi cần hiển thị cho admin.
- Sử dụng AES-256 hoặc SQL Server Transparent Data Encryption (TDE).

### 5.2. Audit log bảo mật

Cần thêm:

- Log tất cả thao tác xem/edit sensitive data.
- Log: admin X viewed KYC documents of user Y at time Z with IP A.
- Log: admin X changed wallet balance for user Y from Z1 to Z2.
- Giữ lại lịch sử 1 năm (archive sau đó).

### 5.3. Xác thực 2 lớp (2FA)

Cần thêm:

- OTP qua SMS khi login từ device mới.
- TOTP (Time-based OTP) qua Google Authenticator.
- Backup codes nếu mất device.

### 5.4. Rate limiting

Cần thêm:

- Limit 5 lần sai password -> khóa 15 phút.
- Limit 10 request/phút cho API công khai.
- Limit 100 request/phút cho authenticated endpoints.
- Sử dụng Redis hoặc in-memory cache.

### 5.5. Monitoring & Alerting

Cần thêm:

- Alert nếu có quá nhiều failed login (brute force detection).
- Alert nếu có anomaly: cùng user login từ 2 địa chỉ IP khác nhau trong 1 phút.
- Alert nếu admin transfer điểm với số tiền lạ (outlier detection).
- Dashboard hiển thị các alert này.

## 6. Chức Năng Cần Phát Triển Mới

### 6.1. OTP và xác thực email

Cần thêm:

```text
PhoneOtp (phoneNumber, otp, expiryTime, attempts, status)
EmailVerificationToken (email, token, expiryTime, verified, createdAt)
```

API:

```text
POST /api/auth/send-phone-otp              (input: phone)
POST /api/auth/verify-phone-otp            (input: phone, otp)
POST /api/auth/send-email-verification     (input: email)
POST /api/auth/verify-email-code           (input: email, code)
GET  /api/auth/verify-email-link?token=X  (verify từ link email)
```

Yêu cầu:

- OTP 6 chữ số, hết hạn sau 5 phút.
- Tối đa 3 lần gõ sai OTP, sau đó phải gửi lại.
- Gửi OTP qua SMS (Nexmo/Twilio) hoặc in-app notification.
- Email verification link có hạn 24 giờ.
- Chỉ cho phép rút xu nếu email đã verified (cấu hình).

### 6.2. Quên mật khẩu

API:

```text
POST /api/auth/forgot-password
POST /api/auth/verify-reset-code
POST /api/auth/reset-password
```

Yêu cầu:

- Gửi OTP qua SĐT hoặc email.
- Token/OTP dùng một lần.
- Reset xong thu hồi refresh token cũ.

### 6.3. Đăng ký đối tác/tổ chức

API:

```text
POST /api/partners/register              (input: name, phone, email, business_license)
POST /api/partners/verify-phone          (input: phone, otp)
POST /api/partners/verify-email          (input: email, token)
GET  /api/admin/partners/pending         (list pending partners)
GET  /api/admin/partners                 (list all partners)
GET  /api/admin/partners/{id}            (detail partner)
POST /api/admin/partners/{id}/approve    (approve partner)
POST /api/admin/partners/{id}/reject     (reject with reason)
PUT  /api/admin/partners/{id}            (edit partner)
DELETE /api/admin/partners/{id}          (delete partner)
```

Entity:

```text
PartnerOrganization (id, name, phone, email, businessLicense, status, createdAt, approvedAt, approvedBy)
```

Yêu cầu:

- Đối tác/tổ chức phải được admin duyệt trước khi hoạt động.
- Được duyệt thì gán role `PartnerOrg`.
- Tự động tạo link REF và nhóm CTV riêng sau khi duyệt.
- Admin có thể reject với lý do cụ thể.
- Partner có thể edit tên/thông tin (admin duyệt lại nếu cần).

### 6.4. Visa/payment

Cần thêm:

- Payment provider.
- Create payment session.
- Webhook xác nhận thanh toán.
- Idempotency chống cộng tiền trùng.

API đề xuất:

```text
POST /api/payments/visa/create-session
POST /api/payments/visa/webhook
GET  /api/payments/my
```

Yêu cầu:

- Hỗ trợ thanh toán Visa/MasterCard/Debit card (payment processor).
- Tạo payment session với order info.
- Webhook nhận kết quả thanh toán (success/failed).
- Idempotency key chống cộng tiền trùng nếu webhook gửi lại.
- Webhook timeout fallback: polling để xác nhận trạng thái thanh toán.

## 7. Việc Đã Dọn Và Không Còn Giữ Trong Tài Liệu

Đã loại bỏ khỏi tài liệu roadmap:

- Nội dung `.md` cũ bị trùng.
- Các đoạn mô tả lỗi mã hóa cũ.
- Các checklist cũ không còn đúng với cấu trúc hiện tại.
- Các file root cũ không còn dùng.

Tài liệu chuẩn hiện tại chỉ còn file này:

```text
backend/CardWallet.Api/Docs/CARDWALLET_FEATURES_AND_ROADMAP.md
```

## 8. Ưu Tiên Nâng Cấp Tiếp Theo

Ưu tiên 1:

- Hoàn thiện KYC entity/API/upload file.
- Hoàn thiện rút xu: KYC required, khóa số dư, duyệt thủ công, tối thiểu 100.000 VND, tối đa 2 lần/tháng.
- Gắn role/permission policy vào admin API.

Ưu tiên 2:

- OTP SĐT và xác thực email.
- Quên mật khẩu.
- Cấu hình hệ thống trong admin.
- Audit log cho thao tác admin.

Ưu tiên 3:

- Đồng bộ bảng giá từ trang mẹ.
- Healthcheck API trang mẹ.
- Đối soát raw request/response giao dịch thẻ.

Ưu tiên 4:

- Link REF, nhóm đối tác/tổ chức, CTV thuộc nhóm.
- Hoa hồng theo rank.
- Job tính hoa hồng và payout.

Ưu tiên 5:

- Nạp xu qua Telegram admin.
- Visa/payment.
- Export report/log/database.

### Giai đoạn 1 (Tuần 1-2): Xác thực & Bảo mật

- [x] Hoàn thiện KYC entity/API/upload file.
- [x] Hoàn thiện rút xu: KYC required, khóa số dư, duyệt thủ công.
- [ ] OTP SMS & xác thực email.
- [ ] Quên mật khẩu.
- [ ] Gắn [Authorize] policy vào admin API.
- [ ] Audit log cho admin actions.

### Giai đoạn 2 (Tuần 3): Cấu hình & Quản lý

- [ ] Cấu hình hệ thống trong admin (SystemSetting).
- [ ] Cấu hình từ database thay vì hardcode.
- [ ] Tìm kiếm từ khóa thẻ cào (SearchAlias CRUD).
- [ ] Đối soát giao dịch: filter, export, search.

### Giai đoạn 3 (Tuần 4-5): Tích hợp & Đối tác

- [ ] Đồng bộ bảng giá từ trang mẹ (worker).
- [ ] Healthcheck & alert API trang mẹ.
- [ ] Đối soát raw request/response thẻ.
- [ ] Đăng ký đối tác/tổ chức.
- [ ] Link REF, nhóm đối tác, CTV.

### Giai đoạn 4 (Tuần 6-7): Tiền lương & Thu nhập

- [ ] Hoa hồng theo rank.
- [ ] Job tính hoa hồng tự động.
- [ ] Payout hoa hồng.
- [ ] Dashboard thống kê commission.

### Giai đoạn 5 (Tuần 8+): Nâng cấp bổ sung

- [ ] Nạp xu qua Telegram admin.
- [ ] Payment gateway (Visa/MasterCard).
- [ ] Export report/log/database.
- [ ] 2FA (TOTP).
- [ ] Rate limiting & DDoS protection.
- [ ] Monitoring & Alert anomaly.
- [ ] Encryption sensitive data (CCCD, bank account).

## 9. Lệnh Chạy Local Quan Trọng

Khởi động MySQL:

```powershell
cd D:\card-wallet-platform
docker compose up -d mysql
```

Xem log Docker:

```powershell
docker compose logs -f mysql
```

Dừng MySQL:

```powershell
docker compose down
```

Restart (reset database):

```powershell
docker compose down -v
docker compose up -d mysql
```

## 10. Công Nghệ & Stack Hiện Tại

Backend:

- .NET 8 (C#)
- ASP.NET Core Web API + Razor Pages
- Entity Framework Core (MySQL)
- JWT Authentication
- FluentValidation

Database:

- MySQL 8.0
- Docker container

Architecture:

- Clean Architecture (API, Application, Domain, Infrastructure)
- Repository Pattern
- Dependency Injection (DI)
- Service Layer Pattern

Cross-cutting:

- Exception Middleware
- Background Workers (CardExchangeWorker, ReconciliationWorker)
- Localization (i18n)

## 11. Convention & Best Practices

Naming:

- Entities: User, Wallet, CardTransaction, etc. (singular, PascalCase).
- DTOs: CreateUserRequest, UserResponse, etc. (Request/Response suffix).
- Services: IUserService, UserService (I prefix for interface).
- Repositories: IUserRepository, UserRepository (IUserRepository pattern).
- Controllers: UsersController, AdminUsersController (plural).

API Route:

- Public: /api/{resource}
- Admin: /api/admin/{resource}
- User: /api/{resource} (with [Authorize] + role check)

Entity Keys:

- Primary key: Id (Guid)
- Timestamps: CreatedAt, UpdatedAt (UTC)
- Soft delete: DeletedAt (nullable DateTime)

Database:

- Table name: dbo.{Entity} (singular).
- Foreign key: {EntityName}Id.
- Composite key: ok nhưng ít dùng.

Testing:

- Unit tests: CardWallet.Application.Tests/*.cs
- Integration tests: cần thêm CardWallet.Integration.Tests
- Test naming: {Method}_{Scenario}_{Expected}

## 12. Links & Resources

Tài liệu:

- CardWallet API Swagger: http://localhost:5000/swagger
- Docs folder: backend/CardWallet.Api/Docs/

Repository:

- Git: [tên repo]
- Branches: main, develop, feature/*, hotfix/*

Communication:

- Team: [team slack/groups]
- Roadmap: [trello/jira board]

Chạy API:

```powershell
dotnet run --project backend/CardWallet.Api/CardWallet.Api.csproj --urls http://0.0.0.0:5115
```

Admin dev:

```text
URL: http://127.0.0.1:5115/Admin/Login
Email: admin@example.com
Password: AdminPass123
```

Build:

```powershell
dotnet build backend/CardWallet.Api/CardWallet.Api.csproj
```
