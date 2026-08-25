using MediatR;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Rfi;

namespace ProcureLive.Application.Rfi.Commands.CreateRfiTemplate;

public sealed class CreateRfiTemplateCommandHandler : IRequestHandler<CreateRfiTemplateCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateRfiTemplateCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateRfiTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = new RfiTemplate
        {
            Name = request.Name,
            Description = request.Description,
            CategoryId = request.CategoryId,
            IsActive = true
        };

        _context.RfiTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        return template.Id;
    }
}
