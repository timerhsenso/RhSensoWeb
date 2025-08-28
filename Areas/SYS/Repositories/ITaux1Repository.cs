using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using RhSensoWeb.Models;

namespace RhSensoWeb.Areas.SYS.Taux1.Repositories
{
    public interface ITaux1Repository
    {
        IQueryable<RhSensoWeb.Models.Taux1> Query();
        Task<RhSensoWeb.Models.Taux1?> GetByIdAsync(string cdtptabela);
        Task<bool> ExistsAsync(string cdtptabela);
        Task AddAsync(RhSensoWeb.Models.Taux1 entity);
        Task UpdateAsync(RhSensoWeb.Models.Taux1 entity);
        Task DeleteAsync(string cdtptabela);
        Task DeleteBatchAsync(IEnumerable<string> ids);
        Task<int> SaveChangesAsync();
    }
}
