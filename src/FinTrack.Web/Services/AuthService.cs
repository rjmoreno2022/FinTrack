using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FinTrack.Application.Auth.DTOs;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Infrastructure.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace FinTrack.Web.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IJSRuntime _jsRuntime;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly AppState _appState;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IJwtTokenService jwtTokenService,
        IJSRuntime jsRuntime,
        AuthenticationStateProvider authStateProvider,
        AppState appState,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenService = jwtTokenService;
        _jsRuntime = jsRuntime;
        _authStateProvider = authStateProvider;
        _appState = appState;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Asegurar que existan los roles Admin y User
        if (!await _roleManager.RoleExistsAsync("Admin"))
            await _roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));

        if (!await _roleManager.RoleExistsAsync("User"))
            await _roleManager.CreateAsync(new IdentityRole<Guid>("User"));

        // Regla: El primer usuario del sistema se convierte en Admin
        var isFirstUser = !await _userManager.Users.AnyAsync();

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            throw new ArgumentException("El correo electrónico ya está registrado");

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
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new ArgumentException($"Error al crear usuario: {errors}");
        }

        var rolesToAssign = isFirstUser ? new[] { "Admin", "User" } : new[] { "User" };
        await _userManager.AddToRolesAsync(user, rolesToAssign);

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email!, user.FirstName, user.LastName, rolesToAssign);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        var refreshDurationDays = int.TryParse(_configuration["Jwt:RefreshTokenDurationInDays"], out var days) ? days : 7;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshDurationDays);
        await _userManager.UpdateAsync(user);

        // Guardar token en localStorage si JS está disponible
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", accessToken);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", refreshToken);
        }
        catch (InvalidOperationException) { }

        if (_authStateProvider is CustomAuthenticationStateProvider customProvider)
        {
            customProvider.MarkUserAsAuthenticated(accessToken);
        }

        _appState.SetDisplayCurrency(user.PreferredCurrency.ToString());

        var durationMinutes = int.TryParse(_configuration["Jwt:DurationInMinutes"], out var minutes) ? minutes : 60;
        return new AuthResponse
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
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Credenciales inválidas");

        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValidPassword)
            throw new UnauthorizedAccessException("Credenciales inválidas");

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email!, user.FirstName, user.LastName, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        var refreshDurationDays = int.TryParse(_configuration["Jwt:RefreshTokenDurationInDays"], out var days) ? days : 7;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshDurationDays);
        await _userManager.UpdateAsync(user);

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", accessToken);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", refreshToken);
        }
        catch (InvalidOperationException) { }

        if (_authStateProvider is CustomAuthenticationStateProvider customProvider)
        {
            customProvider.MarkUserAsAuthenticated(accessToken);
        }

        _appState.SetDisplayCurrency(user.PreferredCurrency.ToString());

        var durationMinutes = int.TryParse(_configuration["Jwt:DurationInMinutes"], out var minutes) ? minutes : 60;
        return new AuthResponse
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
        };
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
        }
        catch (InvalidOperationException) { }

        if (_authStateProvider is CustomAuthenticationStateProvider customProvider)
        {
            customProvider.MarkUserAsLoggedOut();
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var userClaims = authState.User;

        if (userClaims.Identity == null || !userClaims.Identity.IsAuthenticated)
            return null;

        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? userClaims.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return null;

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PreferredCurrency = user.PreferredCurrency,
            Roles = roles.ToList()
        };
    }

    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var accessToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "authToken");
            var refreshToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "refreshToken");

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
                return false;

            var principal = _jwtTokenService.GetPrincipalFromExpiredToken(accessToken);
            if (principal == null)
                return false;

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? principal.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return false;

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return false;

            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email!, user.FirstName, user.LastName, roles);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            var refreshDurationDays = int.TryParse(_configuration["Jwt:RefreshTokenDurationInDays"], out var days) ? days : 7;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshDurationDays);
            await _userManager.UpdateAsync(user);

            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", newAccessToken);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", newRefreshToken);

            if (_authStateProvider is CustomAuthenticationStateProvider customProvider)
            {
                customProvider.MarkUserAsAuthenticated(newAccessToken);
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
