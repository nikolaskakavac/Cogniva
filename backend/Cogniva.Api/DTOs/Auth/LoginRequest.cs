using System.ComponentModel.DataAnnotations;

namespace Cogniva.Api.DTOs.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress, MaxLength(320)] string Email,
    [Required, MaxLength(128)] string Password);
