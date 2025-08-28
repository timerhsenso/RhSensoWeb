using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RhSensoWeb.Common;              // ApiResponse
using RhSensoWeb.Data;
using RhSensoWeb.Models;              // Taux2
using RhSensoWeb.Services.Security;   // IRowTokenService

namespace RhSensoWeb.Areas.SYS.Services
{
    public sealed class Taux2Service : ITaux2Service
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<Taux2Service> _logger;
        private readonly IRowTokenService _rowToken;
        private readonly IMemoryCache _cache;

        private const string PurposeEdit = "Edit";
        private const string PurposeDelete = "Delete";

        public Taux2Service(
            ApplicationDbContext db,
            ILogger<Taux2Service> logger,
            IRowTokenService rowToken,
            IMemoryCache cache)
        {
            _db = db;
            _logger = logger;
            _rowToken = rowToken;
            _cache = cache;
        }

        // Payload do token opaco
        private sealed record RowKeys(string Cdtptabela, string Cdsituacao);

        // Helper local: ModelState -> Dictionary<string, string[]>
        private static Dictionary<string, string[]> BuildErrorDict(ModelStateDictionary modelState)
        {
            var dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in modelState)
            {
                var errs = kv.Value?.Errors;
                if (errs is null || errs.Count == 0) continue;
                dict[kv.Key] = errs
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Valor inválido." : e.ErrorMessage)
                    .ToArray();
            }
            return dict;
        }

        // ===== LISTAGEM =====
        public async Task<ApiResponse<IEnumerable<Taux2>>> GetDataAsync(string userId)
        {
            try
            {
                var rows = await _db.Taux2
                    .AsNoTracking()
                    .OrderBy(x => x.Cdtptabela).ThenBy(x => x.Noordem).ThenBy(x => x.Cdsituacao)
                    .ToListAsync();

                return ApiResponse<IEnumerable<Taux2>>.Ok(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter dados de Taux2");
                return ApiResponse<IEnumerable<Taux2>>.Fail("Erro ao carregar dados do servidor.");
            }
        }

        // ===== CREATE =====
        public async Task<ApiResponse> CreateAsync(Taux2 entity, ModelStateDictionary modelState)
        {
            if (!modelState.IsValid)
                return ApiResponse.Fail("Verifique os campos destacados.", BuildErrorDict(modelState));

            try
            {
                var k1 = (entity.Cdtptabela ?? string.Empty).Trim();
                var k2 = (entity.Cdsituacao ?? string.Empty).Trim();

                var exists = await _db.Taux2.AsNoTracking().AnyAsync(x => x.Cdtptabela == k1 && x.Cdsituacao == k2);
                if (exists)
                {
                    modelState.AddModelError(nameof(Taux2.Cdtptabela), "Já existe um registro com este código.");
                    return ApiResponse.Fail("Já existe um registro com este código.", BuildErrorDict(modelState));
                }

                entity.Cdtptabela = k1; // normaliza PK
                entity.Cdsituacao = k2;
                entity.Dcsituacao = (entity.Dcsituacao ?? string.Empty).Trim();
                entity.Flativoaux = entity.Ativo ? "S" : "N";

                _db.Taux2.Add(entity);
                await _db.SaveChangesAsync();

                return ApiResponse.Ok("Registro criado com sucesso!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar Taux2 {K1}-{K2}", entity.Cdtptabela, entity.Cdsituacao);
                return ApiResponse.Fail("Erro ao salvar o registro. Tente novamente.");
            }
        }

        // ===== EDIT =====
        public async Task<ApiResponse> EditAsync((string cdtptabela, string cdsituacao) id, Taux2 entity, ModelStateDictionary modelState)
        {
            var k1 = (id.cdtptabela ?? string.Empty).Trim();
            var k2 = (id.cdsituacao ?? string.Empty).Trim();
            if (k1 != (entity.Cdtptabela ?? string.Empty).Trim() || k2 != (entity.Cdsituacao ?? string.Empty).Trim())
                return ApiResponse.Fail("Registro inválido (ID divergente).");

            if (!modelState.IsValid)
                return ApiResponse.Fail("Verifique os campos destacados.", BuildErrorDict(modelState));

            try
            {
                entity.Dcsituacao = (entity.Dcsituacao ?? string.Empty).Trim();
                entity.Flativoaux = entity.Ativo ? "S" : "N";
                _db.Taux2.Update(entity);
                await _db.SaveChangesAsync();

                return ApiResponse.Ok("Registro atualizado com sucesso!");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var exists = await _db.Taux2.AnyAsync(e => e.Cdtptabela == entity.Cdtptabela && e.Cdsituacao == entity.Cdsituacao);
                if (!exists)
                    return ApiResponse.Fail("Registro não encontrado.");

                _logger.LogError(ex, "Concorrência ao editar Taux2 {K1}-{K2}", k1, k2);
                return ApiResponse.Fail("O registro foi modificado por outro usuário. Recarregue a página.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao editar Taux2 {K1}-{K2}", k1, k2);
                return ApiResponse.Fail("Erro ao salvar as alterações. Tente novamente.");
            }
        }

        // ===== SAFE EDIT (token) =====
        public async Task<(ApiResponse resp, Taux2? entidade)> GetForSafeEditAsync(string token, string userId)
        {
            try
            {
                var (keys, purpose, tokenUser) = _rowToken.Unprotect<RowKeys>(token);
                if (purpose != PurposeEdit || tokenUser != userId)
                    return (ApiResponse.Fail("Token inválido."), null);

                var e = await _db.Taux2.FindAsync(keys.Cdtptabela, keys.Cdsituacao);
                if (e is null)
                    return (ApiResponse.Fail("Registro não encontrado."), null);

                return (ApiResponse.Ok(), e);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar token de edição em Taux2");
                return (ApiResponse.Fail("Falha ao validar token de edição."), null);
            }
        }

        // ===== DELETE BY TOKEN =====
        public async Task<ApiResponse> DeleteByTokenAsync(string token, string userId)
        {
            try
            {
                var (keys, purpose, tokenUser) = _rowToken.Unprotect<RowKeys>(token);
                if (purpose != PurposeDelete || tokenUser != userId)
                    return ApiResponse.Fail("Token inválido.");

                var entidade = await _db.Taux2.FindAsync(keys.Cdtptabela, keys.Cdsituacao);
                if (entidade is null)
                    return ApiResponse.Fail("Registro não encontrado.");

                _db.Taux2.Remove(entidade);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Taux2 excluído com sucesso: {K1}-{K2}", keys.Cdtptabela, keys.Cdsituacao);
                return ApiResponse.Ok("Excluído com sucesso.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco ao excluir Taux2 via token");
                return ApiResponse.Fail("Erro ao excluir. Verifique dependências.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao excluir Taux2 via token");
                return ApiResponse.Fail("Erro interno do servidor.");
            }
        }

        // ===== SUPORTE A DELETE/DETAILS tradicional =====
        public async Task<Taux2?> GetByIdAsync((string cdtptabela, string cdsituacao) id)
        {
            var k1 = (id.cdtptabela ?? string.Empty).Trim();
            var k2 = (id.cdsituacao ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(k1) || string.IsNullOrWhiteSpace(k2)) return null;

            return await _db.Taux2.AsNoTracking()
                                  .FirstOrDefaultAsync(x => x.Cdtptabela == k1 && x.Cdsituacao == k2);
        }

        public async Task<ApiResponse> DeleteByIdAsync((string cdtptabela, string cdsituacao) id)
        {
            try
            {
                var k1 = (id.cdtptabela ?? string.Empty).Trim();
                var k2 = (id.cdsituacao ?? string.Empty).Trim();
                var entidade = await _db.Taux2.FindAsync(k1, k2);
                if (entidade is null)
                    return ApiResponse.Fail("Registro não encontrado.");

                _db.Taux2.Remove(entidade);
                await _db.SaveChangesAsync();

                return ApiResponse.Ok("Registro excluído com sucesso!");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco ao excluir Taux2 {K1}-{K2}", id.cdtptabela, id.cdsituacao);
                return ApiResponse.Fail("Erro ao excluir. Verifique dependências.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao excluir Taux2 {K1}-{K2}", id.cdtptabela, id.cdsituacao);
                return ApiResponse.Fail("Erro interno do servidor.");
            }
        }

        // ===== HEALTH CHECK =====
        public async Task<ApiResponse<int>> HealthCheckAsync()
        {
            try
            {
                var count = await _db.Taux2.CountAsync();
                return ApiResponse<int>.Ok(count, "Conexão com banco OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no HealthCheck de Taux2");
                return ApiResponse<int>.Fail("Erro na conexão com o banco de dados");
            }
        }

        // ===== COMBOS =====
        public async Task<IEnumerable<SelectListItem>> GetTiposTabelaAsync()
        {
            return await _db.Taux1.AsNoTracking()
                .OrderBy(x => x.Cdtptabela)
                .Select(x => new SelectListItem { Value = x.Cdtptabela, Text = x.Cdtptabela + " - " + x.Dctabela })
                .ToListAsync();
        }
    }
}