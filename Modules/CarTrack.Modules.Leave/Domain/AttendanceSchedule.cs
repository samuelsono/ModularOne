namespace CarTrack.Modules.Leave;

public class WorkLocationType
{
    public Guid Id { get; set; }

    public required string Code { get; set; }

    public required string Name { get; set; }

    public required string Color { get; set; }

    public bool TracksCollaborators { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}

public class ScheduleTemplate
{
    public Guid Id { get; set; }

    public required string UserId { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public string? Notes { get; set; }

    public ICollection<ScheduleTemplateDay> Days { get; set; } = new List<ScheduleTemplateDay>();
}

public class ScheduleTemplateDay
{
    public Guid Id { get; set; }

    public Guid TemplateId { get; set; }

    public ScheduleTemplate Template { get; set; } = null!;

    /// <summary>.NET <see cref="DayOfWeek"/> (0 = Sunday … 6 = Saturday).</summary>
    public int DayOfWeek { get; set; }

    public Guid LocationTypeId { get; set; }

    public WorkLocationType LocationType { get; set; } = null!;
}

public class ScheduleDayOverride
{
    public Guid Id { get; set; }

    public required string UserId { get; set; }

    public DateOnly Date { get; set; }

    public Guid LocationTypeId { get; set; }

    public WorkLocationType LocationType { get; set; } = null!;

    public string? Notes { get; set; }
}

public class AttendanceDay
{
    public Guid Id { get; set; }

    public required string UserId { get; set; }

    public DateOnly Date { get; set; }

    public Guid? PlannedLocationTypeId { get; set; }

    public WorkLocationType? PlannedLocationType { get; set; }

    public Guid ActualLocationTypeId { get; set; }

    public WorkLocationType ActualLocationType { get; set; } = null!;

    public required string Source { get; set; }

    public string? Notes { get; set; }

    public required string RecordedByUserId { get; set; }

    public DateTimeOffset RecordedAt { get; set; }

    public ICollection<AttendanceCollaborator> Collaborators { get; set; } = new List<AttendanceCollaborator>();
}

public class AttendanceCollaborator
{
    public Guid Id { get; set; }

    public Guid AttendanceDayId { get; set; }

    public AttendanceDay AttendanceDay { get; set; } = null!;

    public string? CollaboratorUserId { get; set; }

    public string? ExternalName { get; set; }
}
