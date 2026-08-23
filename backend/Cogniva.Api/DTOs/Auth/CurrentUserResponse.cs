namespace Cogniva.Api.DTOs.Auth;

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName);
