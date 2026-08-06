namespace IAMS.Application.Common.Interfaces;

public interface IDateTimeService
{
    DateTime UtcNow { get; }
    DateTimeOffset UtcNowOffset { get; }
}