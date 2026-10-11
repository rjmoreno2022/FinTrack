using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FinTrack.Application.Accounts.DTOs;
using FinTrack.Application.Common.Models;
using FluentAssertions;
using Xunit;

namespace FinTrack.Integration.Tests;

[Collection("IntegrationTests")]
public class AccountsApiTests : IAsyncLifetime
{
    private readonly FinTrackApiFactory _factory;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AccountsApiTests(FinTrackApiFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _factory.ResetDatabaseAsync();

    [Fact]
    public async Task CreateAccount_WithValidData_ShouldReturnCreatedAndPersistInDatabase()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();
        var request = new CreateAccountRequest
        {
            Name = "Banesco USD",
            Type = "Bank",
            Currency = "USD",
            Description = "Cuenta principal de ahorros"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/accounts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var accountId = created.GetProperty("id").GetGuid();

        // Verify retrieval via GET /api/accounts/{id}
        var getResponse = await client.GetAsync($"/api/accounts/{accountId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var account = await getResponse.Content.ReadFromJsonAsync<AccountDto>(_jsonOptions);
        account.Should().NotBeNull();
        account!.Name.Should().Be("Banesco USD");
        account.Currency.Should().Be("USD");
        account.Type.Should().Be("Bank");
        account.Balance.Should().Be(0);
    }

    [Fact]
    public async Task GetAllAccounts_ShouldReturnUserAccounts()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();
        var request = new CreateAccountRequest
        {
            Name = "Efectivo Cartera",
            Type = "Cash",
            Currency = "VES"
        };
        await client.PostAsJsonAsync("/api/accounts", request);

        // Act
        var response = await client.GetAsync("/api/accounts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var accounts = await response.Content.ReadFromJsonAsync<PagedList<AccountDto>>(_jsonOptions);
        accounts.Should().NotBeNull();
        accounts!.Items.Should().Contain(a => a.Name == "Efectivo Cartera");
    }

    [Fact]
    public async Task CreateAccount_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var request = new CreateAccountRequest
        {
            Name = "Prueba Sin Auth",
            Type = "Bank",
            Currency = "USD"
        };

        // Act
        var response = await unauthenticatedClient.PostAsJsonAsync("/api/accounts", request);

        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, because: $"Received: {content}");
    }

    [Fact]
    public async Task CreateAccount_WithInitialBalance_ShouldPersistInitialBalanceAndBalance()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();
        var request = new CreateAccountRequest
        {
            Name = "Ahorros Apertura USD",
            Type = "Bank",
            Currency = "USD",
            InitialBalance = 750m
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/accounts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var accountId = created.GetProperty("id").GetGuid();

        var getResponse = await client.GetAsync($"/api/accounts/{accountId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var account = await getResponse.Content.ReadFromJsonAsync<AccountDto>(_jsonOptions);
        account.Should().NotBeNull();
        account!.Balance.Should().Be(750m);
        account.InitialBalance.Should().Be(750m);
    }

    [Fact]
    public async Task UpdateAccount_WithNewBalance_ShouldModifyAccountBalance()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/accounts", new CreateAccountRequest
        {
            Name = "Cuenta Para Ajustar",
            Type = "Cash",
            Currency = "USD",
            InitialBalance = 100m
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var accountId = created.GetProperty("id").GetGuid();

        // Act: Modificar saldo a 350
        var updateResponse = await client.PutAsJsonAsync($"/api/accounts/{accountId}", new UpdateAccountRequest
        {
            Name = "Cuenta Para Ajustar",
            Balance = 350m
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert
        var account = await client.GetFromJsonAsync<AccountDto>($"/api/accounts/{accountId}", _jsonOptions);
        account.Should().NotBeNull();
        account!.Balance.Should().Be(350m);
    }

    [Fact]
    public async Task TransferFunds_BetweenAccounts_ShouldUpdateBothBalances()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resSource = await client.PostAsJsonAsync("/api/accounts", new CreateAccountRequest
        {
            Name = "Cuenta Origen API",
            Type = "Bank",
            Currency = "USD",
            InitialBalance = 1000m
        });
        var srcId = (await resSource.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions)).GetProperty("id").GetGuid();

        var resDest = await client.PostAsJsonAsync("/api/accounts", new CreateAccountRequest
        {
            Name = "Cuenta Destino API",
            Type = "Cash",
            Currency = "USD",
            InitialBalance = 50m
        });
        var destId = (await resDest.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions)).GetProperty("id").GetGuid();

        // Act: Transferir 300 USD
        var transferResponse = await client.PostAsJsonAsync("/api/accounts/transfer", new TransferFundsRequest
        {
            SourceAccountId = srcId,
            DestinationAccountId = destId,
            Amount = 300m,
            Description = "Transferencia API Test"
        });
        transferResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: verificar balances
        var srcAcc = await client.GetFromJsonAsync<AccountDto>($"/api/accounts/{srcId}", _jsonOptions);
        var destAcc = await client.GetFromJsonAsync<AccountDto>($"/api/accounts/{destId}", _jsonOptions);

        srcAcc.Should().NotBeNull();
        srcAcc!.Balance.Should().Be(700m);

        destAcc.Should().NotBeNull();
        destAcc!.Balance.Should().Be(350m);
    }
}
