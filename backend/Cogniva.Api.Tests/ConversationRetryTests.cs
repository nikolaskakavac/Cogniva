using Cogniva.Api.Middleware;
using Cogniva.Api.Models;
using Cogniva.Api.Services;

namespace Cogniva.Api.Tests;

public sealed class ConversationRetryTests
{
    [Fact]
    public void EnsureRetryable_AllowsLatestUserMessageWithoutResponse()
    {
        var id = Guid.NewGuid();
        ConversationService.EnsureRetryable(MessageRole.User, id, id);
    }

    [Fact]
    public void EnsureRetryable_RejectsAssistantMessage()
    {
        var id = Guid.NewGuid();
        var exception = Assert.Throws<ApiException>(() =>
            ConversationService.EnsureRetryable(MessageRole.Assistant, id, id));
        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public void EnsureRetryable_RejectsUserMessageThatAlreadyHasLaterMessage()
    {
        var exception = Assert.Throws<ApiException>(() =>
            ConversationService.EnsureRetryable(MessageRole.User, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Equal(409, exception.StatusCode);
    }
}
