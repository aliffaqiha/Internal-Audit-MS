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
}