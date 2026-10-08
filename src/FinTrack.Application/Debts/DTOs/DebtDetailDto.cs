using System;
using System.Collections.Generic;

namespace FinTrack.Application.Debts.DTOs;

public class DebtDetailDto : DebtDto
{
    public List<DebtPaymentDto> Payments { get; set; } = new();
}
