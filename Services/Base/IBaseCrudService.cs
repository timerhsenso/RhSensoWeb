using System.Linq.Expressions;

namespace RhSensoWeb.Services.Base
{
    /// <summary>
    /// Contrato genérico para operações CRUD via EF Core.
    /// </summary>
    public interface IBaseCrudService<TEntity, TKey>
        where TEntity : class
    {
        Task<List<TEntity>> GetAllAsync(CancellationToken ct = default);

        Task<(IReadOnlyList<TEntity> Items, int Total)> GetPagedAsync(
            int page, int pageSize,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            CancellationToken ct = default);

        Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);

        Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct = default);

        Task<TEntity> UpdateAsync(TKey id, TEntity incoming, CancellationToken ct = default);

        Task<bool> DeleteAsync(TKey id, CancellationToken ct = default);

        Task<bool> ExistsAsync(TKey id, CancellationToken ct = default);
    }
}
