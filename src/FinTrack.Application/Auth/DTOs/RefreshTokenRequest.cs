using System.ComponentModel.DataAnnotations;

namespace FinTrack.Application.Auth.DTOs;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "El Access Token es requerido")]
    public string AccessToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "El Refresh Token es requerido")]
    public string RefreshToken { get; set; } = string.Empty;
}
