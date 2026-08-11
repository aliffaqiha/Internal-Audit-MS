using IAMS.Application.AuditReports;
using IAMS.Application.Common.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IAMS.Infrastructure.Services;

public sealed class QuestPdfReportService : IReportService
{
    static QuestPdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateAuditReportPdf(AuditReportDataDto data)
    {
        var checklistSummary = BuildChecklistSummary(data);
        var riskSummary = BuildRiskSummary(data);
        var capSummary = BuildCapSummary(data);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Column(header =>
                {
                    header.Item().Text("LAPORAN HASIL AUDIT INTERNAL")
                        .FontSize(11).SemiBold().FontColor(Colors.Blue.Darken2);
                    header.Item().PaddingBottom(4).Text(data.Title)
                        .FontSize(20).Bold().FontColor(Colors.Blue.Darken4);
                    header.Item().PaddingBottom(4).LineHorizontal(0.8f).LineColor(Colors.Grey.Lighten1);
                    header.Item().Text(BuildHeaderLine(data))
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Content().Column(content =>
                {
                    content.Spacing(12);

                    content.Item().Column(summary =>
                    {
                        summary.Spacing(4);
                        summary.Item().Text("Ringkasan Eksekutif").FontSize(13).Bold();
                        summary.Item().Text("Objektif: " + (data.Objective ?? "-"));
                        summary.Item().Text("Ruang Lingkup: " + (data.Scope ?? "-"));
                        summary.Item().PaddingTop(4).Text(
                            $"Hasil checklist: {checklistSummary.Total} item | Lulus {checklistSummary.Pass}, " +
                            $"Gagal {checklistSummary.Fail}, N/A {checklistSummary.NotApplicable}, Pending {checklistSummary.Pending}.");
                        summary.Item().Text(
                            $"Temuan: {riskSummary.Total} | Rendah {riskSummary.Low}, Sedang {riskSummary.Medium}, " +
                            $"Tinggi {riskSummary.High}, Kritis {riskSummary.Critical}.");
                        summary.Item().Text(
                            $"Tindak lanjut (CAP): {capSummary.Total} | Terbuka {capSummary.Open}, " +
                            $"Berjalan {capSummary.InProgress}, Menunggu Verifikasi {capSummary.PendingVerification}, Selesai {capSummary.Closed}.");
                    });

                    content.Item().Column(team =>
                    {
                        team.Spacing(4);
                        team.Item().Text("Tim Audit").FontSize(13).Bold();
                        if (data.Assignments.Count == 0)
                        {
                            team.Item().Text("-");
                        }
                        else
                        {
                            team.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1.5f);
                                    c.RelativeColumn(2f);
                                    c.RelativeColumn(2f);
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Element(CellStyle).Text("Username").SemiBold();
                                    h.Cell().Element(CellStyle).Text("Nama Lengkap").SemiBold();
                                    h.Cell().Element(CellStyle).Text("Peran dalam Rencana").SemiBold();
                                });

                                foreach (var member in data.Assignments)
                                {
                                    table.Cell().Element(CellStyle).Text(member.Username);
                                    table.Cell().Element(CellStyle).Text(member.FullName);
                                    table.Cell().Element(CellStyle).Text(member.RoleInPlan ?? "-");
                                }
                            });
                        }
                    });

                    content.Item().Column(checklist =>
                    {
                        checklist.Spacing(4);
                        checklist.Item().Text("Hasil Checklist").FontSize(13).Bold();
                        if (data.ChecklistItems.Count == 0)
                        {
                            checklist.Item().Text("Tidak ada item checklist.");
                        }
                        else
                        {
                            checklist.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1.2f);
                                    c.RelativeColumn(4f);
                                    c.RelativeColumn(1.2f);
                                    c.RelativeColumn(2.5f);
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Element(CellStyle).Text("Kategori").SemiBold();
                                    h.Cell().Element(CellStyle).Text("Pertanyaan / Kontrol").SemiBold();
                                    h.Cell().Element(CellStyle).Text("Status").SemiBold();
                                    h.Cell().Element(CellStyle).Text("Catatan").SemiBold();
                                });

                                foreach (var item in data.ChecklistItems)
                                {
                                    table.Cell().Element(CellStyle).Text(item.Category ?? "-");
                                    table.Cell().Element(CellStyle).Text(item.Question);
                                    table.Cell().Element(CellStyle).Text(ChecklistStatusLabel(item.Status));
                                    table.Cell().Element(CellStyle).Text(item.Note ?? "-");
                                }
                            });
                        }
                    });

                    if (data.Findings.Count > 0)
                    {
                        content.Item().Column(findings =>
                        {
                            findings.Spacing(6);
                            findings.Item().Text("Temuan Audit").FontSize(13).Bold();

                            foreach (var finding in data.Findings)
                            {
                                findings.Item().Column(block =>
                                {
                                    block.Spacing(2);
                                    block.Item().Text($"• {finding.Title}")
                                        .SemiBold().FontColor(Colors.Blue.Darken2);
                                    block.Item().Text(
                                        $"Tingkat Risiko: {RiskLevelLabel(finding.RiskLevel)} | " +
                                        $"Kategori: {finding.Category ?? "-"} | Departemen: {finding.DepartmentName ?? "-"}");
                                    block.Item().Text("Deskripsi: " + (finding.Description ?? "-"));
                                    block.Item().Text("Rekomendasi: " + (finding.Recommendation ?? "-"));
                                    block.Item().Text(
                                        "Batas Penyelesaian: " +
                                        (finding.DueDate.HasValue ? finding.DueDate.Value.ToLocalTime().ToString("dd MMM yyyy") : "-"));

                                    if (finding.CorrectiveAction is not null)
                                    {
                                        var cap = finding.CorrectiveAction;
                                        block.Item().PaddingTop(2).Text("Tindak Lanjut (CAP)")
                                            .SemiBold().FontSize(9).FontColor(Colors.Green.Darken2);
                                        block.Item().Text($"Aksi: {cap.Action} | Status: {CapStatusLabel(cap.Status)}");
                                        block.Item().Text(
                                            $"PIC: {cap.PicName ?? "-"} | Target: " +
                                            (cap.TargetDate.HasValue ? cap.TargetDate.Value.ToLocalTime().ToString("dd MMM yyyy") : "-") +
                                            $" | Progres: {cap.Progress}%");
                                    }
                                    else
                                    {
                                        block.Item().Text("Tindak Lanjut (CAP): Belum ada.").FontColor(Colors.Grey.Darken1);
                                    }
                                });
                            }
                        });
                    }

                    content.Item().Column(conclusion =>
                    {
                        conclusion.Spacing(4);
                        conclusion.Item().Text("Kesimpulan").FontSize(13).Bold();
                        conclusion.Item().Text(
                            data.Findings.Count == 0
                                ? "Tidak terdapat temuan pada ruang lingkup audit ini."
                                : $"Audit menemukan {riskSummary.Total} temuan "
                                  + $"(rendah {riskSummary.Low}, sedang {riskSummary.Medium}, tinggi {riskSummary.High}, kritis {riskSummary.Critical}). "
                                  + "Seluruh temuan direkomendasikan untuk ditindaklanjuti sesuai rekomendasi yang telah disusun "
                                  + "dan dimonitor hingga seluruh tindak lanjut (CAP) selesai.");
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("IAMS - Internal Audit Management System | Halaman ");
                    text.CurrentPageNumber();
                    text.Span(" dari ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string BuildHeaderLine(AuditReportDataDto data)
    {
        var period = data.StartDate.HasValue
            ? $"{data.StartDate.Value.ToLocalTime():dd MMM yyyy} s/d "
              + (data.EndDate.HasValue ? data.EndDate.Value.ToLocalTime().ToString("dd MMM yyyy") : "-")
            : "-";

        var parts = new List<string>
        {
            "Standar: " + (data.Standard ?? "-"),
            "Departemen: " + (data.DepartmentName ?? "-"),
            "Status: " + data.Status,
            "Periode: " + period
        };
        return string.Join("   |   ", parts);
    }

    private static IContainer CellStyle(IContainer container) => container
        .Border(1)
        .BorderColor(Colors.Grey.Lighten2)
        .Padding(5);

    private static string RiskLevelLabel(string riskLevel) => riskLevel switch
    {
        "Critical" => "Kritis",
        "High" => "Tinggi",
        "Medium" => "Sedang",
        "Low" => "Rendah",
        _ => riskLevel
    };

    private static string ChecklistStatusLabel(string status) => status switch
    {
        "Pass" => "Lulus",
        "Fail" => "Gagal",
        "NotApplicable" => "N/A",
        _ => "Pending"
    };

    private static string CapStatusLabel(string status) => status switch
    {
        "InProgress" => "Berjalan",
        "PendingVerification" => "Menunggu Verifikasi",
        "Closed" => "Selesai",
        _ => "Terbuka"
    };

    private static ChecklistSummary BuildChecklistSummary(AuditReportDataDto data)
    {
        var summary = new ChecklistSummary();
        foreach (var item in data.ChecklistItems)
        {
            summary.Total++;
            switch (item.Status)
            {
                case "Pass": summary.Pass++; break;
                case "Fail": summary.Fail++; break;
                case "NotApplicable": summary.NotApplicable++; break;
                default: summary.Pending++; break;
            }
        }
        return summary;
    }

    private static RiskSummary BuildRiskSummary(AuditReportDataDto data)
    {
        var summary = new RiskSummary();
        foreach (var finding in data.Findings)
        {
            summary.Total++;
            switch (finding.RiskLevel)
            {
                case "Critical": summary.Critical++; break;
                case "High": summary.High++; break;
                case "Medium": summary.Medium++; break;
                default: summary.Low++; break;
            }
        }
        return summary;
    }

    private static CapSummary BuildCapSummary(AuditReportDataDto data)
    {
        var summary = new CapSummary();
        foreach (var finding in data.Findings)
        {
            if (finding.CorrectiveAction is null)
                continue;

            summary.Total++;
            switch (finding.CorrectiveAction.Status)
            {
                case "InProgress": summary.InProgress++; break;
                case "PendingVerification": summary.PendingVerification++; break;
                case "Closed": summary.Closed++; break;
                default: summary.Open++; break;
            }
        }
        return summary;
    }

    private sealed class ChecklistSummary
    {
        public int Total { get; set; }
        public int Pass { get; set; }
        public int Fail { get; set; }
        public int NotApplicable { get; set; }
        public int Pending { get; set; }
    }

    private sealed class RiskSummary
    {
        public int Total { get; set; }
        public int Low { get; set; }
        public int Medium { get; set; }
        public int High { get; set; }
        public int Critical { get; set; }
    }

    private sealed class CapSummary
    {
        public int Total { get; set; }
        public int Open { get; set; }
        public int InProgress { get; set; }
        public int PendingVerification { get; set; }
        public int Closed { get; set; }
    }
}
