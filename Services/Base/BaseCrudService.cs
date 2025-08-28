using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RhSensoWeb.Data;
using System.Linq.Expressions;

namespace RhSensoWeb.Services.Base
{
    /// <summary>
    /// Implementação base genérica para CRUD (EF Core).
    /// - Chave simples por padrão. Para chaves compostas, sobrescreva BuildKeyValues.
    /// - Hooks de domínio: OnBeforeCreate/Update/Delete.
    /// </summary>
    public abstract class BaseCrudService<TEntity, TKey> : IBaseCrudService<TEntity, TKey>
        where TEntity : class
    {
        protected readonly ApplicationDbContext _db;
        protected readonly ILogger _logger;
        protected DbSet<TEntity> Set => _db.Set<TEntity>();

        protected BaseCrudService(ApplicationDbContext db, ILogger logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Para chave composta, retorne new object[] { parte1, parte2, ... }.
        /// </summary>
        protected virtual object[] BuildKeyValues(TKey id) => new object[] { id! };

        // Hooks (opcionais) para especializações por entidade
        protected virtual Task OnBeforeCreateAsync(TEntity entity, CancellationToken ct) => Task.CompletedTask;
        protected virtual Task OnBeforeUpdateAsync(TEntity current, TEntity incoming, CancellationToken ct) => Task.CompletedTask;
        protected virtual Task OnBeforeDeleteAsync(TEntity current, CancellationToken ct) => Task.CompletedTask;

        public virtual async Task<List<TEntity>> GetAllAsync(CancellationToken ct = default)
            => await Set.AsNoTracking().ToListAsync(ct);

        public virtual async Task<(IReadOnlyList<TEntity> Items, int Total)> GetPagedAsync(
            int page, int pageSize,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            IQueryable<TEntity> q = Set.AsNoTracking();
            if (filter is not null) q = q.Where(filter);

            var total = await q.CountAsync(ct);
            if (orderBy is not null) q = orderBy(q);

            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return (items, total);
        }

        public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default)
            => await Set.FindAsync(BuildKeyValues(id), ct);

        public virtual async Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct = default)
        {
            await OnBeforeCreateAsync(entity, ct);
            await Set.AddAsync(entity, ct);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public virtual async Task<TEntity> UpdateAsync(TKey id, TEntity incoming, CancellationToken ct = default)
        {
            var current = await GetByIdAsync(id, ct);
            if (current is null)
                throw new KeyNotFoundException($"{typeof(TEntity).Name}({id}) não encontrado.");

            await OnBeforeUpdateAsync(current, incoming, ct);
            _db.Entry(current).CurrentValues.SetValues(incoming);
            await _db.SaveChangesAsync(ct);
            return current;
        }

        public virtual async Task<bool> DeleteAsync(TKey id, CancellationToken ct = default)
        {
            var current = await GetByIdAsync(id, ct);
            if (current is null) return false;

            await OnBeforeDeleteAsync(current, ct);
            Set.Remove(current);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public virtual async Task<bool> ExistsAsync(TKey id, CancellationToken ct = default)
            => (await GetByIdAsync(id, ct)) is not null;
    }
}
