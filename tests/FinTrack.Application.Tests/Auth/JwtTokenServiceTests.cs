using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using FinTrack.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FinTrack.Application.Tests.Auth;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service;

    public JwtTokenServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "SuperSecretKeyForFinTrackUnitTestsMustBeLongEnough1234567890!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:DurationInMinutes"] = "30",
                ["Jwt:RefreshTokenDurationInDays"] = "7"
            })
            .Build();

        _service = new JwtTokenService(config);
    }

    [Fact]
    public void GenerateAccessToken_ShouldProduceValidTokenWithCorrectClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "admin@fintrack.test";
        var firstName = "Carlos";
        var lastName = "Perez";
        var roles = new[] { "Admin", "User" };

        // Act
        var token = _service.GenerateAccessToken(userId, email, firstName, lastName, roles);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();

        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Issuer.Should().Be("TestIssuer");
        jwtToken.Audiences.Should().Contain("TestAudience");

        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        subClaim.Should().NotBeNull();
        subClaim!.Value.Should().Be(userId.ToString());

        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be(email);

        var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        nameClaim.Should().NotBeNull();
        nameClaim!.Value.Should().Be("Carlos Perez");

        var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        roleClaims.Should().Contain(new[] { "Admin", "User" });
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyBase64String()
    {
        // Act
        var token1 = _service.GenerateRefreshToken();
        var token2 = _service.GenerateRefreshToken();

        // Assert
        token1.Should().NotBeNullOrWhiteSpace();
        token2.Should().NotBeNullOrWhiteSpace();
        token1.Should().NotBe(token2);

        var bytes = Convert.FromBase64String(token1);
        bytes.Length.Should().Be(64);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ShouldExtractClaimsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "user@fintrack.test";
        var token = _service.GenerateAccessToken(userId, email, "Ana", "Gomez", new[] { "User" });

        // Act
        var principal = _service.GetPrincipalFromExpiredToken(token);

        // Assert
        principal.Should().NotBeNull();
        var sub = principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        sub.Should().Be(userId.ToString());
    }
}
