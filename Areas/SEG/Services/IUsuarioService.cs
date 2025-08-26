// ==============================================
// File: Areas/SEG/Services/IUsuarioService.cs
// ==============================================
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RhSensoWeb.Common;
using RhSensoWeb.Models; // Tuse1
using RhSensoWeb.Areas.SEG.DTOs; // UsuarioListItemDto


namespace RhSensoWeb.Areas.SEG.Services
{
    public interface IUsuarioService
    {
        Task<ApiResponse<IEnumerable<UsuarioListItemDto>>> GetDataAsync(string userId);
        Task<ApiResponse> UpdateAtivoAsync(string id, bool ativo, string userId);
        Task<ApiResponse> CreateAsync(Tuse1 usuario, ModelStateDictionary modelState);
        Task<ApiResponse> EditAsync(string id, Tuse1 usuario, ModelStateDictionary modelState);
        Task<ApiResponse> DeleteByTokenAsync(string token, string userId);
        Task<ApiResponse<int>> HealthCheckAsync();
        Task<(ApiResponse resp, Tuse1? entidade)> GetForSafeEditAsync(string token, string userId);
        Task<Tuse1?> GetByIdAsync(string id);
        Task<ApiResponse> DeleteByIdAsync(string id);
    }
}