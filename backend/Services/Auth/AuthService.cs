using AutoMapper;
using LearnPath.API.Authentication.Jwt;
using LearnPath.API.Common;
using LearnPath.API.DTOs.Auth;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LearnPath.API.Data;

namespace LearnPath.API.Services.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<Entities.User> _userManager;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public AuthService(
        UserManager<Entities.User> userManager,
        JwtTokenGenerator jwtTokenGenerator,
        IMapper mapper,
        ApplicationDbContext context,
        IAuditLogService auditLog)
    {
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _mapper = mapper;
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser is not null)
            throw new ArgumentException("Email is already registered.");

        var user = _mapper.Map<Entities.User>(dto);
        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new ArgumentException(string.Join(" | ", errors));
        }

        await _userManager.AddToRoleAsync(user, "Student");

        await _auditLog.LogAsync(
            AuditAction.USER_CREATED,
            "User",
            user.Id,
            $"User '{user.Email}' registered an account.",
            userId: user.Id,
            username: user.Email,
            role: "Student");

        return await BuildAuthResponseAsync(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            await LogLoginFailedAsync(null, dto.Email, "Unknown email address.");
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var isValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!isValid)
        {
            await LogLoginFailedAsync(user.Id, dto.Email, "Incorrect password.");
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (user.Status == UserStatus.Deleted)
        {
            await LogLoginFailedAsync(user.Id, dto.Email, "Account has been deleted.");
            throw new UnauthorizedAccessException("Account has been deleted.");
        }

        if (user.Status == UserStatus.Inactive)
        {
            await LogLoginFailedAsync(user.Id, dto.Email, "Account is deactivated.");
            throw new UnauthorizedAccessException("Account is deactivated.");
        }

        if (user.Status == UserStatus.Invalid)
        {
            await LogLoginFailedAsync(user.Id, dto.Email, "Account is marked invalid.");
            throw new UnauthorizedAccessException("Account is marked invalid.");
        }

        var roles = await _userManager.GetRolesAsync(user);

        await _auditLog.LogAsync(
            AuditAction.LOGIN,
            "User",
            user.Id,
            $"User '{user.Email}' logged in.",
            userId: user.Id,
            username: user.Email,
            role: roles.FirstOrDefault());

        return await BuildAuthResponseAsync(user);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        var stored = await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r =>
                r.Token == dto.RefreshToken &&
                !r.IsRevoked &&
                r.ExpiresAt > DateTime.UtcNow)
            ?? throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        stored.IsRevoked = true;
        await _context.SaveChangesAsync();

        if (stored.User.Status is UserStatus.Deleted or UserStatus.Inactive or UserStatus.Invalid)
            throw new UnauthorizedAccessException("Account is not active.");

        return await BuildAuthResponseAsync(stored.User);
    }

    public async Task RevokeTokenAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        var tokens = await _context.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
            token.IsRevoked = true;

        await _context.SaveChangesAsync();

        if (user is not null)
        {
            var roles = await _userManager.GetRolesAsync(user);
            await _auditLog.LogAsync(
                AuditAction.LOGOUT,
                "User",
                user.Id,
                $"User '{user.Email}' logged out.",
                userId: user.Id,
                username: user.Email,
                role: roles.FirstOrDefault());
        }
    }

    private Task LogLoginFailedAsync(string? userId, string email, string reason) =>
        _auditLog.LogAsync(
            AuditAction.LOGIN_FAILED,
            "User",
            userId ?? "unknown",
            $"Failed login attempt for '{email}'. Reason: {reason}",
            userId: userId,
            username: email,
            role: null);

    private async Task<AuthResponseDto> BuildAuthResponseAsync(Entities.User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, roles);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        var settings = new JwtSettings();
        var tokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        await _context.RefreshTokens.AddAsync(tokenEntity);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles,
        };
    }
}