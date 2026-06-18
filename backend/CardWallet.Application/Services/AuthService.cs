using CardWallet.Application.DTOs.Auth;
using CardWallet.Application.Exceptions;
using CardWallet.Application.Interfaces;
using CardWallet.Domain.Entities;

namespace CardWallet.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLower();
        var phone = request.PhoneNumber.Trim();

        if (await _userRepository.ExistsByEmailAsync(email))
            throw new ConflictException("Email đã tồn tại.");

        if (await _userRepository.ExistsByPhoneAsync(phone))
            throw new ConflictException("Số điện thoại đã tồn tại.");

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PhoneNumber = phone,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Status = "Active",
            Role = "Customer",
            Wallet = new Wallet
            {
                Balance = 0,
                LockedBalance = 0,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var refreshToken = _refreshTokenService.Create(user.Id);
        await _refreshTokenRepository.AddAsync(refreshToken);
        await _refreshTokenRepository.SaveChangesAsync();

        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            AccessToken = _jwtTokenService.GenerateAccessToken(user),
            RefreshToken = refreshToken.Token
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var login = request.Login.Trim();

        var user = await _userRepository.GetByEmailOrPhoneAsync(login);

        if (user == null)
            throw new UnauthorizedException("Tài khoản hoặc mật khẩu không đúng.");

        if (user.Status != "Active")
            throw new UnauthorizedException("Tài khoản không hoạt động.");

        if (user.LockoutEndAt.HasValue && user.LockoutEndAt.Value > DateTime.UtcNow)
            throw new UnauthorizedException("Tài khoản đang bị khóa do nhập sai mật khẩu nhiều lần. Vui lòng thử lại sau.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEndAt = DateTime.UtcNow.AddMinutes(15);
            }
            await _userRepository.SaveChangesAsync();
            throw new UnauthorizedException("Tài khoản hoặc mật khẩu không đúng.");
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEndAt = null;
        await _userRepository.SaveChangesAsync();

        var refreshToken = _refreshTokenService.Create(user.Id);
        await _refreshTokenRepository.AddAsync(refreshToken);
        await _refreshTokenRepository.SaveChangesAsync();

        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            AccessToken = _jwtTokenService.GenerateAccessToken(user),
            RefreshToken = refreshToken.Token
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var oldToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

        if (oldToken == null || !oldToken.IsActive)
            throw new UnauthorizedException("Refresh token không hợp lệ.");

        var user = oldToken.User;

        if (user.Status != "Active")
            throw new UnauthorizedException("Tài khoản không hoạt động.");

        var newRefreshToken = _refreshTokenService.Create(user.Id);

        oldToken.IsRevoked = true;
        oldToken.RevokedAt = DateTime.UtcNow;
        oldToken.ReplacedByToken = newRefreshToken.Token;

        await _refreshTokenRepository.AddAsync(newRefreshToken);
        await _refreshTokenRepository.SaveChangesAsync();

        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            AccessToken = _jwtTokenService.GenerateAccessToken(user),
            RefreshToken = newRefreshToken.Token
        };
    }

    public async Task LogoutAsync(LogoutRequest request)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

        if (refreshToken == null)
            return;

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;

        await _refreshTokenRepository.SaveChangesAsync();
    }
}
