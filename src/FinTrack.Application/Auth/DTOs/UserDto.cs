using System;
using System.Collections.Generic;
using FinTrack.Domain.Enums;

namespace FinTrack.Application.Auth.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Currency PreferredCurrency { get; set; } = Currency.USD;
    public IList<string> Roles { get; set; } = new List<string>();
}
