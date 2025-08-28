// Areas/SEG/Services/TsistemaService.cs
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RhSensoWeb.Areas.SEG.DTOs;
using RhSensoWeb.Common;
using RhSensoWeb.Common.Tokens;          // << NEW: RowKey genérico
using RhSensoWeb.Data;
using RhSensoWeb.Models;
using RhSensoWeb.Services.Security;
using RhSensoWeb.Support;

namespace RhSensoWeb.Areas.SEG.Services
{
    public sealed class TsistemaService : ITsistemaService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<TsistemaService> _logger;
        private readonly IRowTokenService _rowToken;
        private readonly IMemoryCache _cache;

        private const string PurposeEdit = "Edit";
        private const string PurposeDelete = "Delete";

        public TsistemaService(
            ApplicationDbContext db,
            ILogger<TsistemaService> logger,
            IRowTokenService rowToken,
            IMemoryCache cache)
        {
            _db = db;
            _logger = logger;
            _rowToken = rowToken;
            _cache = cache;
        }

        // ===== LISTAGEM PARA DATATABLES =====
        public async Task<ApiResponse<IEnumerable<TsistemaListItemDto>>> GetDataAsync(string userId)
        {
            try
            {
                var rows = await _db.Tsistema
                    .AsNoTracking()
                    .OrderBy(x => x.Cdsistema)
                    .Select(x => new { x.Cdsistema, x.Dcsistema, x.Ativo })
                    .ToListAsync();

                var data = rows.Select(r =>
                {
                    var id = (r.Cdsistema ?? string.Empty).Trim();
                    return new TsistemaListItemDto(
                        cdsistema: id,
                        dcsistema: r.Dcsistema ?? "",
                        ativo: r.Ativo,
                        editToken: _rowToken.Protect(new RowKey(id), PurposeEdit, userId, TimeSpan.FromMinutes(10)),
                        deleteToken: _rowToken.Protect(new RowKey(id), PurposeDelete, userId, TimeSpan.FromMinutes(10))
                    );
                });

                return ApiResponse<IEnumerable<TsistemaListItemDto>>.Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter dados de Tsistema");
                return ApiResponse<IEnumerable<TsistemaListItemDto>>.Fail("Erro ao carregar dados do servidor.");
            }
        }

        // ===== TOGGLE ATIVO =====
        public async Task<ApiResponse> UpdateAtivoAsync(string id, bool ativo, string userId)
        {
            try
            {
                id = (id ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(id))
                    return ApiResponse.Fail("ID do sistema é obrigatório.");

                // Cooldown (flip-flop)
                var cooldownKey = $"SEG:Tsistema:UpdateAtivo:{userId}:{id}";
                if (_cache.TryGetValue(cooldownKey, out _))
                    return ApiResponse.Fail("Aguarde um instante antes de alterar novamente.");

                _cache.Set(cooldownKey, 1, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2)
                });

                var entidade = await _db.Tsistema.FirstOrDefaultAsync(x => x.Cdsistema == id);
                if (entidade is null)
                    return ApiResponse.Fail("Sistema não encontrado.");

                if (entidade.Ativo == ativo)
                    return ApiResponse.Ok("Status já estava atualizado.");

                entidade.Ativo = ativo;
                await _db.SaveChangesAsync();

                return ApiResponse.Ok(ativo ? "Sistema ativado com sucesso!" : "Sistema desativado com sucesso!");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concorrência ao atualizar Ativo em Tsistema {Id}", id);
                return ApiResponse.Fail("Registro modificado por outro usuário. Recarregue a página.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco ao atualizar Ativo em Tsistema {Id}", id);
                return ApiResponse.Fail("Erro ao salvar no banco de dados. Tente novamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao atualizar Ativo em Tsistema {Id}", id);
                return ApiResponse.Fail("Erro interno do servidor.");
            }
        }

        // ===== CREATE =====
        public async Task<ApiResponse> CreateAsync(Tsistema sistema, ModelStateDictionary modelState)
        {
            if (!modelState.IsValid)
                return ApiResponse.Fail("Verifique os campos destacados.", modelState.ToErrorsDictionary()); // << FIX

            try
            {
                var id = (sistema.Cdsistema ?? "").Trim();
                var existe = await _db.Tsistema.AsNoTracking().AnyAsync(x => x.Cdsistema == id);
                if (existe)
                {
                    modelState.AddModelError(nameof(Tsistema.Cdsistema), "Já existe um sistema com este código.");
                    return ApiResponse.Fail("Já existe um sistema com este código.", modelState.ToErrorsDictionary()); // << FIX
                }

                sistema.Cdsistema = id; // normaliza
                _db.Tsistema.Add(sistema);
                await _db.SaveChangesAsync();

                return ApiResponse.Ok("Sistema criado com sucesso!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar Tsistema {Id}", sistema.Cdsistema);
                return ApiResponse.Fail("Erro ao salvar o sistema. Tente novamente.");
            }
        }

        // ===== EDIT =====
        public async Task<ApiResponse> EditAsync(string id, Tsistema sistema, ModelStateDictionary modelState)
        {
            id = (id ?? "").Trim();
            if (id != (sistema.Cdsistema ?? "").Trim())
                return ApiResponse.Fail("Registro inválido (ID divergente).");

            if (!modelState.IsValid)
                return ApiResponse.Fail("Verifique os campos destacados.", modelState.ToErrorsDictionary()); // << FIX

            try
            {
                _db.Tsistema.Update(sistema);
                await _db.SaveChangesAsync();
                return ApiResponse.Ok("Sistema atualizado com sucesso!");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var exists = await _db.Tsistema.AnyAsync(e => e.Cdsistema == sistema.Cdsistema);
                if (!exists)
                    return ApiResponse.Fail("Registro não encontrado.");

                _logger.LogError(ex, "Concorrência ao editar Tsistema {Id}", id);
                return ApiResponse.Fail("O registro foi modificado por outro usuário. Recarregue a página.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao editar Tsistema {Id}", id);
                return ApiResponse.Fail("Erro ao salvar as alterações. Tente novamente.");
            }
        }

        // ===== SAFE EDIT (token opaco) =====
        public async Task<(ApiResponse resp, Tsistema? entidade)> GetForSafeEditAsync(string token, string userId)
        {
            try
            {
                var (keys, purpose, tokenUser) = _rowToken.Unprotect<RowKey>(token); // << RowKey genérico
                if (purpose != PurposeEdit || tokenUser != userId)
                    return (ApiResponse.Fail("Token inválido."), null);

                var sistema = await _db.Tsistema.FindAsync(keys.Id); // << usa Id
                if (sistema is null)
                    return (ApiResponse.Fail("Sistema não encontrado."), null);

                return (ApiResponse.Ok(), sistema);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar token de edição em Tsistema");
                return (ApiResponse.Fail("Falha ao validar token de edição."), null);
            }
        }

        // ===== DELETE BY TOKEN =====
        public async Task<ApiResponse> DeleteByTokenAsync(string token, string userId)
        {
            try
            {
                var (keys, purpose, tokenUser) = _rowToken.Unprotect<RowKey>(token); // << RowKey genérico
                if (purpose != PurposeDelete || tokenUser != userId)
                    return ApiResponse.Fail("Token inválido.");

                var sistema = await _db.Tsistema.FindAsync(keys.Id); // << usa Id
                if (sistema is null)
                    return ApiResponse.Fail("Sistema não encontrado.");

                _db.Tsistema.Remove(sistema);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Sistema excluído com sucesso: {Id}", keys.Id);
                return ApiResponse.Ok("Excluído com sucesso.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco ao excluir Tsistema via token");
                return ApiResponse.Fail("Erro ao excluir. Verifique dependências.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao excluir Tsistema via token");
                return ApiResponse.Fail("Erro interno do servidor.");
            }
        }

        // ===== HEALTH CHECK =====
        public async Task<ApiResponse<int>> HealthCheckAsync()
        {
            try
            {
                var count = await _db.Tsistema.CountAsync();
                return ApiResponse<int>.Ok(count, "Conexão com banco OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no HealthCheck de Tsistema");
                return ApiResponse<int>.Fail("Erro na conexão com o banco de dados");
            }
        }
    }
}
