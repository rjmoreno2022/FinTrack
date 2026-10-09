using System;
using System.Security.Claims;
using FinTrack.Application.Common.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Web.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    private ClaimsPrincipal? GetUser()
    {
        var httpUser = _httpContextAccessor.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated == true)
            return httpUser;

        // Fallback para Blazor Interactive Server cuando no hay HttpContext activo
        var authProvider = _serviceProvider.GetService<AuthenticationStateProvider>() as CustomAuthenticationStateProvider;
        return authProvider?.CurrentUser;
    }

    public Guid? UserId
    {
        get
        {
            var user = GetUser();
            var claim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? user?.FindFirst("sub")?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? Email => GetUser()?.FindFirst(ClaimTypes.Email)?.Value ?? GetUser()?.FindFirst("email")?.Value;

    public bool IsAuthenticated => GetUser()?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => GetUser()?.IsInRole(role) ?? false;
}
