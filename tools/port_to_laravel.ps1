param(
    [string]$Target = "D:\cardwallet-laravel",
    [string]$Source = "D:\card-wallet-platform"
)

$ErrorActionPreference = "Stop"

$resolvedTarget = [System.IO.Path]::GetFullPath($Target)
if ($resolvedTarget -ne "D:\cardwallet-laravel") {
    throw "Refusing to write outside D:\cardwallet-laravel. Got: $resolvedTarget"
}

function Ensure-Dir([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Write-Text([string]$RelativePath, [string]$Content) {
    $path = Join-Path $resolvedTarget $RelativePath
    Ensure-Dir ([System.IO.Path]::GetDirectoryName($path))
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $Content, $utf8NoBom)
}

function Copy-Dir([string]$From, [string]$To) {
    if (Test-Path -LiteralPath $From) {
        Ensure-Dir $To
        Copy-Item -LiteralPath (Join-Path $From "*") -Destination $To -Recurse -Force
    }
}

Ensure-Dir $resolvedTarget
foreach ($runtimeDir in @(
    "bootstrap\cache",
    "storage\app",
    "storage\framework\cache",
    "storage\framework\sessions",
    "storage\framework\views",
    "storage\logs"
)) {
    Ensure-Dir (Join-Path $resolvedTarget $runtimeDir)
}

Write-Text "composer.json" @'
{
  "name": "cardwallet/cardwallet-laravel",
  "type": "project",
  "description": "Laravel port of the CardWallet ASP.NET Core project.",
  "require": {
    "php": "^8.2",
    "firebase/php-jwt": "^6.10",
    "guzzlehttp/guzzle": "^7.8",
    "laravel/framework": "^12.0",
    "laravel/tinker": "^2.10"
  },
  "require-dev": {
    "fakerphp/faker": "^1.23",
    "laravel/pint": "^1.18",
    "mockery/mockery": "^1.6",
    "nunomaduro/collision": "^8.0",
    "phpunit/phpunit": "^11.0"
  },
  "autoload": {
    "psr-4": {
      "App\\": "app/",
      "Database\\Factories\\": "database/factories/",
      "Database\\Seeders\\": "database/seeders/"
    }
  },
  "autoload-dev": {
    "psr-4": {
      "Tests\\": "tests/"
    }
  },
  "scripts": {
    "post-autoload-dump": [
      "Illuminate\\Foundation\\ComposerScripts::postAutoloadDump",
      "@php artisan package:discover --ansi"
    ],
    "post-root-package-install": [
      "@php -r \"file_exists('.env') || copy('.env.example', '.env');\""
    ],
    "post-create-project-cmd": [
      "@php artisan key:generate --ansi"
    ]
  },
  "config": {
    "optimize-autoloader": true,
    "preferred-install": "dist",
    "sort-packages": true,
    "allow-plugins": {
      "pestphp/pest-plugin": true,
      "php-http/discovery": true
    }
  },
  "minimum-stability": "stable",
  "prefer-stable": true
}
'@

Write-Text "artisan" @'
#!/usr/bin/env php
<?php

use Illuminate\Foundation\Application;
use Symfony\Component\Console\Input\ArgvInput;

define('LARAVEL_START', microtime(true));

require __DIR__.'/vendor/autoload.php';

/** @var Application $app */
$app = require_once __DIR__.'/bootstrap/app.php';

$status = $app->handleCommand(new ArgvInput);

exit($status);
'@

Write-Text "bootstrap/app.php" @'
<?php

use App\Http\Middleware\AuthenticateBearer;
use App\Http\Middleware\EnsureAdmin;
use Illuminate\Foundation\Application;
use Illuminate\Foundation\Configuration\Exceptions;
use Illuminate\Foundation\Configuration\Middleware;

return Application::configure(basePath: dirname(__DIR__))
    ->withRouting(
        web: __DIR__.'/../routes/web.php',
        api: __DIR__.'/../routes/api.php',
        commands: __DIR__.'/../routes/console.php',
        health: '/up',
    )
    ->withMiddleware(function (Middleware $middleware): void {
        $middleware->alias([
            'auth.bearer' => AuthenticateBearer::class,
            'admin' => EnsureAdmin::class,
        ]);
    })
    ->withExceptions(function (Exceptions $exceptions): void {
        //
    })->create();
'@

Write-Text "routes/console.php" "<?php`n"

Write-Text "public/index.php" @'
<?php

use Illuminate\Foundation\Application;
use Illuminate\Http\Request;

define('LARAVEL_START', microtime(true));

if (file_exists($maintenance = __DIR__.'/../storage/framework/maintenance.php')) {
    require $maintenance;
}

require __DIR__.'/../vendor/autoload.php';

/** @var Application $app */
$app = require_once __DIR__.'/../bootstrap/app.php';

$app->handleRequest(Request::capture());
'@

Write-Text ".env.example" @'
APP_NAME=CardWallet
APP_ENV=local
APP_KEY=
APP_DEBUG=true
APP_URL=http://localhost:8000

APP_LOCALE=vi
APP_FALLBACK_LOCALE=en

LOG_CHANNEL=stack
LOG_LEVEL=debug

DB_CONNECTION=mysql
DB_HOST=127.0.0.1
DB_PORT=3306
DB_DATABASE=card_wallet_db
DB_USERNAME=root
DB_PASSWORD=

JWT_SECRET=change-this-secret-to-a-long-random-value
JWT_ISSUER=CardWallet
JWT_AUDIENCE=CardWalletClient
JWT_TTL_MINUTES=60
REFRESH_TOKEN_DAYS=30

ADMIN_EMAIL=admin@example.com
ADMIN_PHONE=0900000000
ADMIN_PASSWORD=AdminPass123
ADMIN_SEED_ON_STARTUP=false

PARENT_CARD_API_BASE_URL=
PARENT_CARD_API_KEY=
'@

Write-Text "app/Support/Uuid.php" @'
<?php

namespace App\Support;

use Illuminate\Support\Str;

trait Uuid
{
    protected static function bootUuid(): void
    {
        static::creating(function ($model): void {
            $key = $model->getKeyName();
            if (empty($model->{$key})) {
                $model->{$key} = (string) Str::uuid();
            }
        });
    }
}
'@

Write-Text "app/Models/User.php" @'
<?php

namespace App\Models;

use App\Support\Uuid;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\Relations\HasOne;

class User extends Model
{
    use Uuid;

    protected $table = 'users';
    protected $primaryKey = 'Id';
    public $incrementing = false;
    protected $keyType = 'string';
    public $timestamps = false;

    protected $hidden = ['PasswordHash'];

    protected $fillable = [
        'Id', 'FullName', 'PhoneNumber', 'IsPhoneVerified', 'Email', 'IsEmailVerified',
        'PasswordHash', 'Status', 'FailedLoginAttempts', 'LockoutEndAt', 'CreatedAt',
        'UpdatedAt', 'IsDeleted', 'DeletedAt', 'Role', 'ParentUserId', 'CanManageUsers',
        'CanManageTasks', 'CanApproveTasks', 'CanApproveKycWithdraw', 'CanTransferPoints',
        'CanManageBlog', 'CanExportReports',
    ];

    protected $casts = [
        'IsPhoneVerified' => 'boolean',
        'IsEmailVerified' => 'boolean',
        'IsDeleted' => 'boolean',
        'CanManageUsers' => 'boolean',
        'CanManageTasks' => 'boolean',
        'CanApproveTasks' => 'boolean',
        'CanApproveKycWithdraw' => 'boolean',
        'CanTransferPoints' => 'boolean',
        'CanManageBlog' => 'boolean',
        'CanExportReports' => 'boolean',
        'CreatedAt' => 'datetime',
        'UpdatedAt' => 'datetime',
        'DeletedAt' => 'datetime',
        'LockoutEndAt' => 'datetime',
    ];

    public function wallet(): HasOne
    {
        return $this->hasOne(Wallet::class, 'UserId', 'Id');
    }

    public function refreshTokens(): HasMany
    {
        return $this->hasMany(RefreshToken::class, 'UserId', 'Id');
    }

    public function cardTransactions(): HasMany
    {
        return $this->hasMany(CardTransaction::class, 'UserId', 'Id');
    }
}
'@

Write-Text "app/Models/Wallet.php" @'
<?php

namespace App\Models;

use App\Support\Uuid;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\Relations\HasMany;

class Wallet extends Model
{
    use Uuid;

    protected $table = 'wallets';
    protected $primaryKey = 'Id';
    public $incrementing = false;
    protected $keyType = 'string';
    public $timestamps = false;

    protected $fillable = ['Id', 'UserId', 'Balance', 'LockedBalance', 'Currency', 'CreatedAt', 'UpdatedAt'];

    protected $casts = [
        'Balance' => 'integer',
        'LockedBalance' => 'integer',
        'CreatedAt' => 'datetime',
        'UpdatedAt' => 'datetime',
    ];

    public function user(): BelongsTo
    {
        return $this->belongsTo(User::class, 'UserId', 'Id');
    }

    public function transactions(): HasMany
    {
        return $this->hasMany(Transaction::class, 'WalletId', 'Id');
    }
}
'@

Write-Text "app/Models/Transaction.php" @'
<?php

namespace App\Models;

use App\Support\Uuid;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class Transaction extends Model
{
    use Uuid;

    protected $table = 'transactions';
    protected $primaryKey = 'Id';
    public $incrementing = false;
    protected $keyType = 'string';
    public $timestamps = false;

    protected $fillable = [
        'Id', 'WalletId', 'UserId', 'Type', 'Status', 'Amount', 'BalanceBefore',
        'BalanceAfter', 'ReferenceCode', 'Description', 'IdempotencyKey', 'CreatedAt',
    ];

    protected $casts = [
        'Amount' => 'integer',
        'BalanceBefore' => 'integer',
        'BalanceAfter' => 'integer',
        'CreatedAt' => 'datetime',
    ];

    public function wallet(): BelongsTo
    {
        return $this->belongsTo(Wallet::class, 'WalletId', 'Id');
    }
}
'@

Write-Text "app/Models/CardRate.php" @'
<?php

namespace App\Models;

use App\Support\Uuid;
use Illuminate\Database\Eloquent\Model;

class CardRate extends Model
{
    use Uuid;

    protected $table = 'card_rates';
    protected $primaryKey = 'Id';
    public $incrementing = false;
    protected $keyType = 'string';
    public $timestamps = false;

    protected $fillable = ['Id', 'Provider', 'FaceValue', 'DiscountPercent', 'IsActive', 'CreatedAt', 'UpdatedAt'];

    protected $casts = [
        'FaceValue' => 'integer',
        'DiscountPercent' => 'decimal:2',
        'IsActive' => 'boolean',
        'CreatedAt' => 'datetime',
        'UpdatedAt' => 'datetime',
    ];
}
'@

Write-Text "app/Models/CardTransaction.php" @'
<?php

namespace App\Models;

use App\Support\Uuid;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class CardTransaction extends Model
{
    use Uuid;

    protected $table = 'card_transactions';
    protected $primaryKey = 'Id';
    public $incrementing = false;
    protected $keyType = 'string';
    public $timestamps = false;

    protected $fillable = [
        'Id', 'UserId', 'Provider', 'FaceValue', 'DiscountPercent', 'ExpectedReceiveAmount',
        'ActualReceiveAmount', 'CardCode', 'Serial', 'Status', 'ParentTransactionCode',
        'FailureReason', 'IdempotencyKey', 'RetryCount', 'NextRetryAt', 'ParentRequestRaw',
        'ParentResponseRaw', 'ErrorMessage', 'CreatedAt', 'ProcessedAt', 'CompletedAt',
    ];

    protected $casts = [
        'FaceValue' => 'integer',
        'DiscountPercent' => 'decimal:2',
        'ExpectedReceiveAmount' => 'integer',
        'ActualReceiveAmount' => 'integer',
        'RetryCount' => 'integer',
        'CreatedAt' => 'datetime',
        'ProcessedAt' => 'datetime',
        'CompletedAt' => 'datetime',
        'NextRetryAt' => 'datetime',
    ];

    public function user(): BelongsTo
    {
        return $this->belongsTo(User::class, 'UserId', 'Id');
    }
}
'@

Write-Text "app/Models/RefreshToken.php" @'
<?php

namespace App\Models;

use App\Support\Uuid;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class RefreshToken extends Model
{
    use Uuid;

    protected $table = 'refresh_tokens';
    protected $primaryKey = 'Id';
    public $incrementing = false;
    protected $keyType = 'string';
    public $timestamps = false;

    protected $fillable = [
        'Id', 'UserId', 'Token', 'ExpiresAt', 'IsRevoked', 'RevokedAt', 'ReplacedByToken', 'CreatedAt',
    ];

    protected $casts = [
        'IsRevoked' => 'boolean',
        'ExpiresAt' => 'datetime',
        'RevokedAt' => 'datetime',
        'CreatedAt' => 'datetime',
    ];

    public function user(): BelongsTo
    {
        return $this->belongsTo(User::class, 'UserId', 'Id');
    }

    public function getIsActiveAttribute(): bool
    {
        return !$this->IsRevoked && $this->ExpiresAt && $this->ExpiresAt->isFuture();
    }
}
'@

Write-Text "app/Models/SearchAlias.php" @'
<?php

namespace App\Models;

use App\Support\Uuid;
use Illuminate\Database\Eloquent\Model;

class SearchAlias extends Model
{
    use Uuid;

    protected $table = 'search_aliases';
    protected $primaryKey = 'Id';
    public $incrementing = false;
    protected $keyType = 'string';
    public $timestamps = false;

    protected $fillable = ['Id', 'Alias', 'EntityType', 'Target', 'CreatedAt', 'UpdatedAt'];
    protected $casts = ['CreatedAt' => 'datetime', 'UpdatedAt' => 'datetime'];
}
'@

Write-Text "app/Models/KycRequest.php" @'
<?php

namespace App\Models;

use App\Support\Uuid;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class KycRequest extends Model
{
    use Uuid;

    protected $table = 'kyc_requests';
    protected $primaryKey = 'Id';
    public $incrementing = false;
    protected $keyType = 'string';
    public $timestamps = false;

    protected $fillable = [
        'Id', 'UserId', 'FrontIdImagePath', 'BackIdImagePath', 'SelfieImagePath',
        'Status', 'RejectReason', 'CreatedAt', 'ReviewedAt', 'ReviewedByUserId',
    ];

    protected $casts = ['CreatedAt' => 'datetime', 'ReviewedAt' => 'datetime'];

    public function user(): BelongsTo
    {
        return $this->belongsTo(User::class, 'UserId', 'Id');
    }
}
'@

Write-Text "app/Models/WithdrawalRequest.php" @'
<?php

namespace App\Models;

use App\Support\Uuid;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class WithdrawalRequest extends Model
{
    use Uuid;

    protected $table = 'withdrawal_requests';
    protected $primaryKey = 'Id';
    public $incrementing = false;
    protected $keyType = 'string';
    public $timestamps = false;

    protected $fillable = [
        'Id', 'UserId', 'Amount', 'BankName', 'BankAccountNumber', 'BankAccountName',
        'Status', 'RejectReason', 'CreatedAt', 'ReviewedAt', 'ReviewedByUserId',
    ];

    protected $casts = ['Amount' => 'integer', 'CreatedAt' => 'datetime', 'ReviewedAt' => 'datetime'];

    public function user(): BelongsTo
    {
        return $this->belongsTo(User::class, 'UserId', 'Id');
    }
}
'@

Write-Text "app/Models/SystemSetting.php" @'
<?php

namespace App\Models;

use App\Support\Uuid;
use Illuminate\Database\Eloquent\Model;

class SystemSetting extends Model
{
    use Uuid;

    protected $table = 'system_settings';
    protected $primaryKey = 'Id';
    public $incrementing = false;
    protected $keyType = 'string';
    public $timestamps = false;

    protected $fillable = ['Id', 'Key', 'Value', 'Description', 'CreatedAt', 'UpdatedAt'];
    protected $casts = ['CreatedAt' => 'datetime', 'UpdatedAt' => 'datetime'];
}
'@

Write-Text "app/Services/TokenService.php" @'
<?php

namespace App\Services;

use App\Models\RefreshToken;
use App\Models\User;
use Carbon\CarbonImmutable;
use Firebase\JWT\JWT;
use Illuminate\Support\Str;

class TokenService
{
    public function accessToken(User $user): string
    {
        $now = CarbonImmutable::now('UTC');
        $ttl = (int) config('cardwallet.jwt_ttl_minutes', 60);

        return JWT::encode([
            'iss' => config('cardwallet.jwt_issuer', 'CardWallet'),
            'aud' => config('cardwallet.jwt_audience', 'CardWalletClient'),
            'iat' => $now->timestamp,
            'nbf' => $now->timestamp,
            'exp' => $now->addMinutes($ttl)->timestamp,
            'sub' => $user->Id,
            'role' => $user->Role,
            'email' => $user->Email,
            'canManageUsers' => (bool) $user->CanManageUsers,
            'canTransferPoints' => (bool) $user->CanTransferPoints,
            'canApproveKycWithdraw' => (bool) $user->CanApproveKycWithdraw,
        ], config('cardwallet.jwt_secret'), 'HS256');
    }

    public function refreshToken(User $user): RefreshToken
    {
        return RefreshToken::create([
            'UserId' => $user->Id,
            'Token' => Str::random(96),
            'ExpiresAt' => now('UTC')->addDays((int) config('cardwallet.refresh_token_days', 30)),
            'IsRevoked' => false,
            'CreatedAt' => now('UTC'),
        ]);
    }

    public function authPayload(User $user, RefreshToken $refreshToken): array
    {
        return [
            'userId' => $user->Id,
            'fullName' => $user->FullName,
            'email' => $user->Email,
            'phoneNumber' => $user->PhoneNumber,
            'accessToken' => $this->accessToken($user),
            'refreshToken' => $refreshToken->Token,
        ];
    }
}
'@

Write-Text "app/Services/WalletService.php" @'
<?php

namespace App\Services;

use App\Models\Transaction;
use App\Models\Wallet;
use Illuminate\Support\Facades\DB;
use InvalidArgumentException;

class WalletService
{
    public function ensureWallet(string $userId): Wallet
    {
        return Wallet::firstOrCreate(
            ['UserId' => $userId],
            ['Balance' => 0, 'LockedBalance' => 0, 'Currency' => 'XU', 'CreatedAt' => now('UTC')]
        );
    }

    public function balance(string $userId): array
    {
        $wallet = $this->ensureWallet($userId);

        return [
            'balance' => $wallet->Balance,
            'lockedBalance' => $wallet->LockedBalance,
            'currency' => $wallet->Currency,
        ];
    }

    public function adjust(string $userId, int $amount, string $type, string $description = '', ?string $referenceCode = null, ?string $idempotencyKey = null): Transaction
    {
        if ($amount === 0) {
            throw new InvalidArgumentException('So tien giao dich khong hop le.');
        }

        return DB::transaction(function () use ($userId, $amount, $type, $description, $referenceCode, $idempotencyKey): Transaction {
            if ($idempotencyKey) {
                $existing = Transaction::where('IdempotencyKey', $idempotencyKey)->first();
                if ($existing) {
                    return $existing;
                }
            }

            $wallet = Wallet::where('UserId', $userId)->lockForUpdate()->first() ?? $this->ensureWallet($userId);
            $wallet = Wallet::where('Id', $wallet->Id)->lockForUpdate()->firstOrFail();

            $before = (int) $wallet->Balance;
            $after = $before + $amount;
            if ($after < 0) {
                throw new InvalidArgumentException('So du khong du.');
            }

            $wallet->Balance = $after;
            $wallet->UpdatedAt = now('UTC');
            $wallet->save();

            return Transaction::create([
                'WalletId' => $wallet->Id,
                'UserId' => $userId,
                'Type' => $type,
                'Status' => 'Completed',
                'Amount' => $amount,
                'BalanceBefore' => $before,
                'BalanceAfter' => $after,
                'ReferenceCode' => $referenceCode,
                'Description' => $description,
                'IdempotencyKey' => $idempotencyKey,
                'CreatedAt' => now('UTC'),
            ]);
        });
    }
}
'@

Write-Text "app/Services/CardExchangeService.php" @'
<?php

namespace App\Services;

use App\Models\CardRate;
use App\Models\CardTransaction;
use InvalidArgumentException;

class CardExchangeService
{
    public function submit(string $userId, array $data): CardTransaction
    {
        if (!empty($data['idempotencyKey'])) {
            $existing = CardTransaction::where('IdempotencyKey', $data['idempotencyKey'])->first();
            if ($existing) {
                return $existing;
            }
        }

        $provider = strtoupper(trim($data['provider']));
        $faceValue = (int) $data['faceValue'];
        $rate = CardRate::where('Provider', $provider)
            ->where('FaceValue', $faceValue)
            ->where('IsActive', true)
            ->first();

        if (!$rate) {
            throw new InvalidArgumentException('Khong tim thay bang gia cho nha mang/menh gia.');
        }

        $expected = (int) floor($rate->FaceValue * (1 - ((float) $rate->DiscountPercent / 100)));

        return CardTransaction::create([
            'UserId' => $userId,
            'Provider' => $provider,
            'FaceValue' => $faceValue,
            'DiscountPercent' => $rate->DiscountPercent,
            'ExpectedReceiveAmount' => $expected,
            'CardCode' => $data['cardCode'],
            'Serial' => $data['serial'],
            'Status' => 'Pending',
            'IdempotencyKey' => $data['idempotencyKey'] ?? null,
            'RetryCount' => 0,
            'CreatedAt' => now('UTC'),
        ]);
    }

    public function dto(CardTransaction $tx): array
    {
        return [
            'id' => $tx->Id,
            'userId' => $tx->UserId,
            'provider' => $tx->Provider,
            'faceValue' => $tx->FaceValue,
            'discountPercent' => (float) $tx->DiscountPercent,
            'expectedReceiveAmount' => $tx->ExpectedReceiveAmount,
            'actualReceiveAmount' => $tx->ActualReceiveAmount,
            'cardCode' => $tx->CardCode,
            'serial' => $tx->Serial,
            'status' => $tx->Status,
            'failureReason' => $tx->FailureReason,
            'idempotencyKey' => $tx->IdempotencyKey,
            'createdAt' => optional($tx->CreatedAt)->toISOString(),
            'completedAt' => optional($tx->CompletedAt)->toISOString(),
        ];
    }
}
'@

Write-Text "app/Http/Middleware/AuthenticateBearer.php" @'
<?php

namespace App\Http\Middleware;

use App\Models\User;
use Closure;
use Firebase\JWT\JWT;
use Firebase\JWT\Key;
use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\Response;

class AuthenticateBearer
{
    public function handle(Request $request, Closure $next): Response
    {
        $token = $request->bearerToken();
        if (!$token) {
            return response()->json(['message' => 'Unauthenticated.'], 401);
        }

        try {
            $payload = JWT::decode($token, new Key(config('cardwallet.jwt_secret'), 'HS256'));
        } catch (\Throwable) {
            return response()->json(['message' => 'Invalid token.'], 401);
        }

        $user = User::where('Id', $payload->sub ?? null)
            ->where('Status', 'Active')
            ->where('IsDeleted', false)
            ->first();

        if (!$user) {
            return response()->json(['message' => 'Unauthenticated.'], 401);
        }

        $request->setUserResolver(fn () => $user);

        return $next($request);
    }
}
'@

Write-Text "app/Http/Middleware/EnsureAdmin.php" @'
<?php

namespace App\Http\Middleware;

use Closure;
use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\Response;

class EnsureAdmin
{
    public function handle(Request $request, Closure $next): Response
    {
        $user = $request->user();
        if (!$user || $user->Role !== 'Admin') {
            return response()->json(['message' => 'Forbidden.'], 403);
        }

        return $next($request);
    }
}
'@

Write-Text "app/Http/Controllers/Controller.php" @'
<?php

namespace App\Http\Controllers;

abstract class Controller
{
    //
}
'@

Write-Text "app/Http/Controllers/AuthController.php" @'
<?php

namespace App\Http\Controllers;

use App\Models\RefreshToken;
use App\Models\User;
use App\Services\TokenService;
use App\Services\WalletService;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Hash;

class AuthController extends Controller
{
    public function register(Request $request, TokenService $tokens, WalletService $wallets): JsonResponse
    {
        $data = $request->validate([
            'fullName' => ['required', 'string', 'max:150'],
            'phoneNumber' => ['required', 'string', 'max:20', 'unique:users,PhoneNumber'],
            'email' => ['required', 'email', 'max:150', 'unique:users,Email'],
            'password' => ['required', 'string', 'min:6'],
        ]);

        $payload = DB::transaction(function () use ($data, $tokens, $wallets) {
            $user = User::create([
                'FullName' => trim($data['fullName']),
                'PhoneNumber' => trim($data['phoneNumber']),
                'IsPhoneVerified' => false,
                'Email' => strtolower(trim($data['email'])),
                'IsEmailVerified' => false,
                'PasswordHash' => Hash::make($data['password']),
                'Status' => 'Active',
                'FailedLoginAttempts' => 0,
                'CreatedAt' => now('UTC'),
                'Role' => 'Customer',
                'IsDeleted' => false,
            ]);

            $wallets->ensureWallet($user->Id);
            $refresh = $tokens->refreshToken($user);

            return $tokens->authPayload($user, $refresh);
        });

        return response()->json($payload);
    }

    public function login(Request $request, TokenService $tokens): JsonResponse
    {
        $data = $request->validate([
            'login' => ['required', 'string'],
            'password' => ['required', 'string'],
        ]);

        $login = trim($data['login']);
        $user = User::where('Email', strtolower($login))->orWhere('PhoneNumber', $login)->first();

        if (!$user || $user->Status !== 'Active' || !Hash::check($data['password'], $user->PasswordHash)) {
            if ($user) {
                $user->FailedLoginAttempts = ((int) $user->FailedLoginAttempts) + 1;
                if ($user->FailedLoginAttempts >= 5) {
                    $user->LockoutEndAt = now('UTC')->addMinutes(15);
                }
                $user->save();
            }
            return response()->json(['message' => 'Tai khoan hoac mat khau khong dung.'], 401);
        }

        if ($user->LockoutEndAt && $user->LockoutEndAt->isFuture()) {
            return response()->json(['message' => 'Tai khoan dang bi khoa. Vui long thu lai sau.'], 401);
        }

        $user->FailedLoginAttempts = 0;
        $user->LockoutEndAt = null;
        $user->UpdatedAt = now('UTC');
        $user->save();

        return response()->json($tokens->authPayload($user, $tokens->refreshToken($user)));
    }

    public function refresh(Request $request, TokenService $tokens): JsonResponse
    {
        $data = $request->validate(['refreshToken' => ['required', 'string']]);
        $old = RefreshToken::with('user')->where('Token', $data['refreshToken'])->first();

        if (!$old || !$old->is_active || !$old->user || $old->user->Status !== 'Active') {
            return response()->json(['message' => 'Refresh token khong hop le.'], 401);
        }

        $new = DB::transaction(function () use ($old, $tokens) {
            $new = $tokens->refreshToken($old->user);
            $old->IsRevoked = true;
            $old->RevokedAt = now('UTC');
            $old->ReplacedByToken = $new->Token;
            $old->save();
            return $new;
        });

        return response()->json($tokens->authPayload($old->user, $new));
    }

    public function logout(Request $request): JsonResponse
    {
        $data = $request->validate(['refreshToken' => ['required', 'string']]);
        RefreshToken::where('Token', $data['refreshToken'])->update([
            'IsRevoked' => true,
            'RevokedAt' => now('UTC'),
        ]);

        return response()->json(['message' => 'Dang xuat thanh cong.']);
    }
}
'@

Write-Text "app/Http/Controllers/WalletController.php" @'
<?php

namespace App\Http\Controllers;

use App\Services\WalletService;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use InvalidArgumentException;

class WalletController extends Controller
{
    public function balance(Request $request, WalletService $wallets): JsonResponse
    {
        return response()->json($wallets->balance($request->user()->Id));
    }

    public function transactions(Request $request): JsonResponse
    {
        $items = $request->user()->wallet?->transactions()
            ->orderByDesc('CreatedAt')
            ->paginate((int) $request->query('pageSize', 20));

        return response()->json($items ?? []);
    }

    public function deposit(): JsonResponse
    {
        return response()->json(['message' => 'Endpoint nap vi truc tiep da bi khoa de bao toan tong cung.'], 400);
    }

    public function withdraw(Request $request, WalletService $wallets): JsonResponse
    {
        $data = $request->validate([
            'amount' => ['required', 'integer', 'min:1'],
            'description' => ['nullable', 'string', 'max:255'],
            'referenceId' => ['nullable', 'string', 'max:100'],
        ]);

        try {
            $tx = $wallets->adjust(
                $request->user()->Id,
                -abs((int) $data['amount']),
                'Withdraw',
                $data['description'] ?? '',
                $data['referenceId'] ?? null
            );
        } catch (InvalidArgumentException $e) {
            return response()->json(['message' => $e->getMessage()], 400);
        }

        return response()->json($tx);
    }
}
'@

Write-Text "app/Http/Controllers/CardExchangeController.php" @'
<?php

namespace App\Http\Controllers;

use App\Models\CardTransaction;
use App\Services\CardExchangeService;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use InvalidArgumentException;

class CardExchangeController extends Controller
{
    public function submit(Request $request, CardExchangeService $cards): JsonResponse
    {
        $data = $request->validate([
            'provider' => ['required', 'string', 'max:50'],
            'faceValue' => ['required', 'integer', 'min:1000'],
            'cardCode' => ['required', 'string', 'max:200'],
            'serial' => ['required', 'string', 'max:200'],
            'idempotencyKey' => ['nullable', 'string', 'max:200'],
        ]);

        try {
            $tx = $cards->submit($request->user()->Id, $data);
        } catch (InvalidArgumentException $e) {
            return response()->json(['message' => $e->getMessage()], 400);
        }

        return response()->json($cards->dto($tx));
    }

    public function myTransactions(Request $request, CardExchangeService $cards): JsonResponse
    {
        $items = CardTransaction::where('UserId', $request->user()->Id)->orderByDesc('CreatedAt')->get();
        return response()->json($items->map(fn ($tx) => $cards->dto($tx)));
    }

    public function show(string $id, CardExchangeService $cards): JsonResponse
    {
        $tx = CardTransaction::find($id);
        return $tx ? response()->json($cards->dto($tx)) : response()->json(['message' => 'Not found.'], 404);
    }

    public function status(string $id): JsonResponse
    {
        $tx = CardTransaction::find($id);
        if (!$tx) {
            return response()->json(['message' => 'Not found.'], 404);
        }

        return response()->json([
            'id' => $tx->Id,
            'status' => $tx->Status,
            'expectedReceiveAmount' => $tx->ExpectedReceiveAmount,
            'actualReceiveAmount' => $tx->ActualReceiveAmount,
            'message' => $tx->Status === 'Processing' ? 'The dang duoc xu ly' : ($tx->Status === 'Pending' ? 'Cho xu ly' : ($tx->FailureReason ?? '')),
        ]);
    }
}
'@

Write-Text "app/Http/Controllers/CardRateController.php" @'
<?php

namespace App\Http\Controllers;

use App\Models\CardRate;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class CardRateController extends Controller
{
    public function index(Request $request): JsonResponse
    {
        $query = CardRate::query();
        if (!$request->boolean('includeInactive')) {
            $query->where('IsActive', true);
        }

        return response()->json($query->orderBy('Provider')->orderBy('FaceValue')->get());
    }

    public function upsert(Request $request): JsonResponse
    {
        $data = $request->validate([
            'provider' => ['required', 'string', 'max:50'],
            'faceValue' => ['required', 'integer', 'min:1000'],
            'discountPercent' => ['required', 'numeric', 'min:0', 'max:100'],
            'isActive' => ['sometimes', 'boolean'],
        ]);

        $rate = CardRate::updateOrCreate(
            ['Provider' => strtoupper($data['provider']), 'FaceValue' => (int) $data['faceValue']],
            [
                'DiscountPercent' => $data['discountPercent'],
                'IsActive' => $data['isActive'] ?? true,
                'UpdatedAt' => now('UTC'),
                'CreatedAt' => now('UTC'),
            ]
        );

        return response()->json($rate);
    }
}
'@

Write-Text "app/Http/Controllers/AdminController.php" @'
<?php

namespace App\Http\Controllers;

use App\Models\CardTransaction;
use App\Models\KycRequest;
use App\Models\Transaction;
use App\Models\User;
use App\Models\Wallet;
use App\Models\WithdrawalRequest;
use App\Services\WalletService;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Hash;

class AdminController extends Controller
{
    public function dashboard(): JsonResponse
    {
        return response()->json([
            'users' => User::where('IsDeleted', false)->count(),
            'walletSupply' => Wallet::sum('Balance'),
            'cardTransactions' => CardTransaction::count(),
            'pendingKyc' => KycRequest::where('Status', 'Pending')->count(),
            'pendingWithdrawals' => WithdrawalRequest::where('Status', 'Pending')->count(),
        ]);
    }

    public function users(Request $request): JsonResponse
    {
        $q = trim((string) $request->query('q', ''));
        $users = User::query()
            ->where('IsDeleted', false)
            ->when($q !== '', fn ($query) => $query->where(function ($inner) use ($q) {
                $inner->where('Email', 'like', "%$q%")
                    ->orWhere('PhoneNumber', 'like', "%$q%")
                    ->orWhere('FullName', 'like', "%$q%");
            }))
            ->orderByDesc('CreatedAt')
            ->paginate((int) $request->query('pageSize', 20));

        return response()->json($users);
    }

    public function createUser(Request $request): JsonResponse
    {
        $data = $request->validate([
            'fullName' => ['required', 'string', 'max:150'],
            'phoneNumber' => ['required', 'string', 'max:20', 'unique:users,PhoneNumber'],
            'email' => ['required', 'email', 'max:150', 'unique:users,Email'],
            'password' => ['required', 'string', 'min:6'],
            'role' => ['nullable', 'string', 'max:40'],
        ]);

        $user = DB::transaction(function () use ($data) {
            $user = User::create([
                'FullName' => $data['fullName'],
                'PhoneNumber' => $data['phoneNumber'],
                'IsPhoneVerified' => false,
                'Email' => strtolower($data['email']),
                'IsEmailVerified' => false,
                'PasswordHash' => Hash::make($data['password']),
                'Status' => 'Active',
                'FailedLoginAttempts' => 0,
                'CreatedAt' => now('UTC'),
                'Role' => $data['role'] ?? 'Customer',
                'IsDeleted' => false,
            ]);
            Wallet::create(['UserId' => $user->Id, 'Balance' => 0, 'LockedBalance' => 0, 'Currency' => 'XU', 'CreatedAt' => now('UTC')]);
            return $user;
        });

        return response()->json($user, 201);
    }

    public function updateUserStatus(Request $request, string $id): JsonResponse
    {
        $data = $request->validate(['status' => ['required', 'string', 'max:30']]);
        $user = User::findOrFail($id);
        $user->Status = $data['status'];
        $user->UpdatedAt = now('UTC');
        $user->save();
        return response()->json($user);
    }

    public function transferPoints(Request $request, WalletService $wallets): JsonResponse
    {
        $data = $request->validate([
            'targetUserId' => ['required', 'string', 'exists:users,Id'],
            'amount' => ['required', 'integer', 'min:1'],
            'reason' => ['nullable', 'string', 'max:255'],
        ]);

        $tx = $wallets->adjust($data['targetUserId'], (int) $data['amount'], 'AdminTransferIn', $data['reason'] ?? 'Admin chuyen diem', null);
        return response()->json($tx);
    }

    public function transactions(Request $request): JsonResponse
    {
        return response()->json(Transaction::orderByDesc('CreatedAt')->paginate((int) $request->query('pageSize', 20)));
    }
}
'@

Write-Text "app/Http/Controllers/KycController.php" @'
<?php

namespace App\Http\Controllers;

use App\Models\KycRequest;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class KycController extends Controller
{
    public function store(Request $request): JsonResponse
    {
        $data = $request->validate([
            'frontIdImagePath' => ['required', 'string', 'max:500'],
            'backIdImagePath' => ['required', 'string', 'max:500'],
            'selfieImagePath' => ['required', 'string', 'max:500'],
        ]);

        $kyc = KycRequest::create([
            'UserId' => $request->user()->Id,
            'FrontIdImagePath' => $data['frontIdImagePath'],
            'BackIdImagePath' => $data['backIdImagePath'],
            'SelfieImagePath' => $data['selfieImagePath'],
            'Status' => 'Pending',
            'CreatedAt' => now('UTC'),
        ]);

        return response()->json($kyc, 201);
    }

    public function mine(Request $request): JsonResponse
    {
        return response()->json(KycRequest::where('UserId', $request->user()->Id)->orderByDesc('CreatedAt')->get());
    }

    public function index(): JsonResponse
    {
        return response()->json(KycRequest::with('user')->orderByDesc('CreatedAt')->paginate(20));
    }

    public function review(Request $request, string $id): JsonResponse
    {
        $data = $request->validate([
            'status' => ['required', 'in:Approved,Rejected'],
            'rejectReason' => ['nullable', 'string', 'max:500'],
        ]);

        $kyc = KycRequest::findOrFail($id);
        $kyc->Status = $data['status'];
        $kyc->RejectReason = $data['rejectReason'] ?? null;
        $kyc->ReviewedAt = now('UTC');
        $kyc->ReviewedByUserId = $request->user()->Id;
        $kyc->save();

        return response()->json($kyc);
    }
}
'@

Write-Text "app/Http/Controllers/WithdrawalController.php" @'
<?php

namespace App\Http\Controllers;

use App\Models\WithdrawalRequest;
use App\Services\WalletService;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class WithdrawalController extends Controller
{
    public function store(Request $request, WalletService $wallets): JsonResponse
    {
        $data = $request->validate([
            'amount' => ['required', 'integer', 'min:1'],
            'bankName' => ['required', 'string', 'max:120'],
            'bankAccountNumber' => ['required', 'string', 'max:80'],
            'bankAccountName' => ['required', 'string', 'max:150'],
        ]);

        $wallets->adjust($request->user()->Id, -abs((int) $data['amount']), 'Withdraw', 'Yeu cau rut tien dang cho duyet');

        $withdrawal = WithdrawalRequest::create([
            'UserId' => $request->user()->Id,
            'Amount' => $data['amount'],
            'BankName' => $data['bankName'],
            'BankAccountNumber' => $data['bankAccountNumber'],
            'BankAccountName' => $data['bankAccountName'],
            'Status' => 'Pending',
            'CreatedAt' => now('UTC'),
        ]);

        return response()->json($withdrawal, 201);
    }

    public function mine(Request $request): JsonResponse
    {
        return response()->json(WithdrawalRequest::where('UserId', $request->user()->Id)->orderByDesc('CreatedAt')->get());
    }

    public function index(): JsonResponse
    {
        return response()->json(WithdrawalRequest::with('user')->orderByDesc('CreatedAt')->paginate(20));
    }

    public function review(Request $request, string $id): JsonResponse
    {
        $data = $request->validate([
            'status' => ['required', 'in:Approved,Rejected'],
            'rejectReason' => ['nullable', 'string', 'max:500'],
        ]);

        $withdrawal = WithdrawalRequest::findOrFail($id);
        $withdrawal->Status = $data['status'];
        $withdrawal->RejectReason = $data['rejectReason'] ?? null;
        $withdrawal->ReviewedAt = now('UTC');
        $withdrawal->ReviewedByUserId = $request->user()->Id;
        $withdrawal->save();

        return response()->json($withdrawal);
    }
}
'@

Write-Text "config/cardwallet.php" @'
<?php

return [
    'jwt_secret' => env('JWT_SECRET', 'change-this-secret'),
    'jwt_issuer' => env('JWT_ISSUER', 'CardWallet'),
    'jwt_audience' => env('JWT_AUDIENCE', 'CardWalletClient'),
    'jwt_ttl_minutes' => env('JWT_TTL_MINUTES', 60),
    'refresh_token_days' => env('REFRESH_TOKEN_DAYS', 30),
    'parent_card_api_base_url' => env('PARENT_CARD_API_BASE_URL'),
    'parent_card_api_key' => env('PARENT_CARD_API_KEY'),
];
'@

Write-Text "database/migrations/2026_06_12_000007_create_search_aliases_table.php" @'
<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('search_aliases', function (Blueprint $table) {
            $table->char('Id', 36)->primary();
            $table->string('Alias', 100);
            $table->string('EntityType', 80);
            $table->string('Target', 255);
            $table->dateTime('CreatedAt', 6);
            $table->dateTime('UpdatedAt', 6)->nullable();
            $table->unique(['Alias', 'EntityType'], 'IX_search_aliases_Alias_EntityType');
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('search_aliases');
    }
};
'@

Write-Text "database/migrations/2026_06_12_000008_create_kyc_requests_table.php" @'
<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('kyc_requests', function (Blueprint $table) {
            $table->char('Id', 36)->primary();
            $table->char('UserId', 36);
            $table->string('FrontIdImagePath', 500);
            $table->string('BackIdImagePath', 500);
            $table->string('SelfieImagePath', 500);
            $table->string('Status', 30);
            $table->string('RejectReason', 500)->nullable();
            $table->dateTime('CreatedAt', 6);
            $table->dateTime('ReviewedAt', 6)->nullable();
            $table->char('ReviewedByUserId', 36)->nullable();
            $table->index(['UserId', 'Status'], 'IX_kyc_requests_UserId_Status');
            $table->foreign('UserId')->references('Id')->on('users')->cascadeOnDelete();
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('kyc_requests');
    }
};
'@

Write-Text "database/migrations/2026_06_12_000009_create_withdrawal_requests_table.php" @'
<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('withdrawal_requests', function (Blueprint $table) {
            $table->char('Id', 36)->primary();
            $table->char('UserId', 36);
            $table->bigInteger('Amount');
            $table->string('BankName', 120);
            $table->string('BankAccountNumber', 80);
            $table->string('BankAccountName', 150);
            $table->string('Status', 30);
            $table->string('RejectReason', 500)->nullable();
            $table->dateTime('CreatedAt', 6);
            $table->dateTime('ReviewedAt', 6)->nullable();
            $table->char('ReviewedByUserId', 36)->nullable();
            $table->index(['UserId', 'Status', 'CreatedAt'], 'IX_withdrawal_requests_UserId_Status_CreatedAt');
            $table->foreign('UserId')->references('Id')->on('users')->cascadeOnDelete();
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('withdrawal_requests');
    }
};
'@

Write-Text "database/migrations/2026_06_12_000010_create_system_settings_table.php" @'
<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('system_settings', function (Blueprint $table) {
            $table->char('Id', 36)->primary();
            $table->string('Key', 100);
            $table->longText('Value');
            $table->string('Description', 500)->nullable();
            $table->dateTime('CreatedAt', 6);
            $table->dateTime('UpdatedAt', 6)->nullable();
            $table->unique('Key', 'IX_system_settings_Key');
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('system_settings');
    }
};
'@

Write-Text "database/migrations/2026_06_12_000011_create_sessions_table.php" @'
<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('sessions', function (Blueprint $table) {
            $table->string('id')->primary();
            $table->foreignId('user_id')->nullable()->index();
            $table->string('ip_address', 45)->nullable();
            $table->text('user_agent')->nullable();
            $table->longText('payload');
            $table->integer('last_activity')->index();
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('sessions');
    }
};
'@

Write-Text "database/seeders/DatabaseSeeder.php" @'
<?php

namespace Database\Seeders;

use App\Models\CardRate;
use App\Models\User;
use App\Models\Wallet;
use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\Hash;

class DatabaseSeeder extends Seeder
{
    public function run(): void
    {
        $admin = User::firstOrCreate(
            ['Email' => env('ADMIN_EMAIL', 'admin@example.com')],
            [
                'FullName' => 'Administrator',
                'PhoneNumber' => env('ADMIN_PHONE', '0900000000'),
                'IsPhoneVerified' => true,
                'IsEmailVerified' => true,
                'PasswordHash' => Hash::make(env('ADMIN_PASSWORD', 'AdminPass123')),
                'Status' => 'Active',
                'FailedLoginAttempts' => 0,
                'CreatedAt' => now('UTC'),
                'Role' => 'Admin',
                'CanManageUsers' => true,
                'CanManageTasks' => true,
                'CanApproveTasks' => true,
                'CanApproveKycWithdraw' => true,
                'CanTransferPoints' => true,
                'CanManageBlog' => true,
                'CanExportReports' => true,
                'IsDeleted' => false,
            ]
        );

        Wallet::firstOrCreate(
            ['UserId' => $admin->Id],
            ['Balance' => 50000000, 'LockedBalance' => 0, 'Currency' => 'XU', 'CreatedAt' => now('UTC')]
        );

        foreach (['VIETTEL', 'VINAPHONE', 'MOBIFONE'] as $provider) {
            foreach ([10000, 20000, 50000, 100000, 200000, 500000] as $faceValue) {
                CardRate::firstOrCreate(
                    ['Provider' => $provider, 'FaceValue' => $faceValue],
                    ['DiscountPercent' => 20, 'IsActive' => true, 'CreatedAt' => now('UTC')]
                );
            }
        }
    }
}
'@

Write-Text "routes/api.php" @'
<?php

use App\Http\Controllers\AdminController;
use App\Http\Controllers\AuthController;
use App\Http\Controllers\CardExchangeController;
use App\Http\Controllers\CardRateController;
use App\Http\Controllers\KycController;
use App\Http\Controllers\WalletController;
use App\Http\Controllers\WithdrawalController;
use Illuminate\Support\Facades\Route;

Route::get('health', fn () => response()->json(['status' => 'ok']));

Route::prefix('auth')->group(function (): void {
    Route::post('register', [AuthController::class, 'register']);
    Route::post('login', [AuthController::class, 'login']);
    Route::post('refresh-token', [AuthController::class, 'refresh']);
    Route::post('logout', [AuthController::class, 'logout']);
});

Route::get('card-rates', [CardRateController::class, 'index']);

Route::middleware('auth.bearer')->group(function (): void {
    Route::get('wallets/me/balance', [WalletController::class, 'balance']);
    Route::get('wallets/me/transactions', [WalletController::class, 'transactions']);
    Route::post('wallets/deposit', [WalletController::class, 'deposit']);
    Route::post('wallets/withdraw', [WalletController::class, 'withdraw']);

    Route::post('card-exchange/submit', [CardExchangeController::class, 'submit']);
    Route::get('card-exchange/my-transactions', [CardExchangeController::class, 'myTransactions']);
    Route::get('card-exchange/{id}/status', [CardExchangeController::class, 'status']);
    Route::get('card-exchange/{id}', [CardExchangeController::class, 'show']);

    Route::post('kyc', [KycController::class, 'store']);
    Route::get('kyc/me', [KycController::class, 'mine']);
    Route::post('withdrawals', [WithdrawalController::class, 'store']);
    Route::get('withdrawals/me', [WithdrawalController::class, 'mine']);

    Route::middleware('admin')->prefix('admin')->group(function (): void {
        Route::get('dashboard', [AdminController::class, 'dashboard']);
        Route::get('users', [AdminController::class, 'users']);
        Route::post('users', [AdminController::class, 'createUser']);
        Route::patch('users/{id}/status', [AdminController::class, 'updateUserStatus']);
        Route::post('points/transfer', [AdminController::class, 'transferPoints']);
        Route::get('transactions', [AdminController::class, 'transactions']);
        Route::post('card-rates', [CardRateController::class, 'upsert']);
        Route::get('kyc', [KycController::class, 'index']);
        Route::patch('kyc/{id}', [KycController::class, 'review']);
        Route::get('withdrawals', [WithdrawalController::class, 'index']);
        Route::patch('withdrawals/{id}', [WithdrawalController::class, 'review']);
    });
});
'@

Write-Text "routes/web.php" @'
<?php

use Illuminate\Support\Facades\Route;

Route::view('/', 'public.home')->name('home');
Route::view('/login', 'auth.login')->name('login');
Route::view('/register', 'auth.register')->name('register');
Route::view('/card-rates', 'public.card-rates')->name('card-rates');
Route::view('/guide', 'public.static')->defaults('title', 'Huong dan')->name('guide');
Route::view('/contact', 'public.static')->defaults('title', 'Lien he')->name('contact');
Route::view('/policy', 'public.static')->defaults('title', 'Chinh sach')->name('policy');

Route::view('/user', 'user.dashboard')->name('user.dashboard');
Route::view('/user/wallet', 'user.wallet')->name('user.wallet');
Route::view('/user/exchange', 'user.exchange')->name('user.exchange');
Route::view('/user/transactions', 'user.transactions')->name('user.transactions');
Route::view('/user/withdraw', 'user.withdraw')->name('user.withdraw');
Route::view('/user/kyc', 'user.kyc')->name('user.kyc');

Route::view('/admin', 'admin.dashboard')->name('admin.dashboard');
Route::view('/admin/users', 'admin.users')->name('admin.users');
Route::view('/admin/card-rates', 'admin.card-rates')->name('admin.card-rates');
Route::view('/admin/transactions', 'admin.transactions')->name('admin.transactions');
Route::view('/admin/withdrawals', 'admin.withdrawals')->name('admin.withdrawals');
Route::view('/admin/kyc', 'admin.kyc')->name('admin.kyc');
'@

Write-Text "resources/views/layouts/app.blade.php" @'
<!doctype html>
<html lang="vi">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>@yield('title', 'CardWallet')</title>
    <link rel="stylesheet" href="/assets/css/bootstrap.css">
    <link rel="stylesheet" href="/assets/css/font-awesome-all.css">
    <link rel="stylesheet" href="/assets/css/style.css">
    <link rel="stylesheet" href="/assets/css/responsive.css">
</head>
<body>
    <header class="main-header">
        <div class="container d-flex align-items-center justify-content-between py-3">
            <a href="/" class="h4 mb-0">CardWallet</a>
            <nav class="d-flex gap-3">
                <a href="/card-rates">Bang gia</a>
                <a href="/guide">Huong dan</a>
                <a href="/login">Dang nhap</a>
                <a href="/register">Dang ky</a>
            </nav>
        </div>
    </header>
    <main>@yield('content')</main>
    <script src="/assets/js/jquery.js"></script>
    <script src="/assets/js/bootstrap.min.js"></script>
    <script src="/assets/js/script.js"></script>
</body>
</html>
'@

Write-Text "resources/views/public/home.blade.php" @'
@extends('layouts.app')

@section('title', 'CardWallet')

@section('content')
<section class="banner-section">
    <div class="container py-5">
        <div class="row align-items-center">
            <div class="col-lg-7">
                <h1>Doi the cao thanh XU nhanh gon</h1>
                <p>Theo doi bang gia, gui the, quan ly vi va lich su giao dich tren mot he thong.</p>
                <a class="theme-btn btn-style-one" href="/user/exchange">Doi the ngay</a>
            </div>
            <div class="col-lg-5">
                <img src="/assets/images/banner/banner-img-1.jpg" alt="CardWallet" class="img-fluid">
            </div>
        </div>
    </div>
</section>
@endsection
'@

Write-Text "resources/views/public/card-rates.blade.php" @'
@extends('layouts.app')

@section('title', 'Bang gia the')

@section('content')
<div class="container py-5">
    <h1>Bang gia the</h1>
    <table class="table table-striped" id="rates-table">
        <thead><tr><th>Nha mang</th><th>Menh gia</th><th>Chiet khau</th></tr></thead>
        <tbody></tbody>
    </table>
</div>
<script>
fetch('/api/card-rates').then(r => r.json()).then(rows => {
  document.querySelector('#rates-table tbody').innerHTML = rows.map(x =>
    `<tr><td>${x.Provider}</td><td>${Number(x.FaceValue).toLocaleString()}</td><td>${x.DiscountPercent}%</td></tr>`
  ).join('');
});
</script>
@endsection
'@

Write-Text "resources/views/public/static.blade.php" @'
@extends('layouts.app')

@section('title', $title ?? 'CardWallet')

@section('content')
<div class="container py-5">
    <h1>{{ $title ?? 'CardWallet' }}</h1>
    <p>Noi dung trang tinh da duoc chuyen sang Laravel Blade. Ban co the thay the file nay bang noi dung chi tiet tu Razor Pages cu.</p>
</div>
@endsection
'@

Write-Text "resources/views/auth/login.blade.php" @'
@extends('layouts.app')

@section('title', 'Dang nhap')

@section('content')
<div class="container py-5" style="max-width: 520px">
    <h1>Dang nhap</h1>
    <form id="login-form" class="d-grid gap-3">
        <input class="form-control" name="login" placeholder="Email hoac so dien thoai" required>
        <input class="form-control" name="password" type="password" placeholder="Mat khau" required>
        <button class="theme-btn btn-style-one" type="submit">Dang nhap</button>
    </form>
</div>
<script>
document.getElementById('login-form').addEventListener('submit', async e => {
  e.preventDefault();
  const body = Object.fromEntries(new FormData(e.target).entries());
  const res = await fetch('/api/auth/login', {method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify(body)});
  const data = await res.json();
  if (data.accessToken) {
    localStorage.setItem('accessToken', data.accessToken);
    localStorage.setItem('refreshToken', data.refreshToken);
    location.href = '/user';
  } else alert(data.message || 'Dang nhap that bai');
});
</script>
@endsection
'@

Write-Text "resources/views/auth/register.blade.php" @'
@extends('layouts.app')

@section('title', 'Dang ky')

@section('content')
<div class="container py-5" style="max-width: 560px">
    <h1>Dang ky</h1>
    <form id="register-form" class="d-grid gap-3">
        <input class="form-control" name="fullName" placeholder="Ho ten" required>
        <input class="form-control" name="phoneNumber" placeholder="So dien thoai" required>
        <input class="form-control" name="email" type="email" placeholder="Email" required>
        <input class="form-control" name="password" type="password" placeholder="Mat khau" required>
        <button class="theme-btn btn-style-one" type="submit">Tao tai khoan</button>
    </form>
</div>
<script>
document.getElementById('register-form').addEventListener('submit', async e => {
  e.preventDefault();
  const body = Object.fromEntries(new FormData(e.target).entries());
  const res = await fetch('/api/auth/register', {method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify(body)});
  const data = await res.json();
  if (data.accessToken) {
    localStorage.setItem('accessToken', data.accessToken);
    localStorage.setItem('refreshToken', data.refreshToken);
    location.href = '/user';
  } else alert(data.message || 'Dang ky that bai');
});
</script>
@endsection
'@

Write-Text "resources/views/layouts/dashboard.blade.php" @'
<!doctype html>
<html lang="vi">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>@yield('title', 'CardWallet')</title>
    <link rel="stylesheet" href="/assets/css/bootstrap.css">
    <link rel="stylesheet" href="/assets/css/font-awesome-all.css">
    <link rel="stylesheet" href="/assets/css/style.css">
</head>
<body>
<div class="container-fluid">
    <div class="row min-vh-100">
        <aside class="col-md-3 col-lg-2 bg-light p-4">
            <h4>CardWallet</h4>
            <nav class="nav flex-column gap-2">
                <a href="/user">Tong quan</a>
                <a href="/user/exchange">Doi the</a>
                <a href="/user/wallet">Vi</a>
                <a href="/user/transactions">Giao dich</a>
                <a href="/user/withdraw">Rut tien</a>
                <a href="/user/kyc">KYC</a>
                <a href="/admin">Admin</a>
            </nav>
        </aside>
        <main class="col-md-9 col-lg-10 p-4">@yield('content')</main>
    </div>
</div>
@yield('scripts')
</body>
</html>
'@

Write-Text "resources/views/user/dashboard.blade.php" @'
@extends('layouts.dashboard')
@section('title', 'Tong quan')
@section('content')
<h1>Tong quan</h1>
<div class="alert alert-info" id="balance">Dang tai so du...</div>
@endsection
@section('scripts')
<script>
fetch('/api/wallets/me/balance', {headers:{Authorization:'Bearer '+localStorage.getItem('accessToken')}})
 .then(r=>r.json()).then(x=>document.getElementById('balance').textContent=`So du: ${x.balance ?? 0} ${x.currency ?? 'XU'}`);
</script>
@endsection
'@

Write-Text "resources/views/user/wallet.blade.php" @'
@extends('layouts.dashboard')
@section('title', 'Vi')
@section('content')
<h1>Vi cua toi</h1>
<pre id="wallet"></pre>
@endsection
@section('scripts')
<script>
fetch('/api/wallets/me/balance', {headers:{Authorization:'Bearer '+localStorage.getItem('accessToken')}})
 .then(r=>r.json()).then(x=>document.getElementById('wallet').textContent=JSON.stringify(x,null,2));
</script>
@endsection
'@

Write-Text "resources/views/user/exchange.blade.php" @'
@extends('layouts.dashboard')
@section('title', 'Doi the')
@section('content')
<h1>Doi the</h1>
<form id="exchange-form" class="d-grid gap-3" style="max-width: 560px">
    <input class="form-control" name="provider" placeholder="VIETTEL" required>
    <input class="form-control" name="faceValue" type="number" placeholder="Menh gia" required>
    <input class="form-control" name="serial" placeholder="Serial" required>
    <input class="form-control" name="cardCode" placeholder="Ma the" required>
    <button class="btn btn-primary">Gui the</button>
</form>
<pre id="result" class="mt-3"></pre>
@endsection
@section('scripts')
<script>
document.getElementById('exchange-form').addEventListener('submit', async e => {
 e.preventDefault();
 const body = Object.fromEntries(new FormData(e.target).entries());
 body.faceValue = Number(body.faceValue);
 const res = await fetch('/api/card-exchange/submit', {method:'POST', headers:{'Content-Type':'application/json', Authorization:'Bearer '+localStorage.getItem('accessToken')}, body:JSON.stringify(body)});
 document.getElementById('result').textContent = JSON.stringify(await res.json(), null, 2);
});
</script>
@endsection
'@

Write-Text "resources/views/user/transactions.blade.php" @'
@extends('layouts.dashboard')
@section('title', 'Giao dich')
@section('content')
<h1>Giao dich the</h1>
<pre id="rows"></pre>
@endsection
@section('scripts')
<script>
fetch('/api/card-exchange/my-transactions', {headers:{Authorization:'Bearer '+localStorage.getItem('accessToken')}})
 .then(r=>r.json()).then(x=>document.getElementById('rows').textContent=JSON.stringify(x,null,2));
</script>
@endsection
'@

Write-Text "resources/views/user/withdraw.blade.php" @'
@extends('layouts.dashboard')
@section('title', 'Rut tien')
@section('content')
<h1>Rut tien</h1>
<form id="withdraw-form" class="d-grid gap-3" style="max-width: 560px">
    <input class="form-control" name="amount" type="number" placeholder="So XU" required>
    <input class="form-control" name="bankName" placeholder="Ngan hang" required>
    <input class="form-control" name="bankAccountNumber" placeholder="So tai khoan" required>
    <input class="form-control" name="bankAccountName" placeholder="Chu tai khoan" required>
    <button class="btn btn-primary">Tao yeu cau</button>
</form>
<pre id="result" class="mt-3"></pre>
@endsection
@section('scripts')
<script>
document.getElementById('withdraw-form').addEventListener('submit', async e => {
 e.preventDefault();
 const body = Object.fromEntries(new FormData(e.target).entries());
 body.amount = Number(body.amount);
 const res = await fetch('/api/withdrawals', {method:'POST', headers:{'Content-Type':'application/json', Authorization:'Bearer '+localStorage.getItem('accessToken')}, body:JSON.stringify(body)});
 document.getElementById('result').textContent = JSON.stringify(await res.json(), null, 2);
});
</script>
@endsection
'@

Write-Text "resources/views/user/kyc.blade.php" @'
@extends('layouts.dashboard')
@section('title', 'KYC')
@section('content')
<h1>KYC</h1>
<p>Gui duong dan anh giay to va selfie de admin duyet.</p>
@endsection
'@

Write-Text "resources/views/admin/dashboard.blade.php" @'
@extends('layouts.dashboard')
@section('title', 'Admin')
@section('content')
<h1>Admin dashboard</h1>
<pre id="stats"></pre>
@endsection
@section('scripts')
<script>
fetch('/api/admin/dashboard', {headers:{Authorization:'Bearer '+localStorage.getItem('accessToken')}})
 .then(r=>r.json()).then(x=>document.getElementById('stats').textContent=JSON.stringify(x,null,2));
</script>
@endsection
'@

foreach ($view in @('users','card-rates','transactions','withdrawals','kyc')) {
    Write-Text "resources/views/admin/$view.blade.php" @"
@extends('layouts.dashboard')
@section('title', 'Admin $view')
@section('content')
<h1>Admin $view</h1>
<p>Trang quan tri da duoc tao trong Laravel. Ket noi API tu routes/api.php de hoan thien UI chi tiet.</p>
@endsection
"@
}

Write-Text "README.md" @'
# CardWallet Laravel

Day la ban Laravel port tu du an ASP.NET Core `card-wallet-platform`.

Da chuyen:
- Schema MySQL sang Laravel migrations cho users, wallets, transactions, card rates, card transactions, refresh tokens, search aliases, KYC, withdrawal requests, system settings.
- Eloquent models voi primary key `Id` kieu UUID de tuong thich database cu.
- API auth/register/login/refresh/logout, wallet balance/withdraw/history, card exchange, card rates, KYC, withdrawals, admin dashboard/users/points.
- Blade routes cho public, user dashboard va admin.
- Static assets tu `backend/CardWallet.Api/wwwroot/assets` sang `public/assets`.

Chay du an:

```powershell
cd D:\cardwallet-laravel
composer install
copy .env.example .env
php artisan key:generate
php artisan migrate --seed
php artisan serve
```

API dung bearer token tra ve tu `/api/auth/login`:

```http
Authorization: Bearer <accessToken>
```

Luu y:
- Worker xu ly the cao bat dong bo tu .NET da duoc giu schema/trang thai, nhung can cai dat command/queue neu muon goi parent card API that.
- UI Blade hien la ban port thuc dung de chay luong chinh; neu can pixel-perfect theo Razor cu, tiep tuc tach noi dung tung file `.cshtml` sang Blade.
'@

Copy-Dir (Join-Path $Source "backend\CardWallet.Api\wwwroot\assets") (Join-Path $resolvedTarget "public\assets")
Copy-Dir (Join-Path $Source "backend\CardWallet.Api\wwwroot\admin") (Join-Path $resolvedTarget "public\admin")

Write-Host "Laravel port files written to $resolvedTarget"
