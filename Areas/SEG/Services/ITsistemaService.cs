using Microsoft.AspNetCore.Mvc.ModelBinding;
using RhSensoWeb.Common;
using RhSensoWeb.Models;
using RhSensoWeb.Areas.SEG.DTOs;

namespace RhSensoWeb.Areas.SEG.Services
{
    public interface ITsistemaService
    {
        Task<ApiResponse<IEnumerable<TsistemaListItemDto>>> GetDataAsync(string userId);
        Task<ApiResponse> UpdateAtivoAsync(string id, bool ativo, string userId);
        Task<ApiResponse> CreateAsync(Tsistema sistema, ModelStateDictionary modelState);
        Task<ApiResponse> EditAsync(string id, Tsistema sistema, ModelStateDictionary modelState);
        Task<ApiResponse> DeleteByTokenAsync(string token, string userId);
        Task<ApiResponse<int>> HealthCheckAsync();
        Task<(ApiResponse resp, Tsistema? entidade)> GetForSafeEditAsync(string token, string userId);
    }
}
