using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Infrastructure.Services;

public sealed class DateTimeService : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
}
