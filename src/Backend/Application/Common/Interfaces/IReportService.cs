using IAMS.Application.AuditReports;

namespace IAMS.Application.Common.Interfaces;

/// <summary>Renders an audit report PDF from collected report data.</summary>
public interface IReportService
{
    byte[] GenerateAuditReportPdf(AuditReportDataDto data);
}
