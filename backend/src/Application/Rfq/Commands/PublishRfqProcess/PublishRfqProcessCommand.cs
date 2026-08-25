using MediatR;

namespace ProcureLive.Application.Rfq.Commands.PublishRfqProcess;

/// <summary>RFQ setup step 4/4: review and publish (moves Sent -> InProgress, open for quotes).</summary>
public sealed record PublishRfqProcessCommand(Guid RfqProcessId) : IRequest;
