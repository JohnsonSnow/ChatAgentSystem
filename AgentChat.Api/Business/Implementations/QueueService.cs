using AgentChat.Api.Domain.Entities;
using System.Collections.Concurrent;

namespace AgentChat.Api.Business.Implementations;

public class QueueService
{
    private readonly ConcurrentQueue<ChatSession> _queue = new ConcurrentQueue<ChatSession>();

    public void EnqueueChatSession(ChatSession chatSession)
    {
        _queue.Enqueue(chatSession);
    }

    public bool TryDequeueChatSession(out ChatSession chatSession)
    {
        return _queue.TryDequeue(out chatSession);
    }

    public int GetQueueCount()
    {
        return _queue.Count;
    }
}