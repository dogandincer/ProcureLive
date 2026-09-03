using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Catalog;

namespace ProcureLive.Application.Catalog.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        Category? category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException(nameof(Category), request.Id);
        }

        if (request.ParentCategoryId is not null)
        {
            // Walk up the proposed new parent's ancestor chain - if it revisits this category, the
            // move would create a cycle and infinite-loop any code that walks the tree (including the
            // admin UI's own tree-flattening).
            Guid? ancestorId = request.ParentCategoryId;
            var visited = new HashSet<Guid>();

            while (ancestorId is not null)
            {
                if (ancestorId == request.Id)
                {
                    throw new ConflictException("Bu değişiklik kategori ağacında bir döngü oluşturur.");
                }

                if (!visited.Add(ancestorId.Value))
                {
                    break;
                }

                ancestorId = await _context.Categories
                    .Where(c => c.Id == ancestorId)
                    .Select(c => c.ParentCategoryId)
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }

        category.Name = request.Name;
        category.Code = request.Code;
        category.ParentCategoryId = request.ParentCategoryId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
