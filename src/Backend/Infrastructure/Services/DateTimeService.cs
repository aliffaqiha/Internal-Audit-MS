using IAMS.Application.Common.Interfaces;

namespace IAMS.Infrastructure.Common;

public sealed class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;
}