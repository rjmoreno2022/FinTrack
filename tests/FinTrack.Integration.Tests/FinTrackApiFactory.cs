using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Data;
using FinTrack.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Integration.Tests;

public class FinTrackApiFactory : WebApplicationFactory<Program>
{
    public const string TestConnectionString =
        "Server=.\\SQLEXPRESS;Database=FinTrack_IntegrationTests;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<FinTrackDbContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            services.AddDbContext<FinTrackDbContext>(options =>
            {
                options.UseSqlServer(TestConnectionString);
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email = "testuser@fintrack.com",
        string password = "Password123!")
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FirstName = "Test",
                LastName = "User",
                PreferredCurrency = Currency.USD,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo crear usuario de prueba: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        var token = tokenService.GenerateAccessToken(user.Id, user.Email!, user.FirstName, user.LastName, Array.Empty<string>());

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();

        await db.DebtPayments.IgnoreQueryFilters().ExecuteDeleteAsync();
        await db.Debts.IgnoreQueryFilters().ExecuteDeleteAsync();
        await db.Goals.IgnoreQueryFilters().ExecuteDeleteAsync();
        await db.Budgets.IgnoreQueryFilters().ExecuteDeleteAsync();
        await db.Transactions.IgnoreQueryFilters().ExecuteDeleteAsync();
        await db.Accounts.IgnoreQueryFilters().ExecuteDeleteAsync();
    }
}
