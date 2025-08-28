// Areas/SYS/Services/Taux1Service.cs
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RhSensoWeb.Common;
using RhSensoWeb.Data;
using RhSensoWeb.Models;                 // Taux1
using RhSensoWeb.Services.Security;      // IRowTokenService

namespace RhSensoWeb.Areas.SYS.Services
{
    public sealed class Taux1Service : ITaux1Service
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<Taux1Service> _logger;
        private readonly IRowTokenService _rowToken;
        private readonly IMemoryCache _cache;

        private const string PurposeEdit = "Edit";
        private const string PurposeDelete = "Delete";

        public Taux1Service(
            ApplicationDbContext db,
            ILogger<Taux1Service> logger,
            IRowTokenService rowToken,
            IMemoryCache cache)
        {
            _db = db;
            _logger = logger;
            _rowToken = rowToken;
            _cache = cache;
        }

        // payload para os tokens opacos
        public sealed record RowKeys(string Cdtptabela);

        // ===== LISTAGEM (DataTables) =====
        public async Task<ApiResponse<IEnumerable<Taux1>>> GetDataAsync(string userId)
        {
            try
            {
                var rows = await _db.Taux1
                    .AsNoTracking()
                    .OrderBy(x => x.Cdtptabela)
                    .ToListAsync();

                return ApiResponse<IEnumerable<Taux1>>.Ok(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter dados de Taux1");
                return ApiResponse<IEnumerable<Taux1>>.Fail("Erro ao carregar dados do servidor.");
            }
        }

        // ===== CREATE =====
        public async Task<ApiResponse> CreateAsync(Taux1 entity, ModelStateDictionary modelState)
        {
            if (!modelState.IsValid)
                return ApiResponse.Fail("Verifique os campos destacados.", modelState.ToErrorDictionary());

            try
            {
                var id = (entity.Cdtptabela ?? string.Empty).Trim();

                var exists = await _db.Taux1.AsNoTracking().AnyAsync(x => x.Cdtptabela == id);
                if (exists)
                {
                    modelState.AddModelError(nameof(Taux1.Cdtptabela), "Já existe um registro com este código.");
                    return ApiResponse.Fail("Já existe um registro com este código.", modelState.ToErrorDictionary());
                }

                entity.Cdtptabela = id; // normaliza PK
                entity.Dctabela = (entity.Dctabela ?? string.Empty).Trim();

                _db.Taux1.Add(entity);
                await _db.SaveChangesAsync();

                return ApiResponse.Ok("Registro criado com sucesso!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar Taux1 {Id}", entity.Cdtptabela);
                return ApiResponse.Fail("Erro ao salvar o registro. Tente novamente.");
            }
        }

        // ===== EDIT =====
        public async Task<ApiResponse> EditAsync(string id, Taux1 entity, ModelStateDictionary modelState)
        {
            id = (id ?? string.Empty).Trim();
            if (id != (entity.Cdtptabela ?? string.Empty).Trim())
                return ApiResponse.Fail("Registro inválido (ID divergente).");

            if (!modelState.IsValid)
                return ApiResponse.Fail("Verifique os campos destacados.", modelState.ToErrorDictionary());

            try
            {
                entity.Dctabela = (entity.Dctabela ?? string.Empty).Trim();
                _db.Taux1.Update(entity);
                await _db.SaveChangesAsync();

                return ApiResponse.Ok("Registro atualizado com sucesso!");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var exists = await _db.Taux1.AnyAsync(e => e.Cdtptabela == entity.Cdtptabela);
                if (!exists)
                    return ApiResponse.Fail("Registro não encontrado.");

                _logger.LogError(ex, "Concorrência ao editar Taux1 {Id}", id);
                return ApiResponse.Fail("O registro foi modificado por outro usuário. Recarregue a página.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao editar Taux1 {Id}", id);
                return ApiResponse.Fail("Erro ao salvar as alterações. Tente novamente.");
            }
        }

        // ===== SAFE EDIT (token opaco) =====
        public async Task<(ApiResponse resp, Taux1? entidade)> GetForSafeEditAsync(string token, string userId)
        {
            try
            {
                var (keys, purpose, tokenUser) = _rowToken.Unprotect<RowKeys>(token);
                if (purpose != PurposeEdit || tokenUser != userId)
                    return (ApiResponse.Fail("Token inválido."), null);

                var e = await _db.Taux1.FindAsync(keys.Cdtptabela);
                if (e is null)
                    return (ApiResponse.Fail("Registro não encontrado."), null);

                return (ApiResponse.Ok(), e);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar token de edição em Taux1");
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

                var entidade = await _db.Taux1.FindAsync(keys.Cdtptabela);
                if (entidade is null)
                    return ApiResponse.Fail("Registro não encontrado.");

                _db.Taux1.Remove(entidade);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Taux1 excluído com sucesso: {Id}", keys.Cdtptabela);
                return ApiResponse.Ok("Excluído com sucesso.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco ao excluir Taux1 via token");
                return ApiResponse.Fail("Erro ao excluir. Verifique dependências.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao excluir Taux1 via token");
                return ApiResponse.Fail("Erro interno do servidor.");
            }
        }

        // ===== SUPORTE A DELETE/DETAILS tradicional =====
        public async Task<Taux1?> GetByIdAsync(string id)
        {
            id = (id ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id)) return null;

            return await _db.Taux1.AsNoTracking()
                                  .FirstOrDefaultAsync(x => x.Cdtptabela == id);
        }

        public async Task<ApiResponse> DeleteByIdAsync(string id)
        {
            try
            {
                id = (id ?? string.Empty).Trim();
                var entidade = await _db.Taux1.FindAsync(id);
                if (entidade is null)
                    return ApiResponse.Fail("Registro não encontrado.");

                _db.Taux1.Remove(entidade);
                await _db.SaveChangesAsync();

                return ApiResponse.Ok("Registro excluído com sucesso!");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco ao excluir Taux1 {Id}", id);
                return ApiResponse.Fail("Erro ao excluir. Verifique dependências.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao excluir Taux1 {Id}", id);
                return ApiResponse.Fail("Erro interno do servidor.");
            }
        }

        // ===== HEALTH CHECK =====
        public async Task<ApiResponse<int>> HealthCheckAsync()
        {
            try
            {
                var count = await _db.Taux1.CountAsync();
                return ApiResponse<int>.Ok(count, "Conexão com banco OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no HealthCheck de Taux1");
                return ApiResponse<int>.Fail("Erro na conexão com o banco de dados");
            }
        }
    }
}
