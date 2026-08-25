using System.ComponentModel.DataAnnotations;

namespace Cogniva.Api.DTOs.Conversations;

public sealed record CreateConversationRequest(
    [MaxLength(255, ErrorMessage = "Naziv razgovora može imati najviše 255 karaktera.")] string? Title,
    [Required(ErrorMessage = "Izaberite najmanje jedan dokument.")]
    [MinLength(1, ErrorMessage = "Izaberite najmanje jedan dokument.")]
    IReadOnlyList<Guid> DocumentIds);
