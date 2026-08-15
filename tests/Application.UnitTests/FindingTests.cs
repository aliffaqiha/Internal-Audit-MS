using FluentValidation.TestHelper;
using IAMS.Application.Findings;
using IAMS.Domain.Enums;
using Xunit;

namespace IAMS.Application.UnitTests;

public class CreateFindingCommandValidatorTests
{
    private readonly CreateFindingCommandValidator _validator = new();

    private CreateFindingCommand Valid()
        => new("Backup tidak jalan", "Backup gagal 3 hari", null, RiskLevel.High,
            "Backup", "Segera perbaiki backup", new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), null);

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.TestValidate(Valid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTitle_Fails()
    {
        var result = _validator.TestValidate(Valid() with { Title = "" });
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Theory]
    [InlineData((RiskLevel)0)]
    [InlineData((RiskLevel)99)]
    public void InvalidRiskLevel_Fails(RiskLevel risk)
    {
        var result = _validator.TestValidate(Valid() with { RiskLevel = risk });
        result.ShouldHaveValidationErrorFor(x => x.RiskLevel);
    }

    [Fact]
    public void TooLongDescription_Fails()
    {
        var result = _validator.TestValidate(Valid() with { Description = new string('x', 4001) });
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}

public class EvidenceFileRulesTests
{
    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("application/msword")]
    public void AllowedContentTypes_AreAccepted(string contentType)
        => Assert.True(EvidenceFileRules.IsContentTypeAllowed(contentType));

    [Theory]
    [InlineData("text/html")]
    [InlineData("application/x-msdownload")]
    [InlineData("text/plain")]
    [InlineData("")]
    public void DisallowedContentTypes_AreRejected(string contentType)
        => Assert.False(EvidenceFileRules.IsContentTypeAllowed(contentType));

    [Theory]
    [InlineData("report.pdf", "report.pdf")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("..\\..\\..\\private\\key.xlsx", "key.xlsx")]
    [InlineData("c:\\windows\\system32\\notes.docx", "notes.docx")]
    [InlineData("../../../../../tmp/attack.pdf", "attack.pdf")]
    public void FileNames_AreSanitized(string input, string expected)
        => Assert.Equal(expected, EvidenceFileRules.SanitizeFileName(input));

    [Fact]
    public void EmptyFileName_FallsBackToSafeName()
        => Assert.Equal("evidence.bin", EvidenceFileRules.SanitizeFileName("  "));

    [Fact]
    public void MaxSize_IsTenMegabytes()
        => Assert.Equal(10 * 1024 * 1024, EvidenceFileRules.MaxSizeBytes);

    private static Stream StreamOf(params byte[][] chunks)
    {
        var ms = new MemoryStream();
        foreach (var chunk in chunks)
            ms.Write(chunk, 0, chunk.Length);
        ms.Position = 0;
        return ms;
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("application/vnd.ms-excel")]
    [InlineData("application/msword")]
    public void ContentTypes_HaveKnownFileFamily(string contentType)
        => Assert.NotEqual(EvidenceFileRules.FileFamily.Unknown,
            SniffFamilyFor(contentType));

    [Fact]
    public void SniffPdf_DetectsPdfFamily()
        => Assert.Equal(EvidenceFileRules.FileFamily.Pdf,
            EvidenceFileRules.SniffFileFamily(StreamOf(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 })));

    [Fact]
    public void SniffPng_DetectsPngFamily()
        => Assert.Equal(EvidenceFileRules.FileFamily.Png,
            EvidenceFileRules.SniffFileFamily(StreamOf(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })));

    [Fact]
    public void SniffJpeg_DetectsJpegFamily()
        => Assert.Equal(EvidenceFileRules.FileFamily.Jpeg,
            EvidenceFileRules.SniffFileFamily(StreamOf(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })));

    [Fact]
    public void SniffZip_DetectsOfficeZipFamily()
        => Assert.Equal(EvidenceFileRules.FileFamily.OfficeZip,
            EvidenceFileRules.SniffFileFamily(StreamOf(new byte[] { 0x50, 0x4B, 0x03, 0x04 })));

    [Fact]
    public void SniffOle2_DetectsOfficeOle2Family()
        => Assert.Equal(EvidenceFileRules.FileFamily.OfficeOle2,
            EvidenceFileRules.SniffFileFamily(StreamOf(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 })));

    [Fact]
    public void SniffUnknownBytes_ReturnsUnknown()
        => Assert.Equal(EvidenceFileRules.FileFamily.Unknown,
            EvidenceFileRules.SniffFileFamily(StreamOf(new byte[] { 0x3C, 0x68, 0x74, 0x6D, 0x6C })));

    [Fact]
    public void SniffFileFamily_RewindsStream()
    {
        var stream = StreamOf(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 });
        EvidenceFileRules.SniffFileFamily(stream);
        Assert.Equal(0, stream.Position);
    }

    [Theory]
    [InlineData("application/pdf", "application/pdf", true)]
    [InlineData("image/png", "image/png", true)]
    [InlineData("application/pdf", "image/png", false)]
    // xlsx and docx share the same ZIP container; only the family can be verified.
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", true)]
    [InlineData("application/msword", "application/vnd.ms-excel", true)]
    public void MatchesFamily_ComparesDeclaredType(string declared, string sniffedAs, bool expected)
        => Assert.Equal(expected,
            EvidenceFileRules.MatchesFamily(declared, SniffFamilyFor(sniffedAs)));

    private static EvidenceFileRules.FileFamily SniffFamilyFor(string contentType)
        => contentType switch
        {
            "application/pdf" => EvidenceFileRules.FileFamily.Pdf,
            "image/png" => EvidenceFileRules.FileFamily.Png,
            "image/jpeg" => EvidenceFileRules.FileFamily.Jpeg,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => EvidenceFileRules.FileFamily.OfficeZip,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => EvidenceFileRules.FileFamily.OfficeZip,
            "application/vnd.ms-excel" => EvidenceFileRules.FileFamily.OfficeOle2,
            "application/msword" => EvidenceFileRules.FileFamily.OfficeOle2,
            _ => EvidenceFileRules.FileFamily.Unknown
        };
}