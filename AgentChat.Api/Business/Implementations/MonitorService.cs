using AgentChat.Api.Domain.Entities;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;
using Hangfire;
using Microsoft.Extensions.Caching.Distributed;

namespace AgentChat.Api.Business.Implementations;

public class MonitorService
{
    private readonly ConcurrentDictionary<Guid, ChatSession> _activeChats;
    private readonly IDistributedCache _cache;
    private readonly ILogger<MonitorService> _logger;

    public MonitorService(ConcurrentDictionary<Guid, ChatSession> activeChats, IDistributedCache cache, ILogger<MonitorService> logger)
    {
        _activeChats = activeChats;
        _cache = cache;
        _logger = logger;
    }

    public async Task MonitorChatSessionAsync(Guid chatSessionId)
    {
        if (_activeChats.TryGetValue(chatSessionId, out var chatSession))
        {
            if (!chatSession.IsActive)
            {
                _logger.LogInformation("Chat {ChatSessionId} marked as inactive.", chatSessionId);
                return;
            }

            var lastActivity = chatSession.LastActivity ?? chatSession.AssignedAt;
            if ((DateTime.UtcNow - lastActivity.Value).TotalMinutes > 5)
            {
                chatSession.IsActive = false;
                _logger.LogInformation("Chat {ChatSessionId} marked as inactive due to inactivity.", chatSessionId);
            }

            await _cache.SetStringAsync($"chat:{chatSessionId}", JsonSerializer.Serialize(chatSession));

            BackgroundJob.Schedule(() => MonitorChatSessionAsync(chatSessionId), TimeSpan.FromSeconds(5));
        }
    }
}
