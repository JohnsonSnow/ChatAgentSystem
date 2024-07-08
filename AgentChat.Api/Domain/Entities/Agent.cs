namespace AgentChat.Api.Domain.Entities;

public class Agent
{
    private Agent(Guid id, string name, string seniority, bool isAvailable, int currentChats)
    {
        Id = id;
        Name = name;
        Seniority = seniority;
        IsAvailable = isAvailable;
        CurrentChats = currentChats;
    }

    public Agent()
    {
    }

    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Seniority { get; set; }
    public bool IsAvailable { get; set; }
    public int CurrentChats { get; set; }

    public int GetMaxChats()
    {
        var seniorityMultiplier = Seniority switch
        {
            "Junior" => 0.4,
            "Mid-Level" => 0.6,
            "Senior" => 0.8,
            "Team Lead" => 0.5,
            _ => 0.4
        };
        return (int)(10 * seniorityMultiplier);
    }

    public static Agent Create(Guid id, string name, string seniority, bool isAvailable, int currentChats)
    {
        var agent = new Agent(Guid.NewGuid(), name, seniority, isAvailable, currentChats);

        return agent;
    }
}
