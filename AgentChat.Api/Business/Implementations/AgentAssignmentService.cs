using AgentChat.Api.Business.Implementations;
using AgentChat.Api.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AgentChat.Api.Business.Implementations
{
    public class AgentAssignmentService
    {
        private readonly List<Agent> _agents;
        private readonly ConcurrentDictionary<Guid, ChatSession> _activeChats;
        private readonly QueueService _queueService;
        private readonly MonitorService _monitorService;
        private readonly ILogger<AgentAssignmentService> _logger;
        private readonly IDistributedCache _cache;


        public AgentAssignmentService(List<Agent> agents, ConcurrentDictionary<Guid, ChatSession> activeChats, QueueService queueService, MonitorService monitorService, ILogger<AgentAssignmentService> logger, IDistributedCache cache)
        {
            _agents = agents;
            _activeChats = activeChats;
            _queueService = queueService;
            _monitorService = monitorService;
            _logger = logger;
            _cache = cache;
        }

        public async Task MonitorQueueAsync()
        {
            while (_queueService.TryDequeueChatSession(out var chatSession))
            {
                var agent = GetNextAvailableAgent();
                if (agent != null)
                {
                    chatSession.AssignedAgentId = agent.Id;
                    chatSession.AssignedAt = DateTime.UtcNow;
                    chatSession.IsActive = true;

                    agent.CurrentChats++;

                    // Add chat session to active chats dictionary
                    if (!_activeChats.TryAdd(chatSession.Id, chatSession))
                    {
                        // Handle the case where adding to active chats fails
                        _logger.LogWarning("Failed to add chat {ChatSessionId} to active chats.", chatSession.Id);

                        agent.CurrentChats--; // Rollback agent's current chats count
                        continue; // Skip to the next chat session
                    }

                    // Update cache with chat session details
                    await _cache.SetStringAsync($"chat:{chatSession.Id}", JsonSerializer.Serialize(chatSession));

                    _logger.LogInformation("Chat {ChatSessionId} assigned to Agent {AgentId}.", chatSession.Id, agent.Id);

                    // Schedule a job to monitor this chat session's activity

                    await _monitorService.MonitorChatSessionAsync(chatSession.Id);
                }
            }
        }

        private Agent GetNextAvailableAgent()
        {
            var availableAgents = _agents.Where(a => a.IsAvailable && a.CurrentChats < a.GetMaxChats()).OrderBy(a => a.CurrentChats).ToList();
            return availableAgents.FirstOrDefault();
        }
    }

}


