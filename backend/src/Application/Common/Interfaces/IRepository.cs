using System.Linq.Expressions;
using ProcureLive.Domain.Common;

namespace ProcureLive.Application.Common.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<List<T>> ListAsync(Expression<Func<T, bool>>? predicate, CancellationToken cancellationToken);

    Task AddAsync(T entity, CancellationToken cancellationToken);

    void Update(T entity);

    void Remove(T entity);
}
