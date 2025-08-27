using Microsoft.AspNetCore.Mvc.ModelBinding;
using RhSensoWeb.Common;
using RhSensoWeb.Models;

namespace RhSensoWeb.Areas.SEG.Services
{
    public interface IBtfuncaoService
    {
        /// <summary>Retorna coleção tipada para o DataTable e um ApiResponse com o status.</summary>
        Task<(ApiResponse resp, IEnumerable<Btfuncao> data)> GetDataAsync(string userId);

        /// <summary>Decodifica token e carrega a entidade para edição segura.</summary>
        Task<(ApiResponse resp, Btfuncao? entidade)> GetForSafeEditAsync(string token, string userId);

        /// <summary>Cria um novo registro (valida a chave composta e ModelState).</summary>
        Task<ApiResponse> CreateAsync(Btfuncao entidade, ModelStateDictionary modelState);

        /// <summary>Edita um registro existente (não permite alterar a chave).</summary>
        Task<ApiResponse> EditAsync((string cds, string cdf, string nmb) id, Btfuncao entidade, ModelStateDictionary modelState);

        /// <summary>Decodifica token e exclui o registro.</summary>
        Task<ApiResponse> DeleteByTokenAsync(string token, string userId);

        /// <summary>Health check simples (ex.: total de registros em mensagem).</summary>
        Task<ApiResponse> HealthCheckAsync();
    }
}
