using System.ComponentModel.DataAnnotations;

namespace Cogniva.Api.DTOs.Conversations;

public sealed record SendMessageRequest(
    [Required(ErrorMessage = "Poruka je obavezna.")]
    [MinLength(1, ErrorMessage = "Poruka je obavezna.")]
    [MaxLength(4000, ErrorMessage = "Poruka može imati najviše 4000 karaktera.")]
    string Content);
