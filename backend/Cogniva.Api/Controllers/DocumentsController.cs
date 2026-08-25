using Cogniva.Api.DTOs.Documents;
using Cogniva.Api.Middleware;
using Cogniva.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cogniva.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/documents")]
public sealed class DocumentsController(
    IDocumentService documentService,
    IDocumentProcessingService documentProcessingService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentListItemResponse>>> GetDocuments(
        CancellationToken cancellationToken)
    {
        return Ok(await documentService.GetDocumentsAsync(GetUserId(), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDetailsResponse>> GetDocument(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await documentService.GetDocumentAsync(GetUserId(), id, cancellationToken));
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<UploadDocumentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UploadDocumentResponse>> UploadDocument(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        var result = await documentService.UploadDocumentAsync(GetUserId(), file, cancellationToken);
        return CreatedAtAction(nameof(GetDocument), new { id = result.Id }, result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDocument(Guid id, CancellationToken cancellationToken)
    {
        await documentService.DeleteDocumentAsync(GetUserId(), id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/process")]
    public async Task<ActionResult<DocumentDetailsResponse>> ProcessDocument(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await documentProcessingService.ProcessDocumentAsync(GetUserId(), id, cancellationToken));
    }

    private Guid GetUserId() => currentUserService.UserId
        ?? throw new ApiException(
            StatusCodes.Status401Unauthorized,
            "Prijava je neophodna.",
            "Prijavite se da biste pristupili dokumentima.");
}
