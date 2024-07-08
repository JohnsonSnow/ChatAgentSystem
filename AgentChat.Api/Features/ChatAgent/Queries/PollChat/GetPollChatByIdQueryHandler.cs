using AgentChat.Api.Business.Interfaces;
using MediatR;

namespace AgentChat.Api.Features.ChatAgent.Queries.PollChat
{
    internal sealed class GetPollChatByIdQueryHandler : IRequestHandler<GetPollChatByIdQuery, bool>
    {
        private readonly IChatService _chatService;

        public GetPollChatByIdQueryHandler(IChatService chatService)
        {
            _chatService = chatService;
        }

        public Task<bool> Handle(GetPollChatByIdQuery request, CancellationToken cancellationToken)
        {
            var result = _chatService.PollChatAsync(request.id, cancellationToken);

            return result;
        }
    }
}
