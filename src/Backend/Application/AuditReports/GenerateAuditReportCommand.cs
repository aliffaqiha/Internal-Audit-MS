using IAMS.Application.Common.Interfaces;
using IAMS.Application.Common.Exceptions;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using IAMS.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.AuditReports;

public sealed record GenerateAuditReportCommand(Guid AuditPlanId) : IRequest<AuditReportDto>;

internal sealed class GenerateAuditReportCommandHandler : IRequestHandler<GenerateAuditReportCommand, AuditReportDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ISender _sender;
    private readonly IReportService _reports;
    private readonly IObjectStorageService _storage;
    private readonly IAuditService _audit;
    private readonly IDateTimeService _dateTime;
    private readonly IPublisher _publisher;

    public GenerateAuditReportCommandHandler(
        IApplicationDbContext db,
        ISender sender,
        IReportService reports,
        IObjectStorageService storage,
        IAuditService audit,
        IDateTimeService dateTime,
        IPublisher publisher)
    {
        _db = db;
        _sender = sender;
        _reports = reports;
        _storage = storage;
        _audit = audit;
        _dateTime = dateTime;
        _publisher = publisher;
    }

    public async Task<AuditReportDto> Handle(GenerateAuditReportCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.AuditPlans
            .FirstOrDefaultAsync(p => p.Id == request.AuditPlanId, cancellationToken)
            ?? throw new KeyNotFoundException("Rencana audit tidak ditemukan.");

        if (plan.Status is AuditPlanStatus.Draft or AuditPlanStatus.Submitted)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Status"] = new[] { "Laporan hanya dapat dibuat untuk rencana audit yang sudah disetujui." }
            });
        }

        var data = await _sender.Send(new GetAuditReportDataQuery(request.AuditPlanId), cancellationToken);

        var bytes = _reports.GenerateAuditReportPdf(data);

        var now = _dateTime.UtcNowOffset;
        var fileName = SanitizeFileName($"Laporan-Audit-{plan.Title}.pdf");
        var objectName = $"reports/audit-plans/{request.AuditPlanId}/{now:yyyyMMdd_HHmmss}.pdf";

        using (var stream = new MemoryStream(bytes))
        {
            await _storage.UploadAsync(objectName, stream, "application/pdf", cancellationToken);
        }

        // Keep only the latest report per plan.
        var previous = await _db.AuditReports
            .FirstOrDefaultAsync(r => r.AuditPlanId == request.AuditPlanId, cancellationToken);
        if (previous is not null)
        {
            await _storage.DeleteAsync(previous.ObjectName, cancellationToken);
            _db.AuditReports.Remove(previous);
        }

        var report = new AuditReport
        {
            AuditPlanId = request.AuditPlanId,
            ObjectName = objectName,
            FileName = fileName,
            ContentType = "application/pdf",
            SizeBytes = bytes.Length,
            GeneratedAt = now
        };

        _db.AuditReports.Add(report);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("Report.Generated", nameof(AuditReport), report.Id.ToString(),
            oldValues: plan.Id.ToString(), newValues: fileName, cancellationToken: cancellationToken);

        await _publisher.Publish(new ReportGeneratedEvent(plan.Id, plan.Title), cancellationToken);

        return new AuditReportDto(
            report.Id,
            report.AuditPlanId,
            report.ObjectName,
            report.FileName,
            report.ContentType,
            report.SizeBytes,
            report.GeneratedAt);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "Laporan-Audit.pdf" : sanitized;
    }
}
