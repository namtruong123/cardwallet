namespace CardWallet.Application.DTOs.Auth;

public class LoginRequest
{
    public string Login { get; set; } = string.Empty; // phone hoặc email
    public string Password { get; set; } = string.Empty;
}
