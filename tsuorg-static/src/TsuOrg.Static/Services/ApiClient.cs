using System.Net;
using TsuOrg.Frontend.Models;

namespace TsuOrg.Frontend.Services;

public sealed class ApiClient
{
    private readonly MockStore _store;
    private readonly AuthSession _session;

    public ApiClient(MockStore store, AuthSession session)
    {
        _store = store;
        _session = session;
    }

    public Task<HttpResponseMessage> HealthAsync(CancellationToken ct = default) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

    public async Task<LoginResponse> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.Login(email, password);
    }

    public Task LogoutAsync(CancellationToken ct = default) => Tick(ct);

    public async Task<UserProfile?> GetProfileAsync(CancellationToken ct = default)
    {
        await Tick(ct);
        return _session.UserId is Guid id ? _store.Profile(id) : null;
    }

    public async Task<UserProfile> UpdateProfileAsync(string fullName, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.UpdateProfile(RequireUserId(), fullName);
    }

    public async Task<UserProfile> UploadAvatarAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        await Tick(ct);
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        var media = string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType;
        var dataUrl = $"data:{media};base64,{Convert.ToBase64String(ms.ToArray())}";
        return _store.UploadAvatar(RequireUserId(), dataUrl);
    }

    public async Task<string?> GetMyAvatarDataUrlAsync(CancellationToken ct = default)
    {
        await Tick(ct);
        return _session.UserId is Guid id ? _store.AvatarDataUrl(id) : null;
    }

    public async Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)
    {
        await Tick(ct);
        _store.ChangePassword(RequireUserId(), currentPassword, newPassword);
    }

    public async Task<IReadOnlyList<DocumentTypeDto>> GetDocumentTypesAsync(CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.DocumentTypes();
    }

    public async Task<IReadOnlyList<OrganizationDto>> GetMyOrganizationsAsync(CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.MyOrganizations(RequireUserId());
    }

    public async Task<IReadOnlyList<OrganizationDto>> GetOrganizationsAsync(CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.Organizations();
    }

    public async Task<AcademicYearDto?> GetCurrentAcademicYearAsync(CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.CurrentYear();
    }

    public async Task<CreateDocumentResult> CreateDocumentAsync(CreateDocumentRequest request, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.CreateDocument(RequireUserId(), request);
    }

    public async Task<UploadFileResult> UploadPrimaryFileAsync(
        Guid documentId, Stream content, string fileName, string contentType,
        IProgress<int>? progress = null, CancellationToken ct = default)
    {
        await SimulateUpload(content, progress, ct);
        return _store.UploadPrimary(documentId, fileName, contentType);
    }

    public async Task<UploadFileResult> UploadAttachmentAsync(
        Guid documentId, string attachmentType, Stream content, string fileName, string contentType,
        IProgress<int>? progress = null, CancellationToken ct = default)
    {
        await SimulateUpload(content, progress, ct);
        return _store.UploadAttachment(documentId, attachmentType, fileName, contentType);
    }

    public async Task<SubmitDocumentResult> SubmitDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.Submit(RequireUserId(), documentId);
    }

    public async Task<ConfirmSubmissionResult> ConfirmSubmissionAsync(Guid documentId, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.Confirm(RequireUserId(), documentId);
    }

    public async Task<DocumentDetailDto?> GetDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.GetDocument(documentId);
    }

    public async Task<PagedDocumentsDto> GetDocumentsAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.ListDocuments(RequireUserId(), page, pageSize);
    }

    public async Task<ValidationDetailDto?> GetValidationAsync(Guid documentId, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.GetValidation(documentId);
    }

    public async Task<TrackerListResult> GetTrackerAsync(
        string tab = "All", int page = 1, int pageSize = 50,
        string? search = null, string? college = null, Guid? organizationId = null,
        CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.Tracker(RequireUserId(), tab, page, pageSize, search, college, organizationId);
    }

    public async Task<SouTrackerResult> GetSouTrackerAsync(
        string tab = "All", int page = 1, int pageSize = 100,
        string? search = null, string? college = null, Guid? organizationId = null,
        string complianceBand = "all", CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.SouTracker(tab, page, pageSize, search, college, organizationId, complianceBand);
    }

    public async Task<WorkflowMonitorResult> GetWorkflowMonitorAsync(string tab = "AllActive", int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.Monitor(tab, page, pageSize);
    }

    public async Task<OrganizationsAdminResult> GetOrganizationsAdminAsync(string tab = "Active", CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.OrganizationsAdmin(tab);
    }

    public async Task<SystemReportsDto?> GetSystemReportsAsync(CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.Reports();
    }

    public async Task<(string FileName, string ContentType, byte[] Bytes)> DownloadReportAsync(string type, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.DownloadReport(type);
    }

    public async Task<SystemAuditResult> GetSystemAuditAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.SystemAudit(page, pageSize);
    }

    public async Task<ScopedAuditResult> GetScopedAuditAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.ScopedAudit(RequireUserId(), page, pageSize);
    }

    public async Task<SystemSettingsDto?> GetSystemSettingsAsync(CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.Settings();
    }

    public async Task<SystemSettingsDto?> UpdateSystemSettingsAsync(SystemSettingsDto settings, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.UpdateSettings(settings);
    }

    public async Task<TrackingTimelineDto?> GetTrackingAsync(Guid documentId, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.Tracking(documentId);
    }

    public async Task<DashboardDto> GetDashboardAsync(Guid? orgId = null, CancellationToken ct = default)
    {
        await Tick(ct);
        _ = orgId;
        return _store.Dashboard(RequireUserId());
    }

    public async Task<SouDashboardDto> GetSouDashboardAsync(CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.SouDashboard();
    }

    public async Task<ArchiveListResult> GetArchiveAsync(
        Guid? organizationId = null, string? documentTypeCode = null, string? eventKeyword = null,
        Guid? academicYearId = null, string? status = null, int page = 1, int pageSize = 50,
        CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.Archive(RequireUserId(), organizationId, documentTypeCode, eventKeyword, academicYearId, status, page, pageSize);
    }

    public async Task<ArchiveCategoryCountsDto> GetArchiveCategoryCountsAsync(
        Guid? organizationId = null, Guid? academicYearId = null, CancellationToken ct = default)
    {
        await Tick(ct);
        _ = academicYearId;
        return _store.ArchiveCounts(RequireUserId(), organizationId);
    }

    public async Task<IReadOnlyList<AcademicYearDto>> GetAcademicYearsAsync(CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.AcademicYears();
    }

    public async Task<IReadOnlyList<DocumentVersionDto>> GetDocumentVersionsAsync(Guid documentId, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.Versions(documentId);
    }

    public async Task<DocumentDownloadDto?> GetDocumentDownloadAsync(Guid documentId, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.Download(documentId);
    }

    public async Task<DocumentDownloadDto?> GetAttachmentDownloadAsync(Guid documentId, Guid attachmentId, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.DownloadAttachment(documentId, attachmentId);
    }

    public async Task<ReviewQueueResult> GetReviewQueueAsync(string tab = "All", int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.ReviewQueue(RequireUserId(), tab, page, pageSize);
    }

    public async Task<ReviewHistoryResult> GetMyReviewHistoryAsync(string tab = "All", int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.ReviewHistory(RequireUserId(), tab, page, pageSize);
    }

    public async Task<WorkflowHistoryDto?> GetWorkflowHistoryAsync(Guid documentId, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.WorkflowHistory(documentId);
    }

    public async Task<WorkflowDecisionResult> ApproveAsync(
        Guid documentId, string? comments, Stream signature, string signatureFileName, CancellationToken ct = default)
    {
        await Tick(ct);
        _ = signature;
        _ = signatureFileName;
        return _store.Decide(RequireUserId(), documentId, "approve", comments);
    }

    public async Task<WorkflowDecisionResult> RejectAsync(Guid documentId, string comments, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.Decide(RequireUserId(), documentId, "reject", comments);
    }

    public async Task<WorkflowDecisionResult> ReturnAsync(Guid documentId, string comments, CancellationToken ct = default)
    {
        await Tick(ct);
        return _store.Decide(RequireUserId(), documentId, "return", comments);
    }

    public async Task<NotificationListDto> GetNotificationsAsync(CancellationToken ct = default)
    {
        await Tick(ct);
        return _session.UserId is Guid id
            ? _store.Notifications(id)
            : new NotificationListDto(0, []);
    }

    public async Task MarkNotificationReadAsync(Guid id, CancellationToken ct = default)
    {
        await Tick(ct);
        if (_session.UserId is Guid uid)
            _store.MarkRead(uid, id);
    }

    public async Task MarkAllNotificationsReadAsync(CancellationToken ct = default)
    {
        await Tick(ct);
        if (_session.UserId is Guid uid)
            _store.MarkAllRead(uid);
    }

    private Guid RequireUserId() =>
        _session.UserId ?? throw new ApiException("Not signed in.", HttpStatusCode.Unauthorized);

    private static async Task Tick(CancellationToken ct) =>
        await Task.Delay(40, ct);

    private static async Task SimulateUpload(Stream content, IProgress<int>? progress, CancellationToken ct)
    {
        if (progress is not null)
        {
            for (var i = 20; i <= 100; i += 20)
            {
                await Task.Delay(30, ct);
                progress.Report(i);
            }
        }
        else
        {
            await Tick(ct);
        }

        if (content.CanSeek) content.Position = 0;
    }
}

public sealed class AuthException : Exception
{
    public AuthException(string message) : base(message) { }
}

public sealed class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public ApiException(string message, HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}
