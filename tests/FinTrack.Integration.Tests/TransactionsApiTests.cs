using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FinTrack.Application.Accounts.DTOs;
using FinTrack.Application.Common.Models;
using FinTrack.Application.Transactions.DTOs;
using FluentAssertions;
using Xunit;

namespace FinTrack.Integration.Tests;

[Collection("IntegrationTests")]
public class TransactionsApiTests : IAsyncLifetime
{
    private readonly FinTrackApiFactory _factory;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TransactionsApiTests(FinTrackApiFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _factory.ResetDatabaseAsync();

    [Fact]
    public async Task CreateIncomeAndExpenseTransactions_ShouldUpdateAccountBalanceCorrectly()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();

        // 1. Crear una cuenta bancaria inicial
        var accountRequest = new CreateAccountRequest
        {
            Name = "Cuenta Corriente USD",
            Type = "Bank",
            Currency = "USD",
            Description = "Cuenta para pruebas de transacciones"
        };
        var accountResponse = await client.PostAsJsonAsync("/api/accounts", accountRequest);
        accountResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdAccount = await accountResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var accountId = createdAccount.GetProperty("id").GetGuid();

        // 2. Registrar un Ingreso de $1,000 USD
        var incomeRequest = new CreateTransactionRequest
        {
            AccountId = accountId,
            Type = "Income",
            Amount = 1000m,
            Currency = "USD",
            Description = "Salario Mensual",
            Date = DateTime.UtcNow
        };
        var incomeResponse = await client.PostAsJsonAsync("/api/transactions", incomeRequest);
        var content = await incomeResponse.Content.ReadAsStringAsync();
        incomeResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: $"Received: {content}");

        // 3. Verificar que el balance de la cuenta subió a 1000
        var accountAfterIncome = await client.GetFromJsonAsync<AccountDto>($"/api/accounts/{accountId}", _jsonOptions);
        accountAfterIncome.Should().NotBeNull();
        accountAfterIncome!.Balance.Should().Be(1000m);

        // 4. Registrar un Gasto de $350 USD
        var expenseRequest = new CreateTransactionRequest
        {
            AccountId = accountId,
            Type = "Expense",
            Amount = 350m,
            Currency = "USD",
            Description = "Mercado Semanal",
            Date = DateTime.UtcNow
        };
        var expenseResponse = await client.PostAsJsonAsync("/api/transactions", expenseRequest);
        expenseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Verificar que el balance se actualizó a 650
        var accountAfterExpense = await client.GetFromJsonAsync<AccountDto>($"/api/accounts/{accountId}", _jsonOptions);
        accountAfterExpense.Should().NotBeNull();
        accountAfterExpense!.Balance.Should().Be(650m);

        // 6. Consultar el listado de transacciones para esa cuenta
        var transactionsResponse = await client.GetFromJsonAsync<PagedList<TransactionDto>>(
            $"/api/transactions?accountId={accountId}", _jsonOptions);
        transactionsResponse.Should().NotBeNull();
        transactionsResponse!.Items.Should().HaveCount(2);
        transactionsResponse.Items.Should().Contain(t => t.Description == "Salario Mensual" && t.Amount == 1000m);
        transactionsResponse.Items.Should().Contain(t => t.Description == "Mercado Semanal" && t.Amount == 350m);
    }
}
