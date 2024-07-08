using MediatR;

namespace AgentChat.Api.Features.ChatAgent.Queries.PollChat
{
    public record GetPollChatByIdQuery(Guid id) : IRequest<bool>
    {
    }
}
