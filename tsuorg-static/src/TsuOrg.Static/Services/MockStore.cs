using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TsuOrg.Frontend.Models;

namespace TsuOrg.Frontend.Services;

/// <summary>In-memory TSU-ORGDOCX data. No HTTP, no backend.</summary>
public sealed class MockStore
{
    private readonly object _gate = new();
    private readonly List<MockUser> _users = [];
    private readonly List<MockOrg> _orgs = [];
    private readonly List<DocumentTypeDto> _types = [];
    private readonly List<MockDoc> _docs = [];
    private readonly List<MockNotification> _notifications = [];
    private readonly List<MockAudit> _audit = [];
    private readonly List<MockReviewHistory> _history = [];
    private readonly List<AcademicYearDto> _years = [];
    private SystemSettingsDto _settings = new(true, true, true, true, true, 75, "All");
    private int _seq = 200;

    public MockStore() => Seed();

    internal MockUser? GetUser(Guid? id) =>
        id is null ? null : _users.FirstOrDefault(u => u.Id == id);

    public LoginResponse Login(string email, string password)
    {
        var user = _users.FirstOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        if (user is null || user.Password != password)
            throw new AuthException("Invalid email or password.");

        return new LoginResponse(
            MockJwt.AccessToken(user),
            Guid.NewGuid().ToString("N"),
            user.Email,
            user.FullName,
            user.Role,
            user.Id,
            user.College);
    }

    public UserProfile? Profile(Guid userId)
    {
        var u = GetUser(userId);
        return u is null
            ? null
            : new UserProfile(u.Id, u.Email, u.FullName, u.College, u.Role,
                string.IsNullOrWhiteSpace(u.AvatarDataUrl) ? null : "mock-avatar");
    }

    public UserProfile UpdateProfile(Guid userId, string fullName)
    {
        var u = GetUser(userId) ?? throw new ApiException("Not signed in.", HttpStatusCode.Unauthorized);
        u.FullName = fullName.Trim();
        return Profile(userId)!;
    }

    public UserProfile UploadAvatar(Guid userId, string dataUrl)
    {
        var u = GetUser(userId) ?? throw new ApiException("Not signed in.", HttpStatusCode.Unauthorized);
        u.AvatarDataUrl = dataUrl;
        return Profile(userId)!;
    }

    public string? AvatarDataUrl(Guid userId) => GetUser(userId)?.AvatarDataUrl;

    public void ChangePassword(Guid userId, string current, string next)
    {
        var u = GetUser(userId) ?? throw new ApiException("Not signed in.", HttpStatusCode.Unauthorized);
        if (u.Password != current)
            throw new ApiException("Current password is incorrect.", HttpStatusCode.BadRequest);
        u.Password = next;
    }

    public IReadOnlyList<DocumentTypeDto> DocumentTypes() => _types;

    public IReadOnlyList<OrganizationDto> MyOrganizations(Guid userId)
    {
        var u = GetUser(userId);
        if (u is null) return [];
        IEnumerable<MockOrg> rows = u.OrgId is Guid oid
            ? _orgs.Where(o => o.Id == oid)
            : u.Role is "SouStaff" or "SystemAdmin" or "Dean" ? _orgs : [];
        return rows.Select(ToOrgDto).ToList();
    }

    public IReadOnlyList<OrganizationDto> Organizations() => _orgs.Select(ToOrgDto).ToList();

    public AcademicYearDto? CurrentYear() => _years.FirstOrDefault(y => y.IsCurrent);

    public IReadOnlyList<AcademicYearDto> AcademicYears() => _years;

    public CreateDocumentResult CreateDocument(Guid userId, CreateDocumentRequest request)
    {
        var u = GetUser(userId) ?? throw new ApiException("Not signed in.", HttpStatusCode.Unauthorized);
        var type = _types.FirstOrDefault(t => t.Id == request.DocumentTypeId)
                   ?? throw new ApiException("Unknown document type.", HttpStatusCode.BadRequest);
        var org = _orgs.FirstOrDefault(o => o.Id == request.OrganizationId)
                  ?? throw new ApiException("Unknown organization.", HttpStatusCode.BadRequest);

        lock (_gate)
        {
            _seq++;
            var number = $"{type.Code}-2026-{_seq:0000}";
            var doc = new MockDoc
            {
                Id = Guid.NewGuid(),
                Number = number,
                Title = string.IsNullOrWhiteSpace(request.Title) ? type.Name : request.Title.Trim(),
                TypeId = type.Id,
                TypeName = type.Name,
                TypeCode = type.Code,
                OrgId = org.Id,
                Status = "Draft",
                Stage = "Officer",
                SubmittedAt = DateTimeOffset.Now,
                CreatedAt = DateTimeOffset.Now,
                SubmittedBy = u.FullName,
                SubmittedByPosition = u.Position,
                AdviserName = org.AdviserName,
            };
            _docs.Add(doc);
            AddEvent(doc, "Draft created", "Officer started a new submission.", "Officer", "Draft", u);
            return new CreateDocumentResult(doc.Id, doc.Number);
        }
    }

    public UploadFileResult UploadPrimary(Guid documentId, string fileName, string contentType)
    {
        lock (_gate)
        {
            var doc = RequireDoc(documentId);
            doc.PrimaryFile = fileName;
            doc.PrimaryContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
            return new UploadFileResult($"mock/{documentId}/{fileName}", fileName);
        }
    }

    public UploadFileResult UploadAttachment(Guid documentId, string attachmentType, string fileName, string contentType)
    {
        lock (_gate)
        {
            var doc = RequireDoc(documentId);
            var existing = doc.Attachments.FirstOrDefault(a =>
                string.Equals(a.Type, attachmentType, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                existing.FileName = fileName;
                existing.ContentType = contentType;
            }
            else
            {
                doc.Attachments.Add(new MockAttachment
                {
                    Id = Guid.NewGuid(),
                    Type = attachmentType,
                    FileName = fileName,
                    ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/pdf" : contentType,
                });
            }
            return new UploadFileResult($"mock/{documentId}/{fileName}", fileName);
        }
    }

    public SubmitDocumentResult Submit(Guid userId, Guid documentId)
    {
        var u = GetUser(userId);
        lock (_gate)
        {
            var doc = RequireDoc(documentId);
            doc.Status = "Submitted";
            doc.Stage = "Calsv";
            doc.SubmittedAt = DateTimeOffset.Now;
            AddEvent(doc, "Submitted for OCR / CALSV", "Queued for Tesseract + LayoutLMv3.", "Calsv", "Submitted", u);
            AddAudit(doc, "Submitted", "Submit", "Submitted", u?.FullName ?? "Officer", u?.Role ?? "OrgOfficer");
            ApplyMockValidation(doc);
            return new SubmitDocumentResult(doc.Id, doc.Number, doc.Status, "Submitted for AI processing.");
        }
    }

    public ConfirmSubmissionResult Confirm(Guid userId, Guid documentId)
    {
        var u = GetUser(userId);
        lock (_gate)
        {
            var doc = RequireDoc(documentId);
            doc.Locked = true;
            doc.LockedAt = DateTimeOffset.Now;
            doc.Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(doc.Id + doc.Number + doc.Title))).ToLowerInvariant();
            if (doc.Validation is null) ApplyMockValidation(doc);
            if (doc.Status is "Draft" or "Submitted")
            {
                doc.Status = doc.Flagged ? "UnderReview" : "UnderReview";
                doc.Stage = "Adviser";
                doc.AssignedTo = doc.AdviserName;
            }
            AddEvent(doc, "Submission confirmed", "Metadata locked. SHA-256 generated.", "Officer", "Submitted", u);
            AddAudit(doc, "Confirmed", "Confirm", "Confirmed", u?.FullName ?? "Officer", RoleLabel(u?.Role));
            return new ConfirmSubmissionResult(
                doc.Id, doc.Number, doc.Status, doc.Stage, true, doc.Hash!, doc.LockedAt.Value,
                doc.Flagged ? "Flagged" : "Passed",
                "Submission confirmed. Routed to the next review stage.");
        }
    }

    public DocumentDetailDto? GetDocument(Guid id)
    {
        var doc = _docs.FirstOrDefault(d => d.Id == id);
        return doc is null ? null : ToDetail(doc);
    }

    public PagedDocumentsDto ListDocuments(Guid userId, int page, int pageSize)
    {
        var items = Visible(userId).OrderByDescending(d => d.CreatedAt).ToList();
        var pageItems = Paginate(items, page, pageSize);
        return new PagedDocumentsDto(
            pageItems.Select(d => new DocumentSummaryDto(
                d.Id, d.Number, d.Title, OrgName(d.OrgId), d.TypeCode, d.Status, d.Stage, d.CreatedAt, d.SubmittedAt)).ToList(),
            items.Count, page, pageSize);
    }

    public ValidationDetailDto? GetValidation(Guid documentId)
    {
        var doc = _docs.FirstOrDefault(d => d.Id == documentId);
        var v = doc?.Validation;
        if (v is null) return null;
        return new ValidationDetailDto(
            v.Id, v.DocumentClass, v.Confidence, v.RequiresHumanReview, v.ModelVersion, v.ProcessedAt,
            v.Fields, null, null, null, v.OcrFullText, v.OcrAvg, v.OcrFullText?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            v.KeyFields,
            v.Flags.Select(f => new OcrLowConfidenceFlagDto(f.Token, f.Confidence, f.Flag)).ToList(),
            "{}", v.ScorePercent,
            v.RequiresHumanReview ? "Flagged for human review" : "Approved for review",
            v.Extracted.Select(e => new ExtractedFieldDto(e.Label, e.Value)).ToList(),
            v.Checks.Select(c => new ComplianceCheckDto(c.Label, c.Passed)).ToList(),
            v.OcrCaption, v.OcrScanned);
    }

    public TrackerListResult Tracker(Guid userId, string tab, int page, int pageSize, string? search, string? college, Guid? organizationId)
    {
        var scoped = FilterDocs(Visible(userId), search, college, organizationId);
        var tabs = PipelineTabs(scoped);
        var filtered = tab is "All" or "" ? scoped : scoped.Where(d => PipelineTab(d) == tab);
        var items = Paginate(filtered.OrderByDescending(d => d.CreatedAt), page, pageSize);
        return new TrackerListResult(items.Select(ToTrackerCard).ToList(), filtered.Count(), page, pageSize, tabs);
    }

    public SouTrackerResult SouTracker(string tab, int page, int pageSize, string? search, string? college, Guid? organizationId, string band)
    {
        var scoped = FilterDocs(_docs, search, college, organizationId).Where(d => d.Status != "Draft");
        scoped = band switch
        {
            "high" => scoped.Where(d => (d.Score ?? 0) >= 80),
            "mid" => scoped.Where(d => (d.Score ?? 0) is >= 60 and < 80),
            "risk" => scoped.Where(d => (d.Score ?? 100) < 60),
            _ => scoped,
        };
        var tabs = PipelineTabs(scoped);
        var filtered = tab is "All" or "" ? scoped : scoped.Where(d => PipelineTab(d) == tab);
        var items = Paginate(filtered.OrderByDescending(d => d.SubmittedAt), page, pageSize);
        var colleges = _orgs.Select(o => o.College).Distinct().Select(c => new CollegeChipDto(c, CollegeName(c))).ToList();
        var orgs = _orgs.Select(o => new OrgComplianceDto(o.Id, o.Name, o.Acronym, o.College, o.Submissions, o.Approved, o.Compliance)).ToList();
        return new SouTrackerResult(items.Select(ToSouTracker).ToList(), filtered.Count(), page, pageSize, tabs, colleges, orgs);
    }

    public WorkflowMonitorResult Monitor(string tab, int page, int pageSize)
    {
        var active = _docs.Where(d => d.Status is "Submitted" or "UnderReview" or "Flagged").ToList();
        var flagged = active.Where(d => d.Flagged).ToList();
        var stale = active.Where(d => DaysElapsed(d) > 5).ToList();
        var source = tab switch
        {
            "Flagged" => flagged,
            "Stale" => stale,
            _ => active,
        };
        var items = Paginate(source.OrderByDescending(d => d.SubmittedAt), page, pageSize);
        return new WorkflowMonitorResult(
            items.Select(d => new WorkflowMonitorItemDto(
                d.Id, d.Number, d.Title, OrgName(d.OrgId), StageLabel(d), d.AssignedTo, d.SubmittedAt,
                d.Events.Count > 0 ? d.Events.Max(e => e.At) : d.SubmittedAt,
                DaysElapsed(d), d.Score, d.Flagged ? 1 : 0,
                d.Flagged ? "Flagged" : DaysElapsed(d) > 5 ? "Stale" : "Active")).ToList(),
            source.Count, page, pageSize, active.Count, flagged.Count, stale.Count);
    }

    public OrganizationsAdminResult OrganizationsAdmin(string tab)
    {
        var active = _orgs.Where(o => o.Status == "Active").ToList();
        var inactive = _orgs.Where(o => o.Status != "Active").ToList();
        var source = tab == "Inactive" ? inactive : active;
        return new OrganizationsAdminResult(
            source.Select(o => new OrganizationAdminDto(
                o.Id, o.Name, o.Acronym, o.College, o.AdviserName, o.PresidentName, o.Health,
                o.Submissions, o.Approved, o.Returned, o.Compliance)).ToList(),
            active.Count, inactive.Count);
    }

    public SystemReportsDto Reports() => new(
        [
            new MonthlySubmissionsDto("Apr", 2026, 4, 4),
            new MonthlySubmissionsDto("May", 2026, 5, 6),
            new MonthlySubmissionsDto("Jun", 2026, 6, 5),
            new MonthlySubmissionsDto("Jul", 2026, 7, 9),
            new MonthlySubmissionsDto("Aug", 2026, 8, 8),
        ],
        new ProcessingTimeDto(2.4, 4.1, 1.8, 5.2),
        42, 86, "2026-2027");

    public (string FileName, string ContentType, byte[] Bytes) DownloadReport(string type)
    {
        var csv = type.ToLowerInvariant() switch
        {
            "processing" => "Metric,Value\nOCR minutes,2.4\nAI minutes,4.1\nAdviser days,1.8\nFull cycle days,5.2\n",
            "validations" => "TotalValidated,SuccessRate\n42,86\n",
            _ => "Month,Year,Count\nApr,2026,4\nMay,2026,6\nJun,2026,5\nJul,2026,9\nAug,2026,8\n",
        };
        return ($"{type}.csv", "text/csv", Encoding.UTF8.GetBytes(csv));
    }

    public SystemAuditResult SystemAudit(int page, int pageSize)
    {
        var items = _audit.OrderByDescending(a => a.At).ToList();
        var pageItems = Paginate(items, page, pageSize);
        return new SystemAuditResult(
            pageItems.Select(a => new SystemAuditItemDto(a.DocumentId, a.Number, a.Title, a.TypeCode, a.Action, a.By, a.ByRole, a.At)).ToList(),
            items.Count, page, pageSize);
    }

    public ScopedAuditResult ScopedAudit(Guid userId, int page, int pageSize)
    {
        var u = GetUser(userId);
        var visibleIds = Visible(userId).Select(d => d.Id).ToHashSet();
        var items = _audit.Where(a => visibleIds.Contains(a.DocumentId)).OrderByDescending(a => a.At).ToList();
        if (u?.Role is "Adviser" or "Dean")
            items = items.Where(a => a.By == u.FullName || a.Kind is "AI" or "OCR" or "Submit" or "Confirm").ToList();
        var pageItems = Paginate(items, page, pageSize);
        return new ScopedAuditResult(
            pageItems.Select(a => new ScopedAuditItemDto(a.DocumentId, a.Number, a.Title, a.TypeCode, a.TypeName, a.Activity, a.Kind, a.At)).ToList(),
            items.Count, page, pageSize);
    }

    public SystemSettingsDto Settings() => _settings;

    public SystemSettingsDto UpdateSettings(SystemSettingsDto settings)
    {
        _settings = settings;
        return _settings;
    }

    public TrackingTimelineDto? Tracking(Guid documentId)
    {
        var d = _docs.FirstOrDefault(x => x.Id == documentId);
        if (d is null) return null;
        var steps = BuildSteps(d);
        var idx = CurrentIndex(d);
        var (overall, tone) = Overall(d);
        return new TrackingTimelineDto(
            d.Id, d.Number, d.Title, OrgName(d.OrgId), d.TypeCode, d.TypeName, d.SubmittedBy,
            d.SubmittedByPosition, d.AdviserName, d.Score, d.Status, d.Stage, idx, overall, tone, steps,
            d.Events.OrderByDescending(e => e.At).Select(e => new TrackingEventDto(
                e.Id, e.Status, e.Stage, e.Message, e.ActorUserId, e.At, e.Headline, e.Detail)).ToList());
    }

    public DashboardDto Dashboard(Guid userId)
    {
        var u = GetUser(userId);
        var docs = Visible(userId).Where(d => d.Status != "Draft").ToList();
        var submitted = docs.Count;
        var under = docs.Count(d => d.Status is "UnderReview" or "Submitted");
        var approved = docs.Count(d => d.Status is "Approved" or "Archived");
        var returned = docs.Count(d => d.Status is "Returned" or "Rejected");
        var flagged = docs.Count(d => d.Flagged);
        var withScore = docs.Where(d => d.Score is not null).ToList();
        var compliance = withScore.Count == 0 ? 0 : (int)Math.Round(withScore.Average(d => d.Score!.Value));
        var thisMonth = docs.Count(d => d.SubmittedAt.Month == 8 && d.SubmittedAt.Year == 2026);
        var recent = docs.OrderByDescending(d => d.SubmittedAt).Take(5)
            .Select(d => new RecentDocDto(d.Id, d.Number, d.Title, d.TypeName, d.Status, StageLabel(d), d.SubmittedAt, d.Score)).ToList();
        var reviewed = _history.Where(h => u is not null && h.ActorUserId == u.Id)
            .OrderByDescending(h => h.At).Take(5)
            .Select(h => new ReviewedDocDto(h.DocumentId, h.Number, h.Title, h.Org, h.Action, h.At, h.Score)).ToList();
        var byStage = docs.GroupBy(d => StageLabel(d)).Select(g => new StageCountDto(g.Key, g.Count())).ToList();
        var byType = docs.GroupBy(d => d.TypeCode).Select(g =>
        {
            var name = g.First().TypeName;
            return new TypeCountDto(g.Key, name, g.Count());
        }).ToList();
        var orgs = _orgs.Where(o => docs.Any(d => d.OrgId == o.Id) || u?.Role is "SouStaff" or "SystemAdmin")
            .Select(o => new OrgComplianceDto(o.Id, o.Name, o.Acronym, o.College, o.Submissions, o.Approved, o.Compliance)).ToList();
        var pending = u?.Role is "Adviser" or "Dean"
            ? docs.Count(d => d.Status == "UnderReview" && d.Stage == (u.Role == "Dean" ? "Dean" : "Adviser"))
            : under;
        return new DashboardDto(
            submitted, under, approved, returned, under, compliance, flagged,
            pending, approved, returned, submitted, byStage, byType, recent, reviewed,
            thisMonth, orgs.Count, orgs);
    }

    public SouDashboardDto SouDashboard()
    {
        var docs = _docs.Where(d => d.Status != "Draft").ToList();
        var approved = docs.Count(d => d.Status is "Approved" or "Archived");
        var under = docs.Count(d => d.Status is "UnderReview" or "Submitted");
        var returned = docs.Count(d => d.Status is "Returned" or "Rejected");
        var total = Math.Max(docs.Count, 1);
        var byType = docs.GroupBy(d => d.TypeCode)
            .Select(g => new TypeCountDto(g.Key, g.First().TypeName, g.Count())).ToList();
        var recent = docs.Where(d => d.Validation is not null).OrderByDescending(d => d.SubmittedAt).Take(6)
            .Select(d => new RecentValidationDto(d.Id, d.Number, d.Title, OrgName(d.OrgId), d.TypeName,
                d.Validation!.DocumentClass, d.Score ?? d.Validation.ScorePercent, d.Validation.ProcessedAt)).ToList();
        var orgs = _orgs.Select(o => new OrgComplianceDto(o.Id, o.Name, o.Acronym, o.College, o.Submissions, o.Approved, o.Compliance)).ToList();
        return new SouDashboardDto(
            docs.Count, docs.Count(d => d.SubmittedAt.Month == 8 && d.SubmittedAt.Year == 2026),
            (int)Math.Round(100.0 * approved / total), _orgs.Count(o => o.Status == "Active"),
            byType,
            new SouStatusBreakdownDto(approved, under, returned,
                (int)Math.Round(100.0 * approved / total),
                (int)Math.Round(100.0 * under / total),
                (int)Math.Round(100.0 * returned / total)),
            orgs, recent);
    }

    public ArchiveListResult Archive(Guid userId, Guid? organizationId, string? typeCode, string? keyword, Guid? yearId, string? status, int page, int pageSize)
    {
        var docs = Visible(userId).Where(d => d.Archived || d.Status is "Approved");
        if (organizationId is Guid oid) docs = docs.Where(d => d.OrgId == oid);
        if (!string.IsNullOrWhiteSpace(typeCode)) docs = docs.Where(d => d.TypeCode == typeCode);
        if (!string.IsNullOrWhiteSpace(keyword))
            docs = docs.Where(d => d.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) || d.Number.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(status)) docs = docs.Where(d => d.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        var year = CurrentYear();
        var list = docs.OrderByDescending(d => d.CreatedAt).ToList();
        var items = Paginate(list, page, pageSize);
        var ayId = year?.Id ?? Guid.Empty;
        return new ArchiveListResult(
            items.Select(d => new ArchiveDocumentDto(
                d.Id, d.Number, d.Title,
                new ArchiveOrganizationDto(d.OrgId, OrgName(d.OrgId), _orgs.First(o => o.Id == d.OrgId).Acronym),
                new ArchiveDocumentTypeDto(d.TypeCode, d.TypeName),
                year?.Label, ayId, d.Status, d.PrimaryFile, d.PrimaryFile is not null, d.CreatedAt, d.SubmittedAt,
                Math.Max(1, d.Versions.Count))).ToList(),
            list.Count, page, pageSize);
    }

    public ArchiveCategoryCountsDto ArchiveCounts(Guid userId, Guid? organizationId)
    {
        var docs = Visible(userId).Where(d => d.Archived || d.Status is "Approved");
        if (organizationId is Guid oid) docs = docs.Where(d => d.OrgId == oid);
        var list = docs.ToList();
        var byType = list.GroupBy(d => d.TypeCode)
            .Select(g => new ArchiveTypeCountDto(g.Key, g.First().TypeName, g.Count())).ToList();
        var byStatus = list.GroupBy(d => d.Status)
            .Select(g => new ArchiveStatusCountDto(g.Key, g.Count())).ToList();
        return new ArchiveCategoryCountsDto(byType, byStatus);
    }

    public IReadOnlyList<DocumentVersionDto> Versions(Guid documentId)
    {
        var doc = _docs.FirstOrDefault(d => d.Id == documentId);
        if (doc is null) return [];
        var rows = doc.Versions.Count > 0
            ? doc.Versions
            : [new MockVersion { VersionNumber = 1, FileName = doc.PrimaryFile ?? doc.Number + ".pdf", ChangeSummary = "Current package", CreatedAt = doc.CreatedAt }];
        return rows.OrderByDescending(v => v.VersionNumber)
            .Select(v => new DocumentVersionDto(v.Id, v.VersionNumber, v.FileName, v.ChangeSummary, v.CreatedAt)).ToList();
    }

    public DocumentDownloadDto? Download(Guid documentId)
    {
        var doc = _docs.FirstOrDefault(d => d.Id == documentId);
        if (doc is null) return null;
        var name = doc.PrimaryFile ?? $"{doc.Number}.txt";
        var body = $"TSU-ORGDOCX mock file\n{doc.Number}\n{doc.Title}\n{doc.TypeName}";
        var url = "data:text/plain;charset=utf-8," + Uri.EscapeDataString(body);
        return new DocumentDownloadDto(name, url, "text/plain");
    }

    public DocumentDownloadDto? DownloadAttachment(Guid documentId, Guid attachmentId)
    {
        var doc = _docs.FirstOrDefault(d => d.Id == documentId);
        var att = doc?.Attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (att is null) return null;
        var body = $"Mock attachment {att.Type}\n{att.FileName}";
        return new DocumentDownloadDto(att.FileName, "data:text/plain;charset=utf-8," + Uri.EscapeDataString(body), att.ContentType);
    }

    public ReviewQueueResult ReviewQueue(Guid userId, string tab, int page, int pageSize)
    {
        var u = GetUser(userId);
        var stage = u?.Role == "Dean" ? "Dean" : "Adviser";
        var all = Visible(userId).Where(d => d.Status == "UnderReview" && d.Stage == stage).ToList();
        var flagged = all.Where(d => d.Flagged).ToList();
        var clean = all.Where(d => !d.Flagged).ToList();
        var source = tab switch { "Flagged" => flagged, "Cleaned" => clean, _ => all };
        var items = Paginate(source.OrderByDescending(d => d.SubmittedAt), page, pageSize);
        return new ReviewQueueResult(
            items.Select(d => new ReviewQueueItemDto(
                d.Id, d.Number, d.Title, OrgName(d.OrgId), d.TypeCode, d.TypeName, d.Status, d.Stage,
                d.SubmittedAt, d.SubmittedBy, d.Validation?.DocumentClass,
                d.Validation?.Confidence, d.Flagged)).ToList(),
            source.Count, page, pageSize, all.Count, flagged.Count, clean.Count);
    }

    public ReviewHistoryResult ReviewHistory(Guid userId, string tab, int page, int pageSize)
    {
        var u = GetUser(userId);
        var all = _history.Where(h => u is not null && h.ActorUserId == u.Id).OrderByDescending(h => h.At).ToList();
        var approved = all.Where(h => h.Action is "Approve" or "Approved").ToList();
        var returned = all.Where(h => h.Action is "Return" or "Returned" or "Reject" or "Rejected").ToList();
        var source = tab switch { "Approved" => approved, "Returned" => returned, _ => all };
        var items = Paginate(source, page, pageSize);
        return new ReviewHistoryResult(
            items.Select(h => new ReviewHistoryItemDto(h.DocumentId, h.Number, h.Title, h.Org, h.TypeCode, h.Action, h.Comments, h.At, h.Score)).ToList(),
            source.Count, page, pageSize, all.Count, approved.Count, returned.Count);
    }

    public WorkflowHistoryDto? WorkflowHistory(Guid documentId)
    {
        var d = _docs.FirstOrDefault(x => x.Id == documentId);
        if (d is null) return null;
        var items = d.Events.Where(e => e.Action is not null)
            .OrderBy(e => e.At)
            .Select(e => new WorkflowHistoryItemDto(e.Id, e.ActorRole ?? "", e.ActorName ?? "", e.Action!, e.Comments, null, e.At))
            .ToList();
        return new WorkflowHistoryDto(d.Id, d.Number, d.Stage, d.Status is "Approved" or "Archived", items);
    }

    public WorkflowDecisionResult Decide(Guid userId, Guid documentId, string action, string? comments)
    {
        var u = GetUser(userId) ?? throw new ApiException("Not signed in.", HttpStatusCode.Unauthorized);
        lock (_gate)
        {
            var doc = RequireDoc(documentId);
            var act = action.ToLowerInvariant();
            if (act == "approve")
            {
                if (u.Role == "Adviser") { doc.Stage = "Dean"; doc.Status = "UnderReview"; doc.AssignedTo = "Dean Demo"; }
                else if (u.Role == "Dean") { doc.Stage = "Sou"; doc.Status = "UnderReview"; doc.AssignedTo = "SOU Staff Demo"; }
                else { doc.Stage = "Sou"; doc.Status = "Approved"; doc.AssignedTo = null; doc.Archived = true; }
                AddEvent(doc, $"{u.Role} approved", comments ?? "Electronic signature applied.", doc.Stage, "Approved", u, "Approve", comments);
                _history.Insert(0, new MockReviewHistory
                {
                    DocumentId = doc.Id, Title = doc.Title, Number = doc.Number, Org = OrgName(doc.OrgId),
                    TypeCode = doc.TypeCode, Action = "Approve", Comments = comments, At = DateTimeOffset.Now,
                    Score = doc.Score, ActorUserId = u.Id,
                });
                AddAudit(doc, "Approved", "Approve", "Approved", u.FullName, RoleLabel(u.Role));
            }
            else if (act == "reject")
            {
                doc.Status = "Rejected";
                doc.AssignedTo = doc.SubmittedBy;
                AddEvent(doc, "Rejected", comments ?? "", doc.Stage, "Rejected", u, "Reject", comments);
                _history.Insert(0, History(doc, u, "Reject", comments));
                AddAudit(doc, "Rejected", "Reject", "Rejected", u.FullName, RoleLabel(u.Role));
            }
            else
            {
                doc.Status = "Returned";
                doc.Stage = "Adviser";
                doc.ReturnComment = comments;
                doc.AssignedTo = doc.SubmittedBy;
                AddEvent(doc, "Returned for revision", comments ?? "", "Adviser", "Returned", u, "Return", comments);
                _history.Insert(0, History(doc, u, "Return", comments));
                AddAudit(doc, "Returned", "Return", "Returned", u.FullName, RoleLabel(u.Role));
            }

            return new WorkflowDecisionResult(doc.Id, doc.Number, action, doc.Status, doc.Stage, DateTimeOffset.Now, null,
                $"{action} recorded.");
        }
    }

    public NotificationListDto Notifications(Guid userId)
    {
        var items = _notifications.Where(n => n.UserId == userId).OrderByDescending(n => n.At).ToList();
        return new NotificationListDto(
            items.Count(n => n.Unread),
            items.Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.RelatedId, n.At)).ToList());
    }

    public void MarkRead(Guid userId, Guid id)
    {
        var n = _notifications.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (n is not null) n.Unread = false;
    }

    public void MarkAllRead(Guid userId)
    {
        foreach (var n in _notifications.Where(x => x.UserId == userId))
            n.Unread = false;
    }

    // ── seed ──────────────────────────────────────────────────────────

    private void Seed()
    {
        _years.Add(new AcademicYearDto(MockIds.Of("ay-2025"), "2025-2026", new DateOnly(2025, 6, 1), new DateOnly(2026, 5, 31), false));
        _years.Add(new AcademicYearDto(MockIds.Of("ay-2026"), "2026-2027", new DateOnly(2026, 6, 1), new DateOnly(2027, 5, 31), true));

        _types.Add(Type("type-sf08", "SF08", "Activity Proposal (SF08)",
            "Request to Conduct an Activity — primary SOU activity proposal form with supporting attachments.",
            ("ActivityProposal", "Activity Proposal Form (PDF)", true, false),
            ("ProgramMatrix", "Program of Activities / Matrix", true, false),
            ("VenueApproval", "Venue Approval / Permit", true, false),
            ("EndorsementLetter", "Endorsement / Supporting Letter", true, false),
            ("ParentConsent", "Parent Consent Form", true, true)));
        _types.Add(Type("type-accomp", "ACCOMPLISHMENT", "Accomplishment Report",
            "Post-activity accomplishment report to be filed after the event.",
            ("AccomplishmentReport", "Accomplishment Report (PDF)", true, false),
            ("ActivityPhotos", "Activity Documentation / Photos", true, false),
            ("AttendanceSheet", "Attendance Sheet(s)", true, false),
            ("FinancialLiquidation", "Financial Liquidation Report", true, false),
            ("Certificates", "Certificates (if any)", false, false)));
        _types.Add(Type("type-accr", "ACCREDITATION", "Accreditation / Application Form",
            "Annual accreditation package for student organizations.",
            ("AccreditationForm", "Accreditation Application Form", true, false),
            ("Constitution", "Constitution and By-Laws", true, false),
            ("OfficerListDoc", "Formal List of Officers", true, false),
            ("MembershipList", "Membership List", true, false),
            ("AnnualPlan", "General Annual Plan of Activities", true, false)));

        _orgs.AddRange(
        [
            Org("org-csc", "Computer Science Society (CSC)", "CSC", "CICS", "Adviser Demo", "Officer Demo", "Active", "Active", 8, 5, 1, 88),
            Org("org-ace", "Association of Computing Engineers", "ACE", "CEA", "CEA Adviser", "CEA Officer", "Active", "Flagged", 6, 3, 2, 64),
            Org("org-jpia", "Junior Philippine Institute of Accountants", "JPIA", "CBA", "Prof. Elena Cruz", "Miguel Santos", "Active", "Active", 11, 9, 0, 94),
            Org("org-eg", "Educators' Guild", "EG", "COED", "Dr. Ana Reyes", "Liza Ramos", "Active", "Active", 4, 3, 1, 79),
            Org("org-tssp", "TSU Society of Student Photographers", "TSSP", "CAFA", "Prof. Carlo Diaz", "Ivy Tan", "Inactive", "Inactive", 2, 1, 1, 51),
        ]);

        _users.AddRange(
        [
            User("u-officer", "officer@student.tsu.edu.ph", "Officer Demo", "OrgOfficer", "CICS", "org-csc", "President"),
            User("u-adviser", "adviser@tsu.edu.ph", "Adviser Demo", "Adviser", "CICS", "org-csc", "Faculty Adviser"),
            User("u-dean", "dean@tsu.edu.ph", "Dean Demo", "Dean", "CICS", null, "College Dean"),
            User("u-sou", "sou@tsu.edu.ph", "SOU Staff Demo", "SouStaff", null, null, "SOU Administrator"),
            User("u-admin", "admin@tsu.edu.ph", "System Admin", "SystemAdmin", null, null, "System Administrator"),
        ]);

        AddDoc("doc-001", "SF08-2026-0142", "Leadership Training 2026", "type-sf08", "org-csc",
            "UnderReview", "Adviser", 91, false, "2026-08-12T09:20:00+08:00",
            "Officer Demo", "Adviser Demo", "Adviser Demo", "SF08_Leadership_Training.pdf",
            ("ProgramMatrix", "Program_Matrix.pdf"), ("VenueApproval", "Venue_Permit.pdf"), ("EndorsementLetter", "Endorsement_Letter.pdf"));
        AddDoc("doc-002", "AR-2026-0088", "Coding Week Accomplishment Report", "type-accomp", "org-csc",
            "UnderReview", "Dean", 86, false, "2026-08-08T14:10:00+08:00",
            "Officer Demo", "Adviser Demo", "Dean Demo", "AR_Coding_Week.pdf",
            ("ActivityPhotos", "CodingWeek_Photos.zip"), ("AttendanceSheet", "Attendance.xlsx"), ("FinancialLiquidation", "Liquidation.pdf"));
        AddDoc("doc-003", "ACC-2026-0004", "CSC Accreditation AY 2026-2027", "type-accr", "org-csc",
            "Approved", "Sou", 97, false, "2026-07-22T10:00:00+08:00",
            "Officer Demo", "Adviser Demo", null, "CSC_Accreditation.pdf",
            ("Constitution", "CSC_CBL.pdf"), ("OfficerListDoc", "Officers_AY2627.pdf"));
        var d3 = RequireDoc(MockIds.Of("doc-003"));
        d3.Archived = true;
        d3.Versions.AddRange(
        [
            new MockVersion { VersionNumber = 2, FileName = "CSC_Accreditation_v2.pdf", ChangeSummary = "Updated officer list for AY 2026-2027", CreatedAt = DateTimeOffset.Parse("2026-07-22T10:00:00+08:00") },
            new MockVersion { VersionNumber = 1, FileName = "CSC_Accreditation_v1.pdf", ChangeSummary = "Initial accreditation draft", CreatedAt = DateTimeOffset.Parse("2026-07-15T09:00:00+08:00") },
        ]);

        AddDoc("doc-004", "SF08-2026-0110", "Community Outreach — San Vicente", "type-sf08", "org-csc",
            "Returned", "Adviser", 58, true, "2026-08-02T16:40:00+08:00",
            "Officer Demo", "Adviser Demo", "Officer Demo", "SF08_Outreach.pdf",
            ("ProgramMatrix", "Outreach_Matrix.pdf"));
        RequireDoc(MockIds.Of("doc-004")).ReturnComment = "Venue permit is missing and activity date conflicts with midterm week. Please revise.";

        AddDoc("doc-005", "SF08-2026-0158", "Hackathon 2026 Proposal", "type-sf08", "org-csc",
            "Submitted", "Calsv", 82, false, "2026-08-18T11:05:00+08:00",
            "Officer Demo", "Adviser Demo", null, "SF08_Hackathon.pdf",
            ("ProgramMatrix", "Hackathon_Program.pdf"), ("VenueApproval", "AVR_Permit.pdf"), ("EndorsementLetter", "Dean_Endorsement.pdf"));

        AddDoc("doc-006", "SF08-2026-0131", "Engineering Week Kickoff", "type-sf08", "org-ace",
            "UnderReview", "Sou", 74, true, "2026-08-05T08:30:00+08:00",
            "CEA Officer", "CEA Adviser", "SOU Staff Demo", "SF08_EngWeek.pdf",
            ("ProgramMatrix", "EngWeek_Matrix.pdf"), ("VenueApproval", "Gym_Permit.pdf"));

        AddDoc("doc-007", "AR-2026-0071", "Tax Month Seminar Report", "type-accomp", "org-jpia",
            "Approved", "Sou", 93, false, "2026-07-30T13:00:00+08:00",
            "Miguel Santos", "Prof. Elena Cruz", null, "AR_TaxMonth.pdf");
        RequireDoc(MockIds.Of("doc-007")).Archived = true;

        AddDoc("doc-008", "SF08-2026-0160", "Literacy Outreach — Capas", "type-sf08", "org-eg",
            "UnderReview", "Adviser", 68, true, "2026-08-16T15:45:00+08:00",
            "Liza Ramos", "Dr. Ana Reyes", "Dr. Ana Reyes", "SF08_Literacy.pdf",
            ("ProgramMatrix", "Literacy_Program.pdf"));

        AddDoc("doc-009", "SF08-2026-0099", "Photo Walk — Campus Heritage", "type-sf08", "org-tssp",
            "Approved", "Sou", 81, false, "2026-06-18T09:00:00+08:00",
            "Ivy Tan", "Prof. Carlo Diaz", null, "SF08_PhotoWalk.pdf");
        RequireDoc(MockIds.Of("doc-009")).Archived = true;

        AddDoc("doc-010", "AR-2026-0094", "Freshmen Orientation Report", "type-accomp", "org-csc",
            "UnderReview", "Adviser", 89, false, "2026-08-14T10:22:00+08:00",
            "Officer Demo", "Adviser Demo", "Adviser Demo", "AR_Freshmen_Orientation.pdf",
            ("ActivityPhotos", "Orientation_Photos.zip"), ("AttendanceSheet", "Attendance_FO.pdf"));

        foreach (var doc in _docs)
            ApplyMockValidation(doc);

        SeedTimelinesAndAudit();
        SeedNotifications();
    }

    private void SeedTimelinesAndAudit()
    {
        Event("doc-001", "2026-08-12T09:20:00+08:00", "Submission confirmed", "Metadata locked. SHA-256 generated.", "Officer", "Submitted");
        Event("doc-001", "2026-08-12T09:22:00+08:00", "OCR scanning complete", "Tesseract v5 average confidence 94%.", "Calsv", "Submitted");
        Event("doc-001", "2026-08-12T09:24:00+08:00", "AI validation complete", "CALSV class: Valid Submission · 91%.", "Calsv", "AiValidated");
        Event("doc-001", "2026-08-12T09:28:00+08:00", "Routed to Adviser", "Package queued for Adviser Demo.", "Adviser", "UnderReview");

        Event("doc-002", "2026-08-08T14:10:00+08:00", "Submission confirmed", "Metadata locked.", "Officer", "Submitted");
        Event("doc-002", "2026-08-08T14:16:00+08:00", "AI validation complete", "CALSV class: Valid Submission · 86%.", "Calsv", "AiValidated");
        Event("doc-002", "2026-08-10T11:02:00+08:00", "Adviser approved", "Electronic signature applied.", "Adviser", "Approved", MockIds.Of("u-adviser"), "Adviser Demo", "Adviser", "Approve");
        Event("doc-002", "2026-08-10T11:05:00+08:00", "Routed to Dean", "Adviser Demo approved. Awaiting Dean Demo.", "Dean", "UnderReview");

        Event("doc-003", "2026-07-22T10:08:00+08:00", "Submission confirmed", "Accreditation package locked.", "Officer", "Submitted");
        Event("doc-003", "2026-07-24T14:12:00+08:00", "Adviser approved", "Endorsed to College Dean.", "Adviser", "Approved", MockIds.Of("u-adviser"), "Adviser Demo", "Adviser", "Approve");
        Event("doc-003", "2026-07-26T09:40:00+08:00", "Dean approved", "Forwarded to SOU.", "Dean", "Approved", MockIds.Of("u-dean"), "Dean Demo", "Dean", "Approve");
        Event("doc-003", "2026-07-28T16:00:00+08:00", "SOU approved", "Archived in organization repository.", "Sou", "Approved", MockIds.Of("u-sou"), "SOU Staff Demo", "SouStaff", "Approve");

        Event("doc-004", "2026-08-02T16:48:00+08:00", "Submission confirmed", "Metadata locked.", "Officer", "Submitted");
        Event("doc-004", "2026-08-02T16:50:00+08:00", "AI flagged for human review", "CALSV score 58%.", "Calsv", "Flagged");
        Event("doc-004", "2026-08-04T10:15:00+08:00", "Returned for revision", "Venue permit missing; date conflicts with midterms.", "Adviser", "Returned", MockIds.Of("u-adviser"), "Adviser Demo", "Adviser", "Return", "Venue permit is missing and activity date conflicts with midterm week.");

        _history.AddRange(
        [
            HistoryKey("doc-003", "Approve", "2026-07-24T14:12:00+08:00", "u-adviser"),
            HistoryKey("doc-004", "Return", "2026-08-04T10:15:00+08:00", "u-adviser"),
            HistoryKey("doc-002", "Approve", "2026-08-10T11:02:00+08:00", "u-adviser"),
            HistoryKey("doc-003", "Approve", "2026-07-26T09:40:00+08:00", "u-dean"),
        ]);

        AddAuditRaw("doc-005", "Submitted", "Submit", "Submitted", "Officer Demo", "Org Officer", "2026-08-18T11:12:00+08:00");
        AddAuditRaw("doc-001", "AI validation", "AI", "AI validated", "CALSV Engine", "System", "2026-08-12T09:24:00+08:00");
        AddAuditRaw("doc-001", "OCR scanning", "OCR", "OCR scanned", "CALSV Engine", "System", "2026-08-12T09:22:00+08:00");
        AddAuditRaw("doc-002", "Approved", "Approve", "Approved", "Adviser Demo", "Adviser", "2026-08-10T11:02:00+08:00");
        AddAuditRaw("doc-004", "Returned", "Return", "Returned", "Adviser Demo", "Adviser", "2026-08-04T10:15:00+08:00");
        AddAuditRaw("doc-003", "Approved", "Approve", "Approved", "SOU Staff Demo", "SOU Admin", "2026-07-28T16:00:00+08:00");
        AddAuditRaw("doc-003", "Confirmed", "Confirm", "Confirmed", "Officer Demo", "Org Officer", "2026-07-22T10:08:00+08:00");
        AddAuditRaw("doc-006", "AI flagged", "AI", "AI flagged", "CALSV Engine", "System", "2026-08-05T08:36:00+08:00");
        AddAuditRaw("doc-007", "Approved", "Approve", "Approved", "SOU Staff Demo", "SOU Admin", "2026-08-01T09:20:00+08:00");
        AddAuditRaw("doc-010", "Routed / confirmed", "Confirm", "Confirmed", "Officer Demo", "Org Officer", "2026-08-14T10:30:00+08:00");
    }

    private void SeedNotifications()
    {
        Note("u-officer", "Document returned", "SF08-2026-0110 was returned for revision.", "doc-004", "2026-08-04T10:16:00+08:00", true);
        Note("u-officer", "Adviser approved", "AR-2026-0088 moved to Dean review.", "doc-002", "2026-08-10T11:03:00+08:00", true);
        Note("u-officer", "Accreditation archived", "ACC-2026-0004 is now in the repository.", "doc-003", "2026-07-28T16:01:00+08:00", false);
        Note("u-adviser", "New review item", "Leadership Training 2026 awaits your decision.", "doc-001", "2026-08-12T09:28:00+08:00", true);
        Note("u-adviser", "New review item", "Freshmen Orientation Report is in your queue.", "doc-010", "2026-08-14T10:30:00+08:00", true);
        Note("u-dean", "Awaiting Dean review", "AR-2026-0088 was endorsed by the adviser.", "doc-002", "2026-08-10T11:05:00+08:00", true);
        Note("u-sou", "Stale document", "SF08-2026-0131 has been in SOU review for 14 days.", "doc-006", "2026-08-19T08:00:00+08:00", true);
        Note("u-sou", "Flagged package", "Engineering Week Kickoff requires human review.", "doc-006", "2026-08-05T08:36:00+08:00", true);
        Note("u-admin", "Stale document", "SF08-2026-0131 has been in SOU review for 14 days.", "doc-006", "2026-08-19T08:00:00+08:00", true);
    }

    // ── helpers ───────────────────────────────────────────────────────

    private IEnumerable<MockDoc> Visible(Guid userId)
    {
        var u = GetUser(userId);
        if (u is null) return [];
        return u.Role switch
        {
            "OrgOfficer" or "Adviser" => _docs.Where(d => d.OrgId == u.OrgId),
            "Dean" => _docs.Where(d => Org(d.OrgId)?.College == u.College),
            _ => _docs,
        };
    }

    private IEnumerable<MockDoc> FilterDocs(IEnumerable<MockDoc> docs, string? search, string? college, Guid? organizationId)
    {
        if (organizationId is Guid oid) docs = docs.Where(d => d.OrgId == oid);
        if (!string.IsNullOrWhiteSpace(college))
            docs = docs.Where(d => Org(d.OrgId)?.College == college);
        if (!string.IsNullOrWhiteSpace(search))
            docs = docs.Where(d =>
                d.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                || d.Number.Contains(search, StringComparison.OrdinalIgnoreCase)
                || OrgName(d.OrgId).Contains(search, StringComparison.OrdinalIgnoreCase));
        return docs;
    }

    private MockDoc RequireDoc(Guid id) =>
        _docs.FirstOrDefault(d => d.Id == id) ?? throw new ApiException("Document not found.", HttpStatusCode.NotFound);

    private MockOrg? Org(Guid id) => _orgs.FirstOrDefault(o => o.Id == id);
    private string OrgName(Guid id) => Org(id)?.Name ?? "Organization";

    private static OrganizationDto ToOrgDto(MockOrg o) =>
        new(o.Id, o.Name, o.Acronym, o.College, o.Status, "1st Semester");

    private DocumentDetailDto ToDetail(MockDoc d)
    {
        ValidationSummaryDto? summary = d.Validation is null ? null : new ValidationSummaryDto(
            d.Validation.DocumentClass, d.Validation.Confidence, d.Validation.RequiresHumanReview,
            d.Validation.ModelVersion, d.Validation.ProcessedAt, "{}");
        return new DocumentDetailDto(
            d.Id, d.Number, d.Title, OrgName(d.OrgId), d.TypeCode, d.TypeName, "2026-2027",
            d.SubmittedBy, d.Status, d.Stage, d.PrimaryFile, d.PrimaryContentType, d.CreatedAt, d.SubmittedAt,
            d.Locked, d.Hash, d.LockedAt,
            d.Attachments.Select(a => new AttachmentDto(a.Id, a.Type, a.FileName, a.ContentType)).ToList(),
            summary);
    }

    private TrackerDocumentCardDto ToTrackerCard(MockDoc d)
    {
        var (overall, tone) = Overall(d);
        return new TrackerDocumentCardDto(
            d.Id, d.Number, d.Title, OrgName(d.OrgId), d.TypeCode, d.Status, d.Stage,
            d.CreatedAt, d.SubmittedAt, CurrentIndex(d), overall, tone, BuildSteps(d), d.Score);
    }

    private SouTrackerDocumentDto ToSouTracker(MockDoc d) =>
        new(d.Id, d.Number, d.Title, OrgName(d.OrgId), Org(d.OrgId)?.College, StageLabel(d), d.Status, d.Stage,
            d.SubmittedAt, d.AssignedTo, d.Score, d.Flagged ? 1 : 0, DaysElapsed(d),
            d.Flagged ? "Flagged" : "Clean");

    private static IReadOnlyList<TrackerStepDto> BuildSteps(MockDoc d)
    {
        var labels = new[] { "Submitted", "AI validated", "Adviser review", "Dean review", "Approved" };
        var keys = new[] { "Submitted", "AiValidated", "AdviserReview", "DeanReview", "Approved" };
        var idx = CurrentIndex(d);
        var steps = new List<TrackerStepDto>();
        for (var i = 0; i < labels.Length; i++)
        {
            var state = i < idx ? "Completed" : i == idx ? (d.Status == "Returned" ? "Returned" : "In progress") : "Upcoming";
            var tone = state switch { "Completed" => "completed", "In progress" or "Returned" => "progress", _ => "" };
            steps.Add(new TrackerStepDto(keys[i], labels[i], state, tone, i <= idx ? d.SubmittedAt : null));
        }
        return steps;
    }

    private static int CurrentIndex(MockDoc d)
    {
        if (d.Status is "Approved" or "Archived") return 4;
        if (d.Status is "Returned" or "Rejected") return 2;
        return d.Stage switch
        {
            "Officer" => 0,
            "Calsv" => d.Status == "Submitted" ? 0 : 1,
            "Adviser" => 2,
            "Dean" => 3,
            "Sou" => 3,
            _ => 0,
        };
    }

    private static string PipelineTab(MockDoc d)
    {
        if (d.Status is "Returned" or "Rejected") return "Returned";
        if (d.Status is "Approved" or "Archived") return "Approved";
        return d.Stage switch
        {
            "Adviser" => "AdviserReview",
            "Dean" => "DeanReview",
            "Sou" => "SouReview",
            "Calsv" => d.Validation is null ? "Submitted" : "AiValidated",
            _ => "Submitted",
        };
    }

    private static IReadOnlyList<TrackerTabCountDto> PipelineTabs(IEnumerable<MockDoc> docs)
    {
        var list = docs.ToList();
        string[] keys = ["All", "Submitted", "AiValidated", "AdviserReview", "DeanReview", "Approved", "Returned"];
        return keys.Select(k => new TrackerTabCountDto(k, k == "All" ? list.Count : list.Count(d => PipelineTab(d) == k))).ToList();
    }

    private static (string Overall, string Tone) Overall(MockDoc d) => d.Status switch
    {
        "Returned" => ("Returned for revision", "returned"),
        "Rejected" => ("Rejected", "returned"),
        "Approved" or "Archived" => ("Approved", "approved"),
        "Submitted" => ("Submitted", "submitted"),
        _ => d.Stage switch
        {
            "Adviser" => ("Adviser review", "adviser"),
            "Dean" => ("Dean review", "dean"),
            "Sou" => ("SOU review", "review"),
            "Calsv" => ("AI validated", "review"),
            _ => ("Under review", "review"),
        },
    };

    private static string StageLabel(MockDoc d) => d.Status switch
    {
        "Returned" => "Returned",
        "Approved" or "Archived" => "Approved",
        _ => d.Stage switch
        {
            "Adviser" => "Adviser review",
            "Dean" => "Dean review",
            "Sou" => "SOU review",
            "Calsv" => "AI validated",
            _ => "Submitted",
        },
    };

    private static int DaysElapsed(MockDoc d) =>
        Math.Max(0, (int)(DateTimeOffset.Now - d.SubmittedAt).TotalDays);

    private static List<T> Paginate<T>(IEnumerable<T> items, int page, int pageSize) =>
        items.Skip(Math.Max(0, (page - 1) * pageSize)).Take(Math.Max(1, pageSize)).ToList();

    private void ApplyMockValidation(MockDoc doc)
    {
        var flagged = doc.Flagged || doc.Id == MockIds.Of("doc-004");
        var fields = flagged ? FlaggedFields(doc) : CleanFields(doc);
        doc.Validation = new MockValidation
        {
            DocumentClass = flagged ? "Requires Human Review" : "Valid Submission",
            Confidence = flagged ? 0.58m : Math.Clamp((doc.Score ?? 90) / 100m, 0.5m, 0.99m),
            ScorePercent = doc.Score ?? (flagged ? 58 : 90),
            RequiresHumanReview = flagged,
            OcrAvg = flagged ? 0.71m : 0.94m,
            OcrCaption = $"{doc.TypeName} — {OrgName(doc.OrgId)}.",
            OcrScanned = flagged ? "Low-confidence tokens on venue and date fields." : "Scanned from uploaded PDF. Fields mapped by CALSV.",
            OcrFullText = flagged
                ? $"ORGANIZATION: {OrgName(doc.OrgId)}\nACTIVITY TITLE: {doc.Title}\nDATE: August 28 2026\nVENUE: [ILLEGIBLE]\nADVISER SIGNATURE: not detected"
                : $"ORGANIZATION: {OrgName(doc.OrgId)}\nACTIVITY TITLE: {doc.Title}\nDATE: September 12, 2026\nVENUE: TSU AVR, Lucinda Campus\nEXPECTED PARTICIPANTS: 120\nOFFICER SIGNATURE: present\nADVISER SIGNATURE: present",
            KeyFields = flagged ? ["ActivityTitle", "ActivityDate"] : ["ActivityTitle", "ActivityDate", "Venue", "OfficerSignature"],
            Flags = flagged
                ? [("VENUE", 41, "ILLEGIBLE"), ("DATE", 62, "LOW_CONFIDENCE")]
                : [],
            Extracted = flagged
                ?
                [
                    ("Organization", OrgName(doc.OrgId)),
                    ("Activity Title", doc.Title),
                    ("Activity Date", "August 28, 2026"),
                    ("Venue", "(illegible)"),
                ]
                :
                [
                    ("Organization", OrgName(doc.OrgId)),
                    ("Activity Title", doc.Title),
                    ("Activity Date", "September 12, 2026"),
                    ("Venue", "TSU AVR, Lucinda Campus"),
                    ("Expected Participants", "120"),
                ],
            Checks = flagged
                ?
                [
                    ("Required fields complete", false),
                    ("Officer signature detected", true),
                    ("Adviser signature detected", false),
                    ("Attachments present", false),
                    ("Layout matches SF08 template", false),
                ]
                :
                [
                    ("Required fields complete", true),
                    ("Officer signature detected", true),
                    ("Adviser signature detected", true),
                    ("Attachments present", true),
                    ("Layout matches SF08 template", true),
                ],
            Fields = fields,
            ProcessedAt = doc.SubmittedAt.AddMinutes(4),
        };
        if (doc.Status == "Submitted" && doc.Stage == "Calsv" && doc.Validation is not null && _settings.AiValidationEnabled)
        {
            // keep Submitted/Calsv so tracker "Submitted" still has an item
        }
    }

    private static JsonElement CleanFields(MockDoc doc) => JsonSerializer.SerializeToElement(new object[]
    {
        new { name = "ActivityTitle", label = "PRESENT", matched_text = doc.Title, ocr_confidence = 96.0 },
        new { name = "ActivityDate", label = "PRESENT", matched_text = "September 12, 2026", ocr_confidence = 93.0 },
        new { name = "Venue", label = "PRESENT", matched_text = "TSU AVR, Lucinda Campus", ocr_confidence = 91.0 },
        new { name = "OfficerSignature", label = "SIGNED", matched_text = "present", ocr_confidence = 88.0 },
        new { name = "AdviserSignature", label = "SIGNED", matched_text = "present", ocr_confidence = 86.0 },
    });

    private static JsonElement FlaggedFields(MockDoc doc) => JsonSerializer.SerializeToElement(new object[]
    {
        new { name = "ActivityTitle", label = "PRESENT", matched_text = doc.Title, ocr_confidence = 81.0 },
        new { name = "ActivityDate", label = "PRESENT", matched_text = "August 28, 2026", ocr_confidence = 62.0 },
        new { name = "Venue", label = "MISSING", matched_text = (string?)null, ocr_confidence = 41.0 },
        new { name = "OfficerSignature", label = "SIGNED", matched_text = "present", ocr_confidence = 80.0 },
        new { name = "AdviserSignature", label = "UNSIGNED", matched_text = (string?)null, ocr_confidence = 20.0 },
    });

    private DocumentTypeDto Type(string key, string code, string name, string description, params (string Key, string Display, bool Mandatory, bool Conditional)[] reqs) =>
        new(MockIds.Of(key), code, name, description,
            reqs.Select(r => new RequirementDto(MockIds.Of(key + "-" + r.Key), r.Key, r.Display, true, r.Mandatory, r.Conditional, null)).ToList());

    private static MockOrg Org(string key, string name, string acronym, string college, string adviser, string president, string status, string health, int sub, int appr, int ret, int comp) =>
        new()
        {
            Id = MockIds.Of(key), Name = name, Acronym = acronym, College = college,
            AdviserName = adviser, PresidentName = president, Status = status, Health = health,
            Submissions = sub, Approved = appr, Returned = ret, Compliance = comp,
        };

    private static MockUser User(string key, string email, string name, string role, string? college, string? orgKey, string position) =>
        new()
        {
            Id = MockIds.Of(key), Email = email, Password = "TsuOrg@2026", FullName = name,
            Role = role, College = college, OrgId = orgKey is null ? null : MockIds.Of(orgKey), Position = position,
        };

    private void AddDoc(string key, string number, string title, string typeKey, string orgKey,
        string status, string stage, int score, bool flagged, string submittedAt,
        string submittedBy, string adviser, string? assigned, string primary,
        params (string Type, string File)[] attachments)
    {
        var type = _types.First(t => t.Id == MockIds.Of(typeKey));
        var at = DateTimeOffset.Parse(submittedAt);
        var doc = new MockDoc
        {
            Id = MockIds.Of(key), Number = number, Title = title, TypeId = type.Id, TypeName = type.Name,
            TypeCode = type.Code, OrgId = MockIds.Of(orgKey), Status = status, Stage = stage, Score = score,
            Flagged = flagged, SubmittedAt = at, CreatedAt = at, SubmittedBy = submittedBy,
            SubmittedByPosition = "President", AdviserName = adviser, AssignedTo = assigned,
            Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key + number))).ToLowerInvariant(),
            Locked = status != "Draft", LockedAt = status == "Draft" ? null : at.AddMinutes(8),
            PrimaryFile = primary,
        };
        foreach (var (typeName, file) in attachments)
            doc.Attachments.Add(new MockAttachment { Id = MockIds.Of(key + "-" + typeName), Type = typeName, FileName = file });
        _docs.Add(doc);
    }

    private void Event(string docKey, string at, string headline, string detail, string stage, string status,
        Guid? actorId = null, string? actorName = null, string? actorRole = null, string? action = null, string? comments = null)
    {
        var doc = RequireDoc(MockIds.Of(docKey));
        doc.Events.Add(new MockEvent
        {
            At = DateTimeOffset.Parse(at), Headline = headline, Detail = detail, Stage = stage, Status = status,
            Message = headline, ActorUserId = actorId, ActorName = actorName, ActorRole = actorRole, Action = action, Comments = comments,
        });
    }

    private void AddEvent(MockDoc doc, string headline, string detail, string stage, string status, MockUser? user, string? action = null, string? comments = null)
    {
        doc.Events.Insert(0, new MockEvent
        {
            At = DateTimeOffset.Now, Headline = headline, Detail = detail, Stage = stage, Status = status,
            Message = headline, ActorUserId = user?.Id, ActorName = user?.FullName, ActorRole = user?.Role,
            Action = action, Comments = comments,
        });
    }

    private void AddAudit(MockDoc doc, string activity, string kind, string action, string by, string byRole) =>
        _audit.Insert(0, new MockAudit
        {
            DocumentId = doc.Id, Number = doc.Number, Title = doc.Title, TypeCode = doc.TypeCode, TypeName = doc.TypeName,
            Activity = activity, Kind = kind, Action = action, By = by, ByRole = byRole, At = DateTimeOffset.Now,
        });

    private void AddAuditRaw(string docKey, string activity, string kind, string action, string by, string byRole, string at)
    {
        var doc = RequireDoc(MockIds.Of(docKey));
        _audit.Add(new MockAudit
        {
            DocumentId = doc.Id, Number = doc.Number, Title = doc.Title, TypeCode = doc.TypeCode, TypeName = doc.TypeName,
            Activity = activity, Kind = kind, Action = action, By = by, ByRole = byRole, At = DateTimeOffset.Parse(at),
        });
    }

    private void Note(string userKey, string title, string message, string docKey, string at, bool unread) =>
        _notifications.Add(new MockNotification
        {
            UserId = MockIds.Of(userKey), Title = title, Message = message,
            RelatedId = MockIds.Of(docKey), At = DateTimeOffset.Parse(at), Unread = unread,
        });

    private MockReviewHistory History(MockDoc doc, MockUser u, string action, string? comments) => new()
    {
        DocumentId = doc.Id, Title = doc.Title, Number = doc.Number, Org = OrgName(doc.OrgId),
        TypeCode = doc.TypeCode, Action = action, Comments = comments, At = DateTimeOffset.Now,
        Score = doc.Score, ActorUserId = u.Id,
    };

    private MockReviewHistory HistoryKey(string docKey, string action, string at, string userKey)
    {
        var doc = RequireDoc(MockIds.Of(docKey));
        return new MockReviewHistory
        {
            DocumentId = doc.Id, Title = doc.Title, Number = doc.Number, Org = OrgName(doc.OrgId),
            TypeCode = doc.TypeCode, Action = action, At = DateTimeOffset.Parse(at), Score = doc.Score,
            ActorUserId = MockIds.Of(userKey),
        };
    }

    private static string RoleLabel(string? role) => role switch
    {
        "OrgOfficer" => "Org Officer",
        "SouStaff" => "SOU Admin",
        "SystemAdmin" => "System Admin",
        _ => role ?? "User",
    };

    private static string CollegeName(string code) => code switch
    {
        "CICS" => "College of Information and Computing Sciences",
        "CEA" => "College of Engineering and Architecture",
        "CBA" => "College of Business and Accountancy",
        "COED" => "College of Education",
        "CAFA" => "College of Architecture and Fine Arts",
        _ => code,
    };
}
