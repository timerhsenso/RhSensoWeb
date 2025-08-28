using System.Collections.Generic;
using System.Threading.Tasks;
using RhSensoWeb.Areas.SYS.Taux1.DTOs;

namespace RhSensoWeb.Areas.SYS.Taux1.Services
{
    public interface ITaux1Service
    {
        Task<(IEnumerable<Taux1Dto> items, int total, int filtered)> GetPageAsync(DataTableRequest req);
        Task<Taux1Dto?> GetAsync(string id);
        Task CreateAsync(Taux1Dto dto);
        Task UpdateAsync(string id, Taux1Dto dto);
        Task DeleteAsync(string id);
        Task DeleteBatchAsync(IEnumerable<string> ids);
    }
}
