using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RhSensoWeb.Common;              // ApiResponse
using RhSensoWeb.Data;
using RhSensoWeb.Models;              // Taux2
using RhSensoWeb.Services.Security;   // IRowTokenService
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Caching.Memory; // se ainda não tiver


namespace RhSensoWeb.Areas.SYS.Services
{
    public sealed class Taux2Service : ITaux2Service
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<Taux2Service> _logger;
        private readonly IRowTokenService _rowToken;

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

        private sealed record RowKeys(string Cdtptabela, string Cdsituacao);

        private const string PurposeEdit = "Edit";
        private const string PurposeDelete = "Delete";

        private readonly IMemoryCache _cache;  // cooldown por usuário

        // Locks e marcações por registro (chave = "cdtptabela|cdsituacao")
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _rowLocks = new();
        private static readonly ConcurrentDictionary<string, DateTimeOffset> _lastChange = new();

        private static string RowKeyOf(string k1, string k2) => $"{k1}|{k2}";
        private static SemaphoreSlim GetRowLock(string key) => _rowLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        // =======================
        // LISTAGEM
        // =======================
        public async Task<ApiResponse<IEnumerable<Taux2>>> GetDataAsync(string userId, string? cdtptabela = null)
        {
            try
            {
                IQueryable<Taux2> q = _db.Taux2.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(cdtptabela))
                {
                    var key = cdtptabela.Trim();
                    q = q.Where(x => x.Cdtptabela == key);
                }

                var rows = await q
                    .OrderBy(x => x.Cdtptabela)
                    .ThenBy(x => x.Noordem)
                    .ThenBy(x => x.Cdsituacao)
                    .ToListAsync();

                // Retorno no padrão ApiResponse genérico
                return ApiResponse<IEnumerable<Taux2>>.Ok(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar Taux2 (user={UserId}, filtro={Filtro})", userId, cdtptabela);
                return ApiResponse<IEnumerable<Taux2>>.Fail("Erro ao consultar a lista de situações.");
            }
        }

        // =======================
        // CREATE
        // =======================
        public async Task<ApiResponse> CreateAsync(Taux2 entity, ModelStateDictionary modelState)
        {
            if (!modelState.IsValid)
                return ApiResponse.Fail("Verifique os campos destacados.", BuildErrorDict(modelState));

            entity.Cdtptabela = (entity.Cdtptabela ?? string.Empty).Trim();
            entity.Cdsituacao = (entity.Cdsituacao ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(entity.Cdtptabela))
                return ApiResponse.Fail("Código Tipo Tabela é obrigatório.");

            if (string.IsNullOrWhiteSpace(entity.Cdsituacao))
                return ApiResponse.Fail("Código Situação é obrigatório.");

            var exists = await _db.Taux2.AsNoTracking()
                .AnyAsync(x => x.Cdtptabela == entity.Cdtptabela && x.Cdsituacao == entity.Cdsituacao);

            if (exists)
                return ApiResponse.Fail("Já existe uma situação com este Código para o Tipo de Tabela informado.");

            try
            {
                _db.Taux2.Add(entity);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Taux2 criado: {K1}-{K2}", entity.Cdtptabela, entity.Cdsituacao);
                return ApiResponse.Ok("Registro criado com sucesso.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco ao criar Taux2 {K1}-{K2}", entity.Cdtptabela, entity.Cdsituacao);
                return ApiResponse.Fail("Erro ao salvar o registro. Verifique dados e tente novamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar Taux2 {K1}-{K2}", entity.Cdtptabela, entity.Cdsituacao);
                return ApiResponse.Fail("Erro ao salvar o registro. Tente novamente.");
            }
        }


        public async Task<ApiResponse> UpdateAtivoAsync((string cdtptabela, string cdsituacao) id, bool ativo, string userId)
        {
            var k1 = (id.cdtptabela ?? string.Empty).Trim();
            var k2 = (id.cdsituacao ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(k1) || string.IsNullOrWhiteSpace(k2))
                return ApiResponse.Fail("ID do registro é obrigatório.");

            // 1) Exclusão mútua por registro (evita corrida simultânea no mesmo item)
            var rowKey = RowKeyOf(k1, k2);
            var gate = GetRowLock(rowKey);
            if (!await gate.WaitAsync(0))
                return ApiResponse.Fail("Outra alteração para este registro já está em andamento. Aguarde.");

            try
            {
                // 2) Janela mínima de 2s entre mudanças no MESMO registro
                var now = DateTimeOffset.UtcNow;
                if (_lastChange.TryGetValue(rowKey, out var last) && (now - last) < TimeSpan.FromSeconds(2))
                    return ApiResponse.Fail("Aguarde um instante antes de alterar novamente.");

                // 3) Cooldown leve por usuário (defesa extra, sem mexer no banco)
                var cooldownKey = $"SYS:Taux2:UpdateAtivo:{userId}:{rowKey}";
                if (_cache.TryGetValue(cooldownKey, out _))
                    return ApiResponse.Fail("Aguarde um instante antes de alterar novamente.");
                _cache.Set(cooldownKey, 1, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2)
                });

                // 4) Carrega e valida existência
                var entidade = await _db.Taux2.FirstOrDefaultAsync(x => x.Cdtptabela == k1 && x.Cdsituacao == k2);
                if (entidade is null)
                    return ApiResponse.Fail("Registro não encontrado.");

                // 5) Idempotência — não grava se já estiver no estado pedido
                if (entidade.Ativo == ativo)
                    return ApiResponse.Ok("Status já estava atualizado.");

                // 6) Aplica mudança (prop. Ativo mapeia “S/N” em flativoaux)
                entidade.Ativo = ativo;
                await _db.SaveChangesAsync();

                // 7) Marca último horário de mudança para este registro
                _lastChange[rowKey] = now;

                // 8) Log (apenas quando realmente mudou)
                _logger.LogInformation("Taux2.UpdateAtivo OK {K1}-{K2} => {V}", k1, k2, ativo ? "S" : "N");

                return ApiResponse.Ok(ativo ? "Situação ativada com sucesso!" : "Situação desativada com sucesso!");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco ao alterar Ativo em Taux2 {K1}-{K2}", k1, k2);
                return ApiResponse.Fail("Erro ao salvar o registro. Tente novamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao alterar Ativo em Taux2 {K1}-{K2}", k1, k2);
                return ApiResponse.Fail("Erro interno do servidor.");
            }
            finally
            {
                gate.Release();
            }
        }



        // =======================
        // EDIT
        // =======================
        public async Task<ApiResponse> EditAsync((string cdtptabela, string cdsituacao) id, Taux2 entity, ModelStateDictionary modelState)
        {
            var k1 = (id.cdtptabela ?? string.Empty).Trim();
            var k2 = (id.cdsituacao ?? string.Empty).Trim();

            if (k1 != (entity.Cdtptabela ?? string.Empty).Trim() ||
                k2 != (entity.Cdsituacao ?? string.Empty).Trim())
            {
                return ApiResponse.Fail("Registro inválido (ID divergente).");
            }

            if (!modelState.IsValid)
                return ApiResponse.Fail("Verifique os campos destacados.", BuildErrorDict(modelState));

            var dbEntity = await _db.Taux2
                .FirstOrDefaultAsync(x => x.Cdtptabela == k1 && x.Cdsituacao == k2);

            if (dbEntity is null)
                return ApiResponse.Fail("Registro não encontrado para alteração.");

            dbEntity.Dcsituacao = entity.Dcsituacao?.Trim() ?? "";
            dbEntity.Noordem = entity.Noordem;
            dbEntity.Flativoaux = (entity.Flativoaux ?? (entity.Ativo ? "S" : "N"))?.Trim();

            try
            {
                await _db.SaveChangesAsync();
                _logger.LogInformation("Taux2 editado: {K1}-{K2}", k1, k2);
                return ApiResponse.Ok("Registro alterado com sucesso.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco ao editar Taux2 {K1}-{K2}", k1, k2);
                return ApiResponse.Fail("Erro ao salvar o registro. Verifique os dados e tente novamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao editar Taux2 {K1}-{K2}", k1, k2);
                return ApiResponse.Fail("Erro ao salvar o registro. Tente novamente.");
            }
        }

        // =======================
        // SAFE EDIT por token
        // =======================
        public async Task<(ApiResponse resp, Taux2? entidade)> GetForSafeEditAsync(string token, string userId)
        {
            try
            {
                var unp = _rowToken.Unprotect<RowKeys>(token);
                if (!string.Equals(unp.Purpose, PurposeEdit, StringComparison.OrdinalIgnoreCase) || !string.Equals(unp.UserId, userId, StringComparison.OrdinalIgnoreCase))
                {
                    return (ApiResponse.Fail("Token inválido ou não pertence a este usuário."), null);
                }
                var k = unp.Payload;
                var entidade = await _db.Taux2.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Cdtptabela == k.Cdtptabela && x.Cdsituacao == k.Cdsituacao);

                if (entidade is null)
                    return (ApiResponse.Fail("Registro não encontrado."), null);

                return (ApiResponse.Ok(), entidade);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao validar token de edição (Taux2). user={User}", userId);
                return (ApiResponse.Fail("Não foi possível validar o registro."), null);
            }
        }

        // =======================
        // DELETE por token
        // =======================
        public async Task<ApiResponse> DeleteByTokenAsync(string token, string userId)
        {
            try
            {
                var unp = _rowToken.Unprotect<RowKeys>(token);
                if (!string.Equals(unp.Purpose, PurposeDelete, StringComparison.OrdinalIgnoreCase) || !string.Equals(unp.UserId, userId, StringComparison.OrdinalIgnoreCase))
                {
                    return ApiResponse.Fail("Token inválido ou não pertence a este usuário.");
                }
                var k = unp.Payload;
                var entidade = await _db.Taux2
                    .FirstOrDefaultAsync(x => x.Cdtptabela == k.Cdtptabela && x.Cdsituacao == k.Cdsituacao);

                if (entidade is null)
                    return ApiResponse.Fail("Registro não encontrado.");

                _db.Taux2.Remove(entidade);
                await _db.SaveChangesAsync();

                return ApiResponse.Ok("Registro excluído com sucesso.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao excluir registro por token (Taux2).", ex);
                return ApiResponse.Fail("Erro ao excluir. Tente novamente.");
            }
        }

        // =======================
        // GET BY ID
        // =======================
        public async Task<Taux2?> GetByIdAsync((string cdtptabela, string cdsituacao) id)
        {
            var k1 = (id.cdtptabela ?? string.Empty).Trim();
            var k2 = (id.cdsituacao ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(k1) || string.IsNullOrWhiteSpace(k2)) return null;
            return await _db.Taux2.AsNoTracking().FirstOrDefaultAsync(x => x.Cdtptabela == k1 && x.Cdsituacao == k2);
        }

        // =======================
        // DELETE BY ID (via form)
        // =======================
        public async Task<ApiResponse> DeleteByIdAsync((string cdtptabela, string cdsituacao) id)
        {
            var k1 = (id.cdtptabela ?? string.Empty).Trim();
            var k2 = (id.cdsituacao ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(k1) || string.IsNullOrWhiteSpace(k2))
                return ApiResponse.Fail("ID inválido.");

            try
            {
                var entidade = await _db.Taux2.FirstOrDefaultAsync(x => x.Cdtptabela == k1 && x.Cdsituacao == k2);
                if (entidade is null)
                    return ApiResponse.Fail("Registro não encontrado.");

                _db.Taux2.Remove(entidade);
                await _db.SaveChangesAsync();

                return ApiResponse.Ok("Registro excluído com sucesso!");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco ao excluir Taux2 {K1}-{K2}", k1, k2);
                return ApiResponse.Fail("Erro ao excluir. Verifique dependências.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao excluir Taux2 {K1}-{K2}", k1, k2);
                return ApiResponse.Fail("Erro interno do servidor.");
            }
        }

        // =======================
        // HEALTH CHECK
        // =======================
        public async Task<ApiResponse<int>> HealthCheckAsync()
        {
            try
            {
                var count = await _db.Taux2.AsNoTracking().CountAsync();
                return ApiResponse<int>.Ok(count, "Conexão com banco OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no HealthCheck de Taux2");
                return ApiResponse<int>.Fail("Erro na conexão com o banco de dados");
            }
        }

        // =======================
        // COMBOS
        // =======================
        public async Task<IEnumerable<SelectListItem>> GetTiposTabelaAsync()
        {
            return await _db.Taux1.AsNoTracking()
                .OrderBy(x => x.Cdtptabela)
                .Select(x => new SelectListItem
                {
                    Value = x.Cdtptabela,
                    Text = x.Cdtptabela + " - " + x.Dctabela
                })
                .ToListAsync();
        }

        // =======================
        // Helpers
        // =======================
        private static IDictionary<string, string[]> BuildErrorDict(ModelStateDictionary modelState)
            => modelState.Where(kv => kv.Value?.Errors.Count > 0)
                         .ToDictionary(
                             kv => kv.Key,
                             kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
    }
}