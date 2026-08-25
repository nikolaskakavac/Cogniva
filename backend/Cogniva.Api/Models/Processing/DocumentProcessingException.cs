namespace Cogniva.Api.Models.Processing;

public sealed class DocumentProcessingException(string message) : Exception(message);
