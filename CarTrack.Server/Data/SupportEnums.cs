namespace CarTrack.Server.Data;

public enum TicketType
{
    Ticket,
    Feedback,
    BugReport,
}

public enum TicketStatus
{
    Open,
    InProgress,
    Resolved,
    Closed,
}

public enum TicketPriority
{
    Low,
    Medium,
    High,
    Critical,
}
