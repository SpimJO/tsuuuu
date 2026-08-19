using System.Security.Cryptography;
using System.Text;

namespace TsuOrg.Frontend.Services;

internal static class MockIds
{
    public static Guid Of(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("tsuorg-static:" + key));
        var bytes = hash.AsSpan(0, 16).ToArray();
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x40);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }
}

internal sealed class MockUser
{
    public Guid Id { get; init; }
    public string Email { get; init; } = "";
    public string Password { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; init; } = "";
    public string? College { get; init; }
    public Guid? OrgId { get; init; }
    public string Position { get; init; } = "";
    public string? AvatarDataUrl { get; set; }
}

internal sealed class MockOrg
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Acronym { get; init; } = "";
    public string College { get; init; } = "";
    public string AdviserName { get; init; } = "";
    public string PresidentName { get; init; } = "";
    public string Status { get; init; } = "Active";
    public string Health { get; init; } = "Active";
    public int Submissions { get; init; }
    public int Approved { get; init; }
    public int Returned { get; init; }
    public int Compliance { get; init; }
}

internal sealed class MockAttachment
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "application/pdf";
}

internal sealed class MockValidation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string DocumentClass { get; init; } = "Valid Submission";
    public decimal Confidence { get; init; } = 0.91m;
    public int ScorePercent { get; init; } = 91;
    public bool RequiresHumanReview { get; init; }
    public decimal OcrAvg { get; init; } = 0.94m;
    public string ModelVersion { get; init; } = "LayoutLMv3-base";
    public string? OcrCaption { get; init; }
    public string? OcrScanned { get; init; }
    public string? OcrFullText { get; init; }
    public IReadOnlyList<string> KeyFields { get; init; } = [];
    public IReadOnlyList<(string Token, double Confidence, string Flag)> Flags { get; init; } = [];
    public IReadOnlyList<(string Label, string Value)> Extracted { get; init; } = [];
    public IReadOnlyList<(string Label, bool Passed)> Checks { get; init; } = [];
    public System.Text.Json.JsonElement Fields { get; init; }
    public DateTimeOffset ProcessedAt { get; init; } = DateTimeOffset.Now;
}

internal sealed class MockVersion
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int VersionNumber { get; init; }
    public string FileName { get; init; } = "";
    public string? ChangeSummary { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

internal sealed class MockEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset At { get; init; }
    public string Headline { get; init; } = "";
    public string Detail { get; init; } = "";
    public string Stage { get; init; } = "";
    public string Status { get; init; } = "";
    public string Message { get; init; } = "";
    public Guid? ActorUserId { get; init; }
    public string? ActorName { get; init; }
    public string? ActorRole { get; init; }
    public string? Action { get; init; }
    public string? Comments { get; init; }
}

internal sealed class MockDoc
{
    public Guid Id { get; init; }
    public string Number { get; set; } = "";
    public string Title { get; set; } = "";
    public Guid TypeId { get; init; }
    public string TypeName { get; init; } = "";
    public string TypeCode { get; init; } = "";
    public Guid OrgId { get; init; }
    public string Status { get; set; } = "Draft";
    public string Stage { get; set; } = "Officer";
    public int? Score { get; set; }
    public bool Flagged { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string SubmittedBy { get; set; } = "";
    public string SubmittedByPosition { get; set; } = "";
    public string? AdviserName { get; set; }
    public string? AssignedTo { get; set; }
    public bool Locked { get; set; }
    public DateTimeOffset? LockedAt { get; set; }
    public string? PrimaryFile { get; set; }
    public string PrimaryContentType { get; set; } = "application/pdf";
    public List<MockAttachment> Attachments { get; } = [];
    public bool Archived { get; set; }
    public string? ReturnComment { get; set; }
    public MockValidation? Validation { get; set; }
    public List<MockVersion> Versions { get; } = [];
    public List<MockEvent> Events { get; } = [];
}

internal sealed class MockNotification
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public string Title { get; init; } = "";
    public string Message { get; init; } = "";
    public Guid? RelatedId { get; init; }
    public DateTimeOffset At { get; init; }
    public bool Unread { get; set; } = true;
}

internal sealed class MockAudit
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid DocumentId { get; init; }
    public string Number { get; init; } = "";
    public string Title { get; init; } = "";
    public string TypeCode { get; init; } = "";
    public string TypeName { get; init; } = "";
    public string Activity { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Action { get; init; } = "";
    public string By { get; init; } = "";
    public string ByRole { get; init; } = "";
    public DateTimeOffset At { get; init; }
}

internal sealed class MockReviewHistory
{
    public Guid DocumentId { get; init; }
    public string Title { get; init; } = "";
    public string Number { get; init; } = "";
    public string Org { get; init; } = "";
    public string TypeCode { get; init; } = "";
    public string Action { get; init; } = "";
    public string? Comments { get; init; }
    public DateTimeOffset At { get; init; }
    public int? Score { get; init; }
    public Guid ActorUserId { get; init; }
}
