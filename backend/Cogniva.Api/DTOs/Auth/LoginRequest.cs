using System.ComponentModel.DataAnnotations;

namespace Cogniva.Api.DTOs.Auth;

public sealed record LoginRequest(
    [Required(ErrorMessage = "Email adresa je obavezna.")]
    [EmailAddress(ErrorMessage = "Unesite ispravnu email adresu.")]
    [MaxLength(320, ErrorMessage = "Email adresa može imati najviše 320 karaktera.")]
    string Email,
    [Required(ErrorMessage = "Lozinka je obavezna.")]
    [MaxLength(128, ErrorMessage = "Lozinka može imati najviše 128 karaktera.")]
    string Password);
