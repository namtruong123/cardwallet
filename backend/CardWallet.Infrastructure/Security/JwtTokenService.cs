using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CardWallet.Application.Interfaces;
using CardWallet.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CardWallet.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user)
    {
        var jwt = _configuration.GetSection("Jwt");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("phone", user.PhoneNumber),
            new("fullName", user.FullName),
            new("role", user.Role),
            new("canManageUsers", user.CanManageUsers.ToString()),
            new("canManageTasks", user.CanManageTasks.ToString()),
            new("canApproveTasks", user.CanApproveTasks.ToString()),
            new("canApproveKycWithdraw", user.CanApproveKycWithdraw.ToString()),
            new("canTransferPoints", user.CanTransferPoints.ToString()),
            new("canManageBlog", user.CanManageBlog.ToString()),
            new("canExportReports", user.CanExportReports.ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwt["Key"]!)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var expires = DateTime.UtcNow.AddMinutes(
            int.Parse(jwt["ExpireMinutes"] ?? "60")
        );

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
