// Areas/SYS/Services/ITaux1Service.cs
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RhSensoWeb.Common;
using RhSensoWeb.Models;

namespace RhSensoWeb.Areas.SYS.Services
{
    public interface ITaux1Service
    {
        Task<ApiResponse<IEnumerable<Taux1>>> GetDataAsync(string userId);
        Task<ApiResponse> CreateAsync(Taux1 entity, ModelStateDictionary modelState);
        Task<ApiResponse> EditAsync(string id, Taux1 entity, ModelStateDictionary modelState);

        Task<(ApiResponse resp, Taux1? entidade)> GetForSafeEditAsync(string token, string userId);
        Task<ApiResponse> DeleteByTokenAsync(string token, string userId);

        Task<Taux1?> GetByIdAsync(string id);
        Task<ApiResponse> DeleteByIdAsync(string id);

        Task<ApiResponse<int>> HealthCheckAsync();
    }
}
