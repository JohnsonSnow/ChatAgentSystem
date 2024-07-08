using AgentChat.Api.Domain.Entities;

namespace AgentChat.Api.Business.Interfaces
{
    public interface IChatService
    {
        Task<string> CreateChatAsync(ChatSession chatSession, CancellationToken cancellationToken);
        Task<bool> PollChatAsync(Guid id, CancellationToken cancellationToken);

        //Task<ChatSession?> CreateSessionAsync();
        //Task AssignChatSessionsAsync();
    }
}
