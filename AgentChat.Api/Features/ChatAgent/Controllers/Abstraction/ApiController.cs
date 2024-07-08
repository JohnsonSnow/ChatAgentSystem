using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgentChat.Api.Features.ChatAgent.Controllers.Abstraction;

[ApiController]
public abstract class ApiController : ControllerBase
{
    protected readonly ISender Sender;

    protected ApiController(ISender sender)
    {
        Sender = sender;
    }
}
