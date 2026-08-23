using System.ComponentModel.DataAnnotations;

namespace Cogniva.Api.DTOs.Auth;

public sealed record RegisterRequest(
    [Required(ErrorMessage = "Email adresa je obavezna.")]
    [EmailAddress(ErrorMessage = "Unesite ispravnu email adresu.")]
    [MaxLength(320, ErrorMessage = "Email adresa može imati najviše 320 karaktera.")]
    string Email,
    [Required(ErrorMessage = "Lozinka je obavezna.")]
    [MinLength(8, ErrorMessage = "Lozinka mora imati najmanje 8 karaktera.")]
    [MaxLength(128, ErrorMessage = "Lozinka može imati najviše 128 karaktera.")]
    string Password,
    [Required(ErrorMessage = "Ime je obavezno.")]
    [MaxLength(100, ErrorMessage = "Ime može imati najviše 100 karaktera.")]
    string FirstName,
    [Required(ErrorMessage = "Prezime je obavezno.")]
    [MaxLength(100, ErrorMessage = "Prezime može imati najviše 100 karaktera.")]
    string LastName);
