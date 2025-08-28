using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using RhSensoWeb.Common;
using RhSensoWeb.Models;

namespace RhSensoWeb.Areas.SYS.Services
{
    public interface ITaux2Service
    {
        Task<ApiResponse<IEnumerable<Taux2>>> GetDataAsync(string userId);
        Task<ApiResponse> CreateAsync(Taux2 entity, ModelStateDictionary modelState);
        Task<ApiResponse> EditAsync((string cdtptabela, string cdsituacao) id, Taux2 entity, ModelStateDictionary modelState);

        Task<(ApiResponse resp, Taux2? entidade)> GetForSafeEditAsync(string token, string userId);
        Task<ApiResponse> DeleteByTokenAsync(string token, string userId);

        Task<Taux2?> GetByIdAsync((string cdtptabela, string cdsituacao) id);
        Task<ApiResponse> DeleteByIdAsync((string cdtptabela, string cdsituacao) id);

        Task<ApiResponse<int>> HealthCheckAsync();

        // Combos
        Task<IEnumerable<SelectListItem>> GetTiposTabelaAsync();
    }
}