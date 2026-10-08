using AutoMapper;
using FinTrack.Application.Accounts.DTOs;
using FinTrack.Application.Categories.DTOs;
using FinTrack.Application.Debts.DTOs;
using FinTrack.Application.Goals.DTOs;
using FinTrack.Application.Transactions.DTOs;
using FinTrack.Domain.Entities;

namespace FinTrack.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Account mappings
        CreateMap<Account, AccountDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency.ToString()));

        // Transaction mappings
        CreateMap<Transaction, TransactionDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency.ToString()))
            .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.Account != null ? src.Account.Name : null))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null));

        // Category mappings
        CreateMap<Category, CategoryDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));

        // Debt mappings
        CreateMap<Debt, DebtDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency.ToString()))
            .ForMember(dest => dest.ProgressPercentage, opt => opt.MapFrom(src => src.ProgressPercentage));

        // Goal mappings
        CreateMap<Goal, GoalDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority.ToString()))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency.ToString()))
            .ForMember(dest => dest.ProgressPercentage, opt => opt.MapFrom(src => src.ProgressPercentage))
            .ForMember(dest => dest.RemainingAmount, opt => opt.MapFrom(src => src.RemainingAmount));

        // Budget mappings
        CreateMap<Budget, FinTrack.Application.Budgets.DTOs.BudgetDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
            .ForMember(dest => dest.Period, opt => opt.MapFrom(src => src.Period.ToString()))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency.ToString()))
            .ForMember(dest => dest.Remaining, opt => opt.MapFrom(src => src.Remaining))
            .ForMember(dest => dest.UsagePercentage, opt => opt.MapFrom(src => src.UsagePercentage))
            .ForMember(dest => dest.IsOverBudget, opt => opt.MapFrom(src => src.IsOverBudget));

        // Debt payment and detail mappings
        CreateMap<DebtPayment, DebtPaymentDto>();

        CreateMap<Debt, DebtDetailDto>()
            .IncludeBase<Debt, DebtDto>()
            .ForMember(dest => dest.Payments, opt => opt.MapFrom(src => src.Payments));

        // ExchangeRate mappings
        CreateMap<ExchangeRate, FinTrack.Application.ExchangeRates.DTOs.ExchangeRateDto>()
            .ForMember(dest => dest.FromCurrency, opt => opt.MapFrom(src => src.FromCurrency.ToString()))
            .ForMember(dest => dest.ToCurrency, opt => opt.MapFrom(src => src.ToCurrency.ToString()))
            .ForMember(dest => dest.Source, opt => opt.MapFrom(src => src.Source.ToString()));
    }
}
