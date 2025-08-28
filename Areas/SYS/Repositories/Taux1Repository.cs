using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RhSensoWeb.Data; // seu ApplicationDbContext
using RhSensoWeb.Models;

namespace RhSensoWeb.Areas.SYS.Taux1.Repositories
{
    public class Taux1Repository : ITaux1Repository
    {
        private readonly ApplicationDbContext _db;

        public Taux1Repository(ApplicationDbContext db)
        {
            _db = db;
        }

        public IQueryable<RhSensoWeb.Models.Taux1> Query() => _db.Set<RhSensoWeb.Models.Taux1>().AsNoTracking();

        public async Task<RhSensoWeb.Models.Taux1?> GetByIdAsync(string cdtptabela)
            => await _db.Set<RhSensoWeb.Models.Taux1>().FindAsync(cdtptabela);

        public async Task<bool> ExistsAsync(string cdtptabela)
            => await _db.Set<RhSensoWeb.Models.Taux1>().AnyAsync(x => x.Cdtptabela == cdtptabela);

        public async Task AddAsync(RhSensoWeb.Models.Taux1 entity)
            => await _db.Set<RhSensoWeb.Models.Taux1>().AddAsync(entity);

        public Task UpdateAsync(RhSensoWeb.Models.Taux1 entity)
        {
            _db.Set<RhSensoWeb.Models.Taux1>().Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(string cdtptabela)
        {
            var e = await _db.Set<RhSensoWeb.Models.Taux1>().FindAsync(cdtptabela);
            if (e != null) _db.Set<RhSensoWeb.Models.Taux1>().Remove(e);
        }

        public async Task DeleteBatchAsync(IEnumerable<string> ids)
        {
            var list = await _db.Set<RhSensoWeb.Models.Taux1>()
                .Where(x => ids.Contains(x.Cdtptabela))
                .ToListAsync();
            _db.Set<RhSensoWeb.Models.Taux1>().RemoveRange(list);
        }

        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
