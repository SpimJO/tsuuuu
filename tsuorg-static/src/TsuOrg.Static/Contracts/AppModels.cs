namespace TsuOrg.Frontend.Models;

// ── Catalog / reference ────────────────────────────────────────────────────

public sealed record DocumentTypeDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    IReadOnlyList<RequirementDto> Requirements);

public sealed record RequirementDto(
    Guid Id,
    string RequirementKey,
    string DisplayName,
    bool IsAttachment,
    bool IsMandatory,
    bool IsConditional,
    string? ConditionExpression);

public sealed record OrganizationDto(
    Guid Id,
    string Name,
    string? Acronym,
    string? College,
    string? Status,
    string? Semester);

public sealed record AcademicYearDto(
    Guid Id,
    string Label,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsCurrent);

// ── Documents ──────────────────────────────────────────────────────────────

public sealed record CreateDocumentRequest(
    Guid OrganizationId,
    Guid DocumentTypeId,
    Guid AcademicYearId,
    string? Title);

public sealed record CreateDocumentResult(Guid DocumentId, string DocumentNumber);

public sealed record UploadFileResult(string BlobPath, string FileName);

public sealed record SubmitDocumentResult(
    Guid DocumentId,
    string DocumentNumber,
    string Status,
    string Message);

public sealed record ConfirmSubmissionResult(
    Guid DocumentId,
    string DocumentNumber,
    string Status,
    string CurrentStage,
    bool IsMetadataLocked,
    string MetadataLockHash,
    DateTimeOffset MetadataLockedAt,
    string ValidationStatus,
    string Message);

public sealed record DocumentSummaryDto(
    Guid Id,
    string DocumentNumber,
    string Title,
    string OrganizationName,
    string DocumentTypeCode,
    string Status,
    string CurrentStage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record PagedDocumentsDto(
    IReadOnlyList<DocumentSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record DocumentDetailDto(
    Guid Id,
    string DocumentNumber,
    string Title,
    string OrganizationName,
    string DocumentTypeCode,
    string DocumentTypeName,
    string AcademicYear,
    string SubmittedBy,
    string Status,
    string CurrentStage,
    string? PrimaryFileName,
    string? PrimaryContentType,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    bool IsMetadataLocked,
    string? MetadataLockHash,
    DateTimeOffset? MetadataLockedAt,
    IReadOnlyList<AttachmentDto> Attachments,
    ValidationSummaryDto? LatestValidation);

public sealed record AttachmentDto(
    Guid Id,
    string AttachmentType,
    string FileName,
    string ContentType);

public sealed record ValidationSummaryDto(
    string DocumentClass,
    decimal Confidence,
    bool RequiresHumanReview,
    string ModelVersion,
    DateTimeOffset ProcessedAt,
    string FieldResultsJson);

// ── Validation detail ──────────────────────────────────────────────────────

public sealed record OcrLowConfidenceFlagDto(string Token, double Confidence, string Flag);

public sealed record ExtractedFieldDto(string Label, string Value);

public sealed record ComplianceCheckDto(string Label, bool Passed);

public sealed record ValidationDetailDto(
    Guid Id,
    string DocumentClass,
    decimal Confidence,
    bool RequiresHumanReview,
    string ModelVersion,
    DateTimeOffset ProcessedAt,
    System.Text.Json.JsonElement Fields,
    System.Text.Json.JsonElement? Attachments,
    System.Text.Json.JsonElement? Preprocessing,
    System.Text.Json.JsonElement? ConsistencyIssues,
    string? OcrFullText,
    decimal? OcrAvgConfidence,
    int? OcrTokenCount,
    IReadOnlyList<string> KeyFieldsIdentified,
    IReadOnlyList<OcrLowConfidenceFlagDto> LowConfidenceFlags,
    string RawResponseJson,
    int ScorePercent = 0,
    string Verdict = "",
    IReadOnlyList<ExtractedFieldDto>? ExtractedContent = null,
    IReadOnlyList<ComplianceCheckDto>? Checks = null,
    string? OcrCaption = null,
    string? OcrScannedCaption = null);

public sealed record TrackerStepDto(
    string Key,
    string Label,
    string State,
    string Tone,
    DateTimeOffset? OccurredAt = null);

public sealed record TrackerDocumentCardDto(
    Guid Id,
    string DocumentNumber,
    string Title,
    string OrganizationName,
    string DocumentTypeCode,
    string Status,
    string CurrentStage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    int CurrentIndex,
    string OverallStatus,
    string OverallTone,
    IReadOnlyList<TrackerStepDto> Steps,
    int? Score = null);

public sealed record TrackerTabCountDto(string Tab, int Count);

public sealed record TrackerListResult(
    IReadOnlyList<TrackerDocumentCardDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<TrackerTabCountDto> TabCounts);

public sealed record CollegeChipDto(string Code, string Name);

public sealed record SouTrackerDocumentDto(
    Guid Id,
    string DocumentNumber,
    string Title,
    string OrganizationName,
    string? CollegeCode,
    string PipelineLabel,
    string Status,
    string CurrentStage,
    DateTimeOffset SubmittedAt,
    string? AssignedTo,
    int? Score,
    int FlagCount,
    int DaysElapsed,
    string CleanLabel);

public sealed record SouTrackerResult(
    IReadOnlyList<SouTrackerDocumentDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<TrackerTabCountDto> TabCounts,
    IReadOnlyList<CollegeChipDto> Colleges,
    IReadOnlyList<OrgComplianceDto> Organizations);

public sealed record TrackingTimelineDto(
    Guid DocumentId,
    string DocumentNumber,
    string Title,
    string OrganizationName,
    string DocumentTypeCode,
    string DocumentTypeName,
    string SubmittedBy,
    string? SubmittedByPosition,
    string? AdviserName,
    int? Score,
    string CurrentStatus,
    string CurrentStage,
    int CurrentIndex,
    string OverallStatus,
    string OverallTone,
    IReadOnlyList<TrackerStepDto> Steps,
    IReadOnlyList<TrackingEventDto> Events);

public sealed record TrackingEventDto(
    Guid Id,
    string Status,
    string Stage,
    string Message,
    Guid? ActorUserId,
    DateTimeOffset OccurredAt,
    string? Headline = null,
    string? Detail = null);

// ── Archive (DMA) ──────────────────────────────────────────────────────────

public sealed record ArchiveOrganizationDto(Guid Id, string Name, string? Acronym);

public sealed record ArchiveDocumentTypeDto(string Code, string Name);

public sealed record ArchiveDocumentDto(
    Guid Id,
    string DocumentNumber,
    string Title,
    ArchiveOrganizationDto Organization,
    ArchiveDocumentTypeDto DocumentType,
    string? AcademicYear,
    Guid AcademicYearId,
    string Status,
    string? PrimaryFileName,
    bool HasPrimaryFile,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    int VersionCount = 0);

public sealed record ArchiveListResult(
    IReadOnlyList<ArchiveDocumentDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ArchiveTypeCountDto(string Code, string Name, int Count);

public sealed record ArchiveStatusCountDto(string Status, int Count);

public sealed record ArchiveCategoryCountsDto(
    IReadOnlyList<ArchiveTypeCountDto> ByType,
    IReadOnlyList<ArchiveStatusCountDto> ByStatus);

// ── Workflow (WM) ──────────────────────────────────────────────────────────

public sealed record ReviewQueueItemDto(
    Guid DocumentId,
    string DocumentNumber,
    string Title,
    string OrganizationName,
    string DocumentTypeCode,
    string DocumentTypeName,
    string Status,
    string CurrentStage,
    DateTimeOffset SubmittedAt,
    string SubmittedBy,
    string? LatestValidationClass,
    decimal? LatestValidationConfidence,
    bool RequiresHumanReview = false);

public sealed record ReviewQueueResult(
    IReadOnlyList<ReviewQueueItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int AllCount = 0,
    int FlaggedCount = 0,
    int CleanedCount = 0);

public sealed record WorkflowDecisionResult(
    Guid DocumentId,
    string DocumentNumber,
    string Action,
    string NewStatus,
    string NewStage,
    DateTimeOffset DecidedAt,
    string? SignatureHash,
    string Message);

public sealed record WorkflowHistoryItemDto(
    Guid Id,
    string ActorRole,
    string ActorName,
    string Action,
    string? Comments,
    string? SignatureHash,
    DateTimeOffset DecidedAt);

public sealed record WorkflowHistoryDto(
    Guid DocumentId,
    string DocumentNumber,
    string CurrentStage,
    bool IsComplete,
    IReadOnlyList<WorkflowHistoryItemDto> History);

// ── Notifications ──────────────────────────────────────────────────────────

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    Guid? RelatedDocumentId,
    DateTimeOffset CreatedAt);

public sealed record NotificationListDto(
    int UnreadCount,
    IReadOnlyList<NotificationDto> Items);

public sealed record DocumentVersionDto(
    Guid Id,
    int VersionNumber,
    string FileName,
    string? ChangeSummary,
    DateTimeOffset CreatedAt);

public sealed record DocumentDownloadDto(string FileName, string SasUrl, string ContentType);

// ── Dashboard (Figure 5 / 14 / 21) ─────────────────────────────────────────

public sealed record DashboardDto(
    int TotalSubmitted,
    int UnderReview,
    int Approved,
    int Returned,
    int OutstandingDocs,
    int ComplianceRate,
    int ItemsFlagged,
    int PendingCount,
    int ApprovedThisMonth,
    int ReturnedCount,
    int TotalDocuments,
    IReadOnlyList<StageCountDto> ByStage,
    IReadOnlyList<TypeCountDto> ByType,
    IReadOnlyList<RecentDocDto> RecentDocuments,
    IReadOnlyList<ReviewedDocDto> ReviewedDocuments,
    int SubmittedThisMonth,
    int ActiveOrgs,
    IReadOnlyList<OrgComplianceDto> OrgCompliance);

public sealed record StageCountDto(string Stage, int Count);

public sealed record TypeCountDto(string TypeCode, string TypeName, int Count);

public sealed record RecentDocDto(
    Guid DocumentId,
    string DocumentNumber,
    string Title,
    string DocumentTypeName,
    string Status,
    string Stage,
    DateTimeOffset SubmittedAt,
    int? Score = null);

public sealed record ReviewedDocDto(
    Guid DocumentId,
    string DocumentNumber,
    string Title,
    string OrganizationName,
    string Action,
    DateTimeOffset DecidedAt,
    int? Score);

public sealed record OrgComplianceDto(
    Guid OrganizationId,
    string Name,
    string? Acronym,
    string? College,
    int Submissions,
    int Approved,
    int CompliancePercent);

public sealed record SouStatusBreakdownDto(
    int ApprovedCount,
    int UnderReviewCount,
    int ReturnedCount,
    int ApprovedPercent,
    int UnderReviewPercent,
    int ReturnedPercent);

public sealed record RecentValidationDto(
    Guid DocumentId,
    string DocumentNumber,
    string Title,
    string OrganizationName,
    string DocumentTypeName,
    string DocumentClass,
    int ScorePercent,
    DateTimeOffset ProcessedAt);

public sealed record SouDashboardDto(
    int TotalDocuments,
    int SubmittedThisMonth,
    int ApprovalRate,
    int ActiveOrgs,
    IReadOnlyList<TypeCountDto> ByType,
    SouStatusBreakdownDto StatusBreakdown,
    IReadOnlyList<OrgComplianceDto> OrgCompliance,
    IReadOnlyList<RecentValidationDto> RecentValidations);

// ── Review history (Figures 19–20) ─────────────────────────────────────────

public sealed record ReviewHistoryItemDto(
    Guid DocumentId,
    string DocumentNumber,
    string Title,
    string OrganizationName,
    string DocumentTypeCode,
    string Action,
    string? Comments,
    DateTimeOffset DecidedAt,
    int? Score);

public sealed record ReviewHistoryResult(
    IReadOnlyList<ReviewHistoryItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int AllCount = 0,
    int ApprovedCount = 0,
    int ReturnedCount = 0);

// ── SOU Workflow Monitor (Figure 22) ───────────────────────────────────────

public sealed record WorkflowMonitorItemDto(
    Guid DocumentId,
    string DocumentNumber,
    string Title,
    string OrganizationName,
    string StageLabel,
    string? AssignedTo,
    DateTimeOffset SubmittedAt,
    DateTimeOffset LastActivityAt,
    int DaysElapsed,
    int? Score,
    int FlagCount,
    string State);

public sealed record WorkflowMonitorResult(
    IReadOnlyList<WorkflowMonitorItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int AllActiveCount = 0,
    int FlaggedCount = 0,
    int StaleCount = 0);

// ── SOU Organizations Admin (Figure 26) ────────────────────────────────────

public sealed record OrganizationAdminDto(
    Guid Id,
    string Name,
    string? Acronym,
    string? College,
    string? AdviserName,
    string? PresidentName,
    string HealthStatus,
    int Submissions,
    int Approved,
    int Returned,
    int CompliancePercent);

public sealed record OrganizationsAdminResult(
    IReadOnlyList<OrganizationAdminDto> Items,
    int ActiveCount,
    int InactiveCount);

// ── SOU System Reports (Figure 27) ─────────────────────────────────────────

public sealed record MonthlySubmissionsDto(string Label, int Year, int Month, int Count);

public sealed record ProcessingTimeDto(
    double? OcrMinutes,
    double? AiValidationMinutes,
    double? AdviserReviewDays,
    double? FullCycleDays);

public sealed record SystemReportsDto(
    IReadOnlyList<MonthlySubmissionsDto> MonthlySubmissions,
    ProcessingTimeDto ProcessingTime,
    int TotalValidated,
    int ValidationSuccessRate,
    string? AcademicYearLabel = null);

// ── SOU System Audit (Figure 28) ───────────────────────────────────────────

public sealed record SystemAuditItemDto(
    Guid DocumentId,
    string DocumentNumber,
    string Title,
    string DocumentTypeCode,
    string Action,
    string ByName,
    string ByRole,
    DateTimeOffset At);

public sealed record SystemAuditResult(
    IReadOnlyList<SystemAuditItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

// ── Figure 20: Adviser / Dean scoped audit ───────────────────────────────────

public sealed record ScopedAuditItemDto(
    Guid DocumentId,
    string DocumentNumber,
    string Title,
    string DocumentTypeCode,
    string DocumentTypeName,
    string Activity,
    string ActivityKind,
    DateTimeOffset At);

public sealed record ScopedAuditResult(
    IReadOnlyList<ScopedAuditItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

// ── SOU System Settings (Figure 29) ────────────────────────────────────────

public sealed record SystemSettingsDto(
    bool AiValidationEnabled,
    bool OcrAutoProcessing,
    bool EmailNotifications,
    bool StaleDocumentAlerts,
    bool LowComplianceWarnings,
    int MinComplianceScore,
    string MandatoryAdviserReviewTypes);
