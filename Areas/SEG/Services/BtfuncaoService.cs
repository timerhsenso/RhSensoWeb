using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RhSensoWeb.Common;
using RhSensoWeb.Data;
using RhSensoWeb.Models;
using RhSensoWeb.Services.Security; // IRowTokenService
using RhSensoWeb.Support;           // ModelStateExtensions + RowTokenServiceExtensions

namespace RhSensoWeb.Areas.SEG.Services
{
    public sealed class BtfuncaoService : IBtfuncaoService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<BtfuncaoService> _logger;
        private readonly IRowTokenService _rowToken;

        private sealed record RowKeys(string Cdsistema, string Cdfuncao, string Nmbotao);

        public BtfuncaoService(
            ApplicationDbContext db,
            ILogger<BtfuncaoService> logger,
            IRowTokenService rowToken)
        {
            _db = db;
            _logger = logger;
            _rowToken = rowToken;
        }

        public async Task<(ApiResponse resp, IEnumerable<Btfuncao> data)> GetDataAsync(string userId)
        {
            var list = await _db.Btfuncao
                .AsNoTracking()
                .OrderBy(x => x.Cdsistema)
                .ThenBy(x => x.Cdfuncao)
                .ThenBy(x => x.Nmbotao)
                .ToListAsync();

            // Como seu ApiResponse não tem Data, devolvo dados na tupla.
            return (ApiResponse.Ok("OK"), list);
        }

        public async Task<(ApiResponse resp, Btfuncao? entidade)> GetForSafeEditAsync(string token, string userId)
        {
            if (!_rowToken.TryUnprotect(token, "Edit", userId, out RowKeys? keys) || keys is null)
                return (ApiResponse.Fail("Token inválido ou expirado."), null);

            var e = await FindAsync(keys.Cdsistema, keys.Cdfuncao, keys.Nmbotao, asNoTracking: true);
            return e is null
                ? (ApiResponse.Fail("Registro não encontrado."), null)
                : (ApiResponse.Ok("OK"), e);
        }

        public async Task<ApiResponse> CreateAsync(Btfuncao entidade, ModelStateDictionary modelState)
        {
            if (!modelState.IsValid)
                return ApiResponse.Fail("Verifique os dados informados.", modelState.ToErrorsDictionary());

            NormalizeKeys(entidade);

            var exists = await _db.Btfuncao.AsNoTracking().AnyAsync(x =>
                x.Cdsistema == entidade.Cdsistema &&
                x.Cdfuncao == entidade.Cdfuncao &&
                x.Nmbotao == entidade.Nmbotao);

            if (exists)
            {
                modelState.AddModelError(string.Empty, "Já existe um registro com essa combinação (Sistema, Função, Botão).");
                return ApiResponse.Fail("Registro duplicado.", modelState.ToErrorsDictionary());
            }

            _db.Btfuncao.Add(entidade);
            await _db.SaveChangesAsync();
            return ApiResponse.Ok("Criado com sucesso.");
        }

        public async Task<ApiResponse> EditAsync((string cds, string cdf, string nmb) id, Btfuncao entidade, ModelStateDictionary modelState)
        {
            if (!modelState.IsValid)
                return ApiResponse.Fail("Verifique os dados informados.", modelState.ToErrorsDictionary());

            NormalizeKeys(entidade);

            if (!KeyEquals(id, (entidade.Cdsistema, entidade.Cdfuncao, entidade.Nmbotao)))
                return ApiResponse.Fail("Chave do registro alterada. Edição não permitida.");

            var dbEntity = await FindAsync(id.cds, id.cdf, id.nmb, asNoTracking: false);
            if (dbEntity is null)
                return ApiResponse.Fail("Registro não encontrado.");

            dbEntity.Dcbotao = entidade.Dcbotao?.Trim() ?? "";
            dbEntity.Cdacao = entidade.Cdacao?.Trim() ?? "";

            try
            {
                await _db.SaveChangesAsync();
                return ApiResponse.Ok("Alterado com sucesso.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro ao editar Btfuncao {@Key}", id);
                return ApiResponse.Fail("Não foi possível salvar as alterações.");
            }
        }

        public async Task<ApiResponse> DeleteByTokenAsync(string token, string userId)
        {
            if (!_rowToken.TryUnprotect(token, "Delete", userId, out RowKeys? keys) || keys is null)
                return ApiResponse.Fail("Token inválido ou expirado.");

            var e = await FindAsync(keys.Cdsistema, keys.Cdfuncao, keys.Nmbotao, asNoTracking: false);
            if (e is null)
                return ApiResponse.Fail("Registro não encontrado.");

            _db.Btfuncao.Remove(e);

            try
            {
                await _db.SaveChangesAsync();
                return ApiResponse.Ok("Excluído com sucesso.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Falha ao excluir Btfuncao {@Key}", keys);
                return ApiResponse.Fail("Não foi possível excluir. Verifique dependências.");
            }
        }

        public async Task<ApiResponse> HealthCheckAsync()
        {
            var total = await _db.Btfuncao.AsNoTracking().CountAsync();
            // Seu ApiResponse.Ok não aceita 2 parâmetros. Coloco a info na mensagem.
            return ApiResponse.Ok($"OK | total={total}");
        }

        // ---------- privados ----------
        private static void NormalizeKeys(Btfuncao e)
        {
            e.Cdsistema = (e.Cdsistema ?? "").Trim();
            e.Cdfuncao = (e.Cdfuncao ?? "").Trim();
            e.Nmbotao = (e.Nmbotao ?? "").Trim();
        }

        private static bool KeyEquals((string cds, string cdf, string nmb) a, (string cds, string cdf, string nmb) b) =>
            string.Equals(a.cds, b.cds, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.cdf, b.cdf, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.nmb, b.nmb, StringComparison.OrdinalIgnoreCase);

        private async Task<Btfuncao?> FindAsync(string cds, string cdf, string nmb, bool asNoTracking)
        {
            var q = _db.Btfuncao.Where(x =>
                x.Cdsistema == cds &&
                x.Cdfuncao == cdf &&
                x.Nmbotao == nmb);

            if (asNoTracking) q = q.AsNoTracking();
            return await q.FirstOrDefaultAsync();
        }
    }
}
