namespace PingMe.DTOs.Poll;

public class CreatePollRequest
{
    public int? ReceiverId { get; set; }
    public int? GroupId { get; set; }
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = [];
    public bool AllowMultiple { get; set; } = false;
    public DateTime? EndsAt { get; set; }
}

public class PollVoteRequest
{
    public List<int> OptionIds { get; set; } = [];
}

public class PollOptionResponse
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
    public int VoteCount { get; set; }
    public double Percentage { get; set; }
    public List<int> VoterIds { get; set; } = [];
}

public class PollResponse
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public string Question { get; set; } = string.Empty;
    public bool AllowMultiple { get; set; }
    public DateTime? EndsAt { get; set; }
    public int TotalVotes { get; set; }
    public List<int> MyVoteOptionIds { get; set; } = [];
    public List<PollOptionResponse> Options { get; set; } = [];
}

public class PollVoteUpdatedEvent
{
    public int MessageId { get; set; }
    public int? GroupId { get; set; }
    public int? ReceiverId { get; set; }
    public int SenderId { get; set; }
    public PollResponse Poll { get; set; } = null!;
}
