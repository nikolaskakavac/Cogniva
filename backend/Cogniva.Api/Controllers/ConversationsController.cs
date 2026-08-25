using Cogniva.Api.DTOs.Conversations;
using Cogniva.Api.Middleware;
using Cogniva.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cogniva.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/conversations")]
public sealed class ConversationsController(
    IConversationService conversationService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ConversationDetailsResponse>> Create(
        CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await conversationService.CreateAsync(GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationListItemResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await conversationService.GetAllAsync(GetUserId(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversationDetailsResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await conversationService.GetAsync(GetUserId(), id, cancellationToken));

    [HttpPost("{id:guid}/messages")]
    public async Task<ActionResult<MessageResponse>> SendMessage(
        Guid id,
        SendMessageRequest request,
        CancellationToken cancellationToken) =>
        Ok(await conversationService.SendMessageAsync(GetUserId(), id, request, cancellationToken));

    [HttpPost("{id:guid}/messages/{messageId:guid}/retry")]
    public async Task<ActionResult<MessageResponse>> RetryMessage(
        Guid id,
        Guid messageId,
        CancellationToken cancellationToken) =>
        Ok(await conversationService.RetryMessageAsync(GetUserId(), id, messageId, cancellationToken));

    private Guid GetUserId() => currentUserService.UserId
        ?? throw new ApiException(401, "Prijava je neophodna.", "Prijavite se da biste pristupili razgovorima.");
}
