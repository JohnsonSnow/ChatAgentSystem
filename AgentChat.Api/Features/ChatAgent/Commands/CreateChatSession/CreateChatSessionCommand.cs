using AgentChat.Api.SharedKernel;
using MediatR;

namespace AgentChat.Api.Features.ChatAgent.Command.CreateChatSession;

public record CreateChatSessionCommand(string UserId) : IRequest<string>;

