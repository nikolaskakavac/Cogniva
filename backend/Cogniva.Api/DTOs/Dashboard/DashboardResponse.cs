using Cogniva.Api.DTOs.Conversations;
using Cogniva.Api.DTOs.Documents;

namespace Cogniva.Api.DTOs.Dashboard;

public sealed record DashboardResponse(
    int DocumentCount,
    int ReadyDocumentCount,
    int ConversationCount,
    IReadOnlyList<DocumentListItemResponse> RecentDocuments,
    IReadOnlyList<ConversationListItemResponse> RecentConversations);
