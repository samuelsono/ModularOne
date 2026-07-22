namespace CarTrack.Modules.Leave;

public record LeaveRequestDto(
    Guid Id,
    string RequesterUserId,
    string RequesterDisplayName,
    string ManagerUserId,
    Guid LeaveTypeId,
    string LeaveType,
    string? LeaveTypeColor,
    string StartDate,
    string EndDate,
    string StartDayPortion,
    string EndDayPortion,
    decimal WorkingDays,
    string Status,
    string? Notes,
    bool HasDocument,
    string? DocumentFileName,
    string CreatedAt,
    string? CreatedByUserId,
    string? CreatedByDisplayName,
    string UpdatedAt,
    string? UpdatedByUserId,
    string? UpdatedByDisplayName,
    string? DecidedAt);

public record CreateLeaveRequest(
    Guid LeaveTypeId,
    string StartDate,
    string EndDate,
    string? Notes,
    string? StartDayPortion = null,
    string? EndDayPortion = null,
    /// <summary>When set by HR/Admin, create the request for this employee instead of the caller.</summary>
    string? OnBehalfOfUserId = null);

public record CancelLeaveRequest(string Notes);

public record LeaveTypeDto(
    Guid Id,
    string Name,
    string Code,
    string Color,
    bool IsPaid,
    bool DeductsBalance,
    bool RequiresDocument,
    bool AllowHalfDay,
    string AccrualMethod,
    decimal? AnnualEntitlement,
    int? MaxConsecutiveDays,
    int MinNoticeDays,
    string EligibleGender,
    bool IsActive,
    int SortOrder,
    string CreatedAt,
    string? CreatedByUserId,
    string? CreatedByDisplayName,
    string UpdatedAt,
    string? UpdatedByUserId,
    string? UpdatedByDisplayName);

public record SaveLeaveTypeRequest(
    string Name,
    string Code,
    string? Color,
    bool IsPaid,
    bool DeductsBalance,
    bool RequiresDocument,
    bool AllowHalfDay,
    string AccrualMethod,
    decimal? AnnualEntitlement,
    int? MaxConsecutiveDays,
    int MinNoticeDays,
    string EligibleGender,
    bool IsActive,
    int SortOrder);

public record PublicHolidayDto(
    Guid Id,
    string Name,
    string Date,
    bool IsRecurring,
    string? Branch);

public record SavePublicHolidayRequest(
    string Name,
    string Date,
    bool IsRecurring,
    string? Branch);

public record LeaveBalanceDto(
    Guid Id,
    string UserId,
    Guid LeaveTypeId,
    string LeaveTypeName,
    string? LeaveTypeColor,
    string CycleStart,
    string CycleEnd,
    decimal Allocated,
    decimal Used,
    decimal Pending,
    decimal Adjusted,
    decimal Remaining);

public record AdjustLeaveBalanceRequest(
    string UserId,
    Guid LeaveTypeId,
    string CycleStart,
    string CycleEnd,
    decimal AllocatedDelta,
    decimal AdjustedDelta);

public record LeaveReportCountDto(string Label, int Count);

public record LeaveReportSummaryDto(
    int PendingCount,
    int OnLeaveTodayCount,
    decimal TotalRemainingDays,
    decimal RemainingAnnualDays,
    decimal RemainingSickDays,
    decimal RemainingOtherDays,
    IReadOnlyList<LeaveReportCountDto> ByType,
    IReadOnlyList<LeaveReportCountDto> ByStatus,
    IReadOnlyList<LeaveReportCountDto> ByDepartment);

public record LeaveHistoryRowDto(
    Guid Id,
    string RequesterUserId,
    string ManagerUserId,
    string RequesterDisplayName,
    string? Department,
    string? Branch,
    string LeaveType,
    string StartDate,
    string EndDate,
    decimal WorkingDays,
    string Status,
    string? Notes,
    string CreatedAt,
    string? CreatedByUserId,
    string? CreatedByDisplayName,
    string UpdatedAt,
    string? UpdatedByUserId,
    string? UpdatedByDisplayName,
    string? DecidedAt);

public record LeaveHistoryFilters(
    string? Status,
    string? Department,
    string? Branch,
    string? StartDate,
    string? EndDate,
    string? UserId,
    int? Year);

public record LeaveLiabilityRowDto(
    string UserId,
    string DisplayName,
    string? Department,
    string LeaveTypeName,
    string LeaveTypeCode,
    string? LeaveTypeColor,
    decimal Allocated,
    decimal Used,
    decimal Pending,
    decimal Remaining);

public record LeaveCalendarEntryDto(
    Guid RequestId,
    string UserId,
    string DisplayName,
    string? Department,
    string? Branch,
    Guid LeaveTypeId,
    string LeaveTypeName,
    string LeaveTypeColor,
    string StartDate,
    string EndDate,
    string Status,
    decimal WorkingDays);

public record LeaveCalendarResponse(
    IReadOnlyList<LeaveCalendarEntryDto> Entries,
    IReadOnlyList<PublicHolidayDto> Holidays,
    IReadOnlyList<string> Departments,
    IReadOnlyList<string> Branches);

public record LeaveCalendarFilters(
    string StartDate,
    string EndDate,
    string? Branch,
    string? Department,
    string? UserId);

public record WorkingDaysResult(
    string StartDate,
    string EndDate,
    decimal WorkingDays,
    IReadOnlyList<string> HolidayDates,
    string StartDayPortion,
    string EndDayPortion);

public record LeaveDocumentMetadata(string StoredPath, string FileName, string ContentType);

public record WorkLocationTypeDto(
    Guid Id,
    string Code,
    string Name,
    string Color,
    bool TracksCollaborators,
    bool IsActive,
    int SortOrder);

public record SaveWorkLocationTypeRequest(
    string Name,
    string Code,
    string? Color,
    bool TracksCollaborators,
    bool IsActive,
    int SortOrder);

public record ScheduleTemplateDayDto(int DayOfWeek, Guid LocationTypeId, string LocationTypeName, string LocationTypeColor);

public record ScheduleTemplateDto(
    Guid Id,
    string UserId,
    string UserDisplayName,
    string EffectiveFrom,
    string? EffectiveTo,
    string? Notes,
    IReadOnlyList<ScheduleTemplateDayDto> Days);

public record UpsertScheduleTemplateRequest(
    string? UserId,
    string EffectiveFrom,
    string? EffectiveTo,
    string? Notes,
    IReadOnlyList<ScheduleTemplateDayInput> Days);

public record ScheduleTemplateDayInput(int DayOfWeek, Guid LocationTypeId);

public record ScheduleDayOverrideDto(
    Guid Id,
    string UserId,
    string Date,
    Guid LocationTypeId,
    string LocationTypeName,
    string LocationTypeColor,
    string? Notes);

public record UpsertScheduleDayOverrideRequest(
    string? UserId,
    string Date,
    Guid LocationTypeId,
    string? Notes);

public record ResolvedScheduleDayDto(
    string UserId,
    string UserDisplayName,
    string Date,
    string Kind,
    Guid? LocationTypeId,
    string? LocationTypeCode,
    string? LocationTypeName,
    string? LocationTypeColor,
    bool TracksCollaborators,
    string? LeaveRequestId,
    string? LeaveTypeName,
    bool FromOverride);

public record AttendanceCollaboratorDto(
    string? CollaboratorUserId,
    string? CollaboratorDisplayName,
    string? ExternalName);

public record AttendanceDayDto(
    Guid Id,
    string UserId,
    string UserDisplayName,
    string Date,
    Guid? PlannedLocationTypeId,
    string? PlannedLocationTypeName,
    string? PlannedLocationTypeColor,
    Guid ActualLocationTypeId,
    string ActualLocationTypeName,
    string ActualLocationTypeColor,
    bool TracksCollaborators,
    string Source,
    string? Notes,
    string RecordedByUserId,
    string RecordedByDisplayName,
    string RecordedAt,
    IReadOnlyList<AttendanceCollaboratorDto> Collaborators);

public record UpsertAttendanceDayRequest(
    string? UserId,
    Guid ActualLocationTypeId,
    string? Notes,
    IReadOnlyList<AttendanceCollaboratorInput>? Collaborators);

public record AttendanceCollaboratorInput(string? CollaboratorUserId, string? ExternalName);

public record AttendanceCompareRowDto(
    string UserId,
    string UserDisplayName,
    string Date,
    string PlannedKind,
    Guid? PlannedLocationTypeId,
    string? PlannedLocationTypeName,
    string? PlannedLocationTypeColor,
    Guid? ActualLocationTypeId,
    string? ActualLocationTypeName,
    string? ActualLocationTypeColor,
    bool HasActual,
    bool IsMatch,
    bool IsMismatch,
    IReadOnlyList<AttendanceCollaboratorDto> Collaborators);

public record AttendancePolicySettingsDto(
    string DefaultAssumption,
    string? UpdatedAt);

public record UpdateAttendancePolicySettingsRequest(string DefaultAssumption);

