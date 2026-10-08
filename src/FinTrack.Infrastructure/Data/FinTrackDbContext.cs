using System;
using System.Collections.Generic;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Data;

public class FinTrackDbContext : DbContext
{
    public FinTrackDbContext(DbContextOptions<FinTrackDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Debt> Debts => Set<Debt>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<DebtPayment> DebtPayments => Set<DebtPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignorar eventos de dominio para que no se mapeen como entidades de base de datos
        modelBuilder.Ignore<DomainEvent>();

        // Aplicar todas las configuraciones IEntityTypeConfiguration de este ensamblado
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinTrackDbContext).Assembly);

        // Sembrado inicial de categorías predeterminadas del sistema
        modelBuilder.Entity<Category>().HasData(GetSeedCategories());
    }

    private static IEnumerable<Category> GetSeedCategories()
    {
        // Categorías de Ingreso
        yield return new Category(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Salario", CategoryType.Income, icon: "attach_money", color: "#238636", isSystem: true);

        yield return new Category(
            Guid.Parse("11111111-1111-1111-1111-111111111112"),
            "Freelance", CategoryType.Income, icon: "laptop", color: "#58a6ff", isSystem: true);

        yield return new Category(
            Guid.Parse("11111111-1111-1111-1111-111111111113"),
            "Regalo", CategoryType.Income, icon: "card_giftcard", color: "#bc8cff", isSystem: true);

        yield return new Category(
            Guid.Parse("11111111-1111-1111-1111-111111111114"),
            "Inversiones", CategoryType.Income, icon: "trending_up", color: "#d29922", isSystem: true);

        // Categorías de Gasto
        yield return new Category(
            Guid.Parse("22222222-2222-2222-2222-222222222221"),
            "Alimentación", CategoryType.Expense, icon: "restaurant", color: "#da3633", isSystem: true);

        yield return new Category(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Transporte", CategoryType.Expense, icon: "directions_car", color: "#d29922", isSystem: true);

        yield return new Category(
            Guid.Parse("22222222-2222-2222-2222-222222222223"),
            "Vivienda", CategoryType.Expense, icon: "home", color: "#58a6ff", isSystem: true);

        yield return new Category(
            Guid.Parse("22222222-2222-2222-2222-222222222224"),
            "Servicios", CategoryType.Expense, icon: "bolt", color: "#d29922", isSystem: true);

        yield return new Category(
            Guid.Parse("22222222-2222-2222-2222-222222222225"),
            "Salud", CategoryType.Expense, icon: "local_hospital", color: "#da3633", isSystem: true);

        yield return new Category(
            Guid.Parse("22222222-2222-2222-2222-222222222226"),
            "Entretenimiento", CategoryType.Expense, icon: "movie", color: "#bc8cff", isSystem: true);

        yield return new Category(
            Guid.Parse("22222222-2222-2222-2222-222222222227"),
            "Suscripciones", CategoryType.Expense, icon: "subscriptions", color: "#8b949e", isSystem: true);

        yield return new Category(
            Guid.Parse("22222222-2222-2222-2222-222222222228"),
            "Compras", CategoryType.Expense, icon: "shopping_cart", color: "#58a6ff", isSystem: true);

        yield return new Category(
            Guid.Parse("22222222-2222-2222-2222-222222222229"),
            "Educación", CategoryType.Expense, icon: "school", color: "#238636", isSystem: true);

        yield return new Category(
            Guid.Parse("22222222-2222-2222-2222-22222222222a"),
            "Otros", CategoryType.Expense, icon: "more_horiz", color: "#6e7681", isSystem: true);
    }
}
