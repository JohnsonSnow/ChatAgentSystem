using AgentChat.Api.Business.Interfaces;
using AgentChat.Api.Domain.Entities;
using Hangfire;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AgentChat.Api.Business.Implementations
{
    public class ChatService : IChatService
    {
        private readonly QueueService _queueService;
        private readonly AgentAssignmentService _agentAssignmentService;
        private readonly List<Agent> _agents;
        private readonly int _maxQueueLength;
        private readonly TimeSpan _officeHoursStart = TimeSpan.FromHours(9);
        private readonly TimeSpan _officeHoursEnd = TimeSpan.FromHours(17);
        private readonly ILogger<ChatService> _logger;
        private readonly IDistributedCache _cache;

        public ChatService(ILogger<ChatService> logger, IDistributedCache cache, QueueService queueService, AgentAssignmentService agentAssignmentService)
        {
            _logger = logger;
            _cache = cache;
            _agents = InitializeAgents();
            _maxQueueLength = CalculateMaxQueueLength(_agents);
            _queueService = queueService;
            _agentAssignmentService = agentAssignmentService;
        }

        public async Task<string> CreateChatAsync(ChatSession chatSession, CancellationToken cancellationToken)
        {
            if (_queueService.GetQueueCount() >= _maxQueueLength && (!IsWithinOfficeHours() || IsOverflowFull()))
            {
                return string.Empty;
            }

            _queueService.EnqueueChatSession(chatSession);

            _logger.LogInformation("Enqueue Chat Session in process");

            BackgroundJob.Enqueue(() => _agentAssignmentService.MonitorQueueAsync());

            return chatSession.Id.ToString();
        }

        public async Task<bool> PollChatAsync(Guid id, CancellationToken cancellationToken)
        {
            string? chatJson = await _cache.GetStringAsync($"chat:{id}");
            if (!string.IsNullOrEmpty(chatJson))
            {
                var chat = JsonSerializer.Deserialize<ChatSession>(chatJson);
                if (chat != null)
                {
                    chat.LastActivity = DateTime.UtcNow;
                    await _cache.SetStringAsync($"chat:{id}", JsonSerializer.Serialize(chat));
                    return await Task.FromResult(true);
                }
            }

            return await Task.FromResult(false);
        }


        private bool IsWithinOfficeHours()
        {
            var now = DateTime.UtcNow.TimeOfDay;
            return now >= _officeHoursStart && now <= _officeHoursEnd;
        }

        private bool IsOverflowFull()
        {
            var overflowAgents = _agents.Where(agent => agent.Name.StartsWith("Overflow"));
            var overflowCapacity = overflowAgents.Sum(agent => agent.GetMaxChats());
            var currentOverflowChats = overflowAgents.Sum(agent => agent.CurrentChats);
            return currentOverflowChats >= overflowCapacity;
        }

        private int CalculateMaxQueueLength(List<Agent> agents)
        {
            var capacity = agents.Sum(agent => agent.GetMaxChats());
            return (int)(capacity * 1.5);
        }

        public static List<Agent> InitializeAgents()
        {
            // Initialize agents based on the provided teams
            var agents = new List<Agent>
            {
                new Agent { Id = Guid.NewGuid(), Name = "Team Lead A", Seniority = "Team Lead", IsAvailable = true, CurrentChats = 0 },
                new Agent { Id = Guid.NewGuid(), Name = "Mid Level A1", Seniority = "Mid-Level", IsAvailable = true, CurrentChats = 0 },
                new Agent { Id = Guid.NewGuid(), Name = "Mid Level A2", Seniority = "Mid-Level", IsAvailable = true, CurrentChats = 0 },
                new Agent { Id = Guid.NewGuid(), Name = "Junior A", Seniority = "Junior", IsAvailable = true, CurrentChats = 0 },
                new Agent { Id = Guid.NewGuid(), Name = "Senior B", Seniority = "Senior", IsAvailable = true, CurrentChats = 0 },
                new Agent { Id = Guid.NewGuid(), Name = "Mid Level B", Seniority = "Mid-Level", IsAvailable = true, CurrentChats = 0 },
                new Agent { Id = Guid.NewGuid(), Name = "Junior B1", Seniority = "Junior", IsAvailable = true, CurrentChats = 0 },
                new Agent { Id = Guid.NewGuid(), Name = "Junior B2", Seniority = "Junior", IsAvailable = true, CurrentChats = 0 },
                new Agent { Id = Guid.NewGuid(), Name = "Mid Level C1", Seniority = "Mid-Level", IsAvailable = true, CurrentChats = 0 },
                new Agent { Id = Guid.NewGuid(), Name = "Mid Level C2", Seniority = "Mid-Level", IsAvailable = true, CurrentChats = 0 }
            };

            // Add overflow team agents
            for (int i = 11; i <= 16; i++)
            {
                agents.Add(new Agent { Id = Guid.NewGuid(), Name = $"Overflow {i - 10}", Seniority = "Junior", IsAvailable = true, CurrentChats = 0 });
            }

            return agents;
        }

    }
}
