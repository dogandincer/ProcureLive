using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Catalog;

namespace ProcureLive.Application.Catalog.Categories.Commands.DeleteCategory;

public sealed class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        Category? category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException(nameof(Category), request.Id);
        }

        bool hasChildren = await _context.Categories.AnyAsync(c => c.ParentCategoryId == request.Id, cancellationToken);

        if (hasChildren)
        {
            throw new ConflictException("Alt kategorileri olan bir kategori silinemez.");
        }

        bool inUse = await _context.Products.AnyAsync(p => p.CategoryId == request.Id, cancellationToken);
        if (inUse)
        {
            throw new ConflictException("Ürünlerde kullanılan kategori silinemez. Önce bu kategorideki ürünleri taşıyın veya silin.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
