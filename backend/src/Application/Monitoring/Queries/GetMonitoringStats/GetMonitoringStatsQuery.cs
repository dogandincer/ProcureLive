using MediatR;
using ProcureLive.Contracts.Monitoring;

namespace ProcureLive.Application.Monitoring.Queries.GetMonitoringStats;

public sealed record GetMonitoringStatsQuery : IRequest<MonitoringStatsDto>;
