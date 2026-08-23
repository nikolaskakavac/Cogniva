using System.ComponentModel.DataAnnotations;

namespace Cogniva.Api.DTOs.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(320)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password,
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName);
