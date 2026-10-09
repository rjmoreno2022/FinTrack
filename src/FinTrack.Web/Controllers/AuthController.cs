using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FinTrack.Application.Auth.DTOs;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FinTrack.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!await _roleManager.RoleExistsAsync("Admin"))
            await _roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));

        if (!await _roleManager.RoleExistsAsync("User"))
            await _roleManager.CreateAsync(new IdentityRole<Guid>("User"));

        var isFirstUser = !await _userManager.Users.AnyAsync();

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return BadRequest(new { error = "El correo electrónico ya está registrado" });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PreferredCurrency = request.PreferredCurrency,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { error = string.Join("; ", result.Errors.Select(e => e.Description)) });

        var rolesToAssign = isFirstUser ? new[] { "Admin", "User" } : new[] { "User" };
        await _userManager.AddToRolesAsync(user, rolesToAssign);

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email!, user.FirstName, user.LastName, rolesToAssign);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        var refreshDurationDays = int.TryParse(_configuration["Jwt:RefreshTokenDurationInDays"], out var days) ? days : 7;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshDurationDays);
        await _userManager.UpdateAsync(user);

        var durationMinutes = int.TryParse(_configuration["Jwt:DurationInMinutes"], out var minutes) ? minutes : 60;
        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(durationMinutes),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PreferredCurrency = user.PreferredCurrency,
                Roles = rolesToAssign.ToList()
            }
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
            return Unauthorized(new { error = "Credenciales inválidas" });

        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValidPassword)
            return Unauthorized(new { error = "Credenciales inválidas" });

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email!, user.FirstName, user.LastName, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        var refreshDurationDays = int.TryParse(_configuration["Jwt:RefreshTokenDurationInDays"], out var days) ? days : 7;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshDurationDays);
        await _userManager.UpdateAsync(user);

        var durationMinutes = int.TryParse(_configuration["Jwt:DurationInMinutes"], out var minutes) ? minutes : 60;
        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(durationMinutes),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PreferredCurrency = user.PreferredCurrency,
                Roles = roles.ToList()
            }
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        ClaimsPrincipal? principal;
        try
        {
            principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        }
        catch (Exception)
        {
            return BadRequest(new { error = "Token de acceso inválido" });
        }

        if (principal == null)
            return BadRequest(new { error = "Token de acceso inválido" });

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? principal.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return BadRequest(new { error = "Token no contiene identificador de usuario válido" });

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return Unauthorized(new { error = "Refresh Token inválido o expirado" });

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email!, user.FirstName, user.LastName, roles);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        var refreshDurationDays = int.TryParse(_configuration["Jwt:RefreshTokenDurationInDays"], out var days) ? days : 7;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshDurationDays);
        await _userManager.UpdateAsync(user);

        var durationMinutes = int.TryParse(_configuration["Jwt:DurationInMinutes"], out var minutes) ? minutes : 60;
        return Ok(new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(durationMinutes),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PreferredCurrency = user.PreferredCurrency,
                Roles = roles.ToList()
            }
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return NotFound(new { error = "Usuario no encontrado" });

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PreferredCurrency = user.PreferredCurrency,
            Roles = roles.ToList()
        });
    }
}
