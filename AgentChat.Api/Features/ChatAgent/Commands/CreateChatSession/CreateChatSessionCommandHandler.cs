using AgentChat.Api.Business.Interfaces;
using AgentChat.Api.Domain.Entities;
using AgentChat.Api.SharedKernel;
using MediatR;

namespace AgentChat.Api.Features.ChatAgent.Command.CreateChatSession;

internal sealed class CreateChatSessionCommandHandler : IRequestHandler<CreateChatSessionCommand, string>
{
    private readonly IChatService _chatService;

    public CreateChatSessionCommandHandler(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<string> Handle(CreateChatSessionCommand request, CancellationToken cancellationToken)
    {
        var chatSession = new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow
        };
        var result = await _chatService.CreateChatAsync(chatSession, cancellationToken);

        return result;
    }
}
