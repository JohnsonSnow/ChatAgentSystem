using AgentChat.Api.Domain.Entities;
using AgentChat.Api.Features.ChatAgent.Command.CreateChatSession;
using AgentChat.Api.Features.ChatAgent.Controllers.Abstraction;
using AgentChat.Api.Features.ChatAgent.Queries.PollChat;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentChat.Api.Features.ChatAgent.Controllers
{
    [Route("api/[controller]")]
    public class ChatAgentController : ApiController
    {
        public ChatAgentController(ISender sender) : base(sender)
        {
        }

        [HttpPost(nameof(CreateChatSession))]
        public async Task<IActionResult> CreateChatSession([FromBody] CreateChatSessionCommand request, CancellationToken cancellationToken)
        {
            var command = new CreateChatSessionCommand(request.UserId);

            var result = await Sender.Send(command, cancellationToken);

            if (string.IsNullOrEmpty(result))
            {
                return StatusCode(503, "Chat queue is full or outside office hours.");
            }
            return Ok(result);

        }

        [HttpGet("poll/{id}")]
        public async Task<IActionResult> PollChat(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetPollChatByIdQuery(id);
            var result = await Sender.Send(query, cancellationToken);

            if (result)
            {
                return Ok(new { status = "active" });
            }
            else
            {
                return NotFound(new { status = "inactive" });
            }
        }
    }
}
