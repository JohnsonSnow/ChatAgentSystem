namespace AgentChat.Api.Domain.Entities;

public class ChatSession
{

    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid AssignedAgentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? LastActivity { get; set; }
    public bool IsActive { get; set; }
}
