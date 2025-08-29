using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RhSensoWeb.Common;
using RhSensoWeb.Common.Tokens;         // << usa RowKey genérico
using RhSensoWeb.Data;
using RhSensoWeb.Models;                 // Tuse1
using RhSensoWeb.Services.Security;      // IRowTokenService
using RhSensoWeb.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace RhSensoWeb.Areas.SEG.Services
{
    public sealed class UsuarioService : IUsuarioService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UsuarioService> _logger;
        private readonly IRowTokenService _rowToken;
        private readonly IMemoryCache _cache;

        private const string PurposeEdit = "Edit";
        private const string PurposeDelete = "Delete";


        // === Locks por registro e controle de intervalo mínimo ===
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _rowLocks = new();
        private static readonly ConcurrentDictionary<string, DateTimeOffset> _lastChange = new();

        private static SemaphoreSlim GetRowLock(string key)
            => _rowLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));


        public UsuarioService(
            ApplicationDbContext db,
            ILogger<UsuarioService> logger,
            IRowTokenService rowToken,
            IMemoryCache cache)
        {
            _db = db;
            _logger = logger;
            _rowToken = rowToken;
            _cache = cache;
        }

        // ===== LISTAGEM PARA DATATABLES =====
        public async Task<ApiResponse<IEnumerable<DTOs.UsuarioListItemDto>>> GetDataAsync(string userId)
        {
            try
            {
                var rows = await _db.Tuse1
                    .AsNoTracking()
                    .OrderBy(x => x.Cdusuario)
                    .Select(x => new { x.Cdusuario, x.Dcusuario, x.Email_usuario, x.Tpusuario, x.Ativo })
                    .ToListAsync();

                var data = rows.Select(r =>
                {
                    var id = (r.Cdusuario ?? string.Empty).Trim();
                    var tipoDesc = (r.Tpusuario?.ToString() == "1") ? "Empregado" : "Terceiro";

                    return new DTOs.UsuarioListItemDto(
                        cdusuario: id,
                        dcusuario: r.Dcusuario ?? string.Empty,
                        email_usuario: r.Email_usuario ?? string.Empty,
                        tpusuario: r.Tpusuario?.ToString() ?? string.Empty,
                        tipo_desc: tipoDesc,
                        ativo: r.Ativo,
                        editToken: _rowToken.Protect(new RowKey(id), PurposeEdit, userId, TimeSpan.FromMinutes(10)),
                        deleteToken: _rowToken.Protect(new RowKey(id), PurposeDelete, userId, TimeSpan.FromMinutes(10))
                    );
                });

                return ApiResponse<IEnumerable<DTOs.UsuarioListItemDto>>.Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter dados de Usuario");
                return ApiResponse<IEnumerable<DTOs.UsuarioListItemDto>>.Fail("Erro ao carregar dados do servidor.");
            }
        }

        // ===== TOGGLE ATIVO =====
        public async Task<ApiResponse> UpdateAtivoAsync(string id, bool ativo, string userId)
        {
            try
            {
                id = (id ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(id))
                    return ApiResponse.Fail("ID do usuário é obrigatório.");

                // === Exclusão mútua por linha (evita corrida de requisições simultâneas) ===
                var rowKey = id; // trave por ID do usuário
                var gate = GetRowLock(rowKey);
                if (!await gate.WaitAsync(0))
                    return ApiResponse.Fail("Outra alteração para este usuário já está em andamento. Aguarde.");

                try
                {
                    // === Intervalo mínimo (ex.: 2s) entre mudanças no MESMO registro ===
                    var now = DateTimeOffset.UtcNow;
                    if (_lastChange.TryGetValue(rowKey, out var last) && (now - last) < TimeSpan.FromSeconds(2))
                        return ApiResponse.Fail("Aguarde um instante antes de alterar novamente.");

                    // Cooldown leve em memória (extra; pode manter)
                    var cooldownKey = $"SEG:Usuario:UpdateAtivo:{userId}:{id}";
                    if (_cache.TryGetValue(cooldownKey, out _))
                        return ApiResponse.Fail("Aguarde um instante antes de alterar novamente.");

                    _cache.Set(cooldownKey, 1, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2)
                    });

                    var entidade = await _db.Tuse1.FirstOrDefaultAsync(x => x.Cdusuario == id);
                    if (entidade is null)
                        return ApiResponse.Fail("Usuário não encontrado.");

                    // Idempotência — não grava se já estiver no estado pedido
                    if (entidade.Ativo == ativo)
                        return ApiResponse.Ok("Status já estava atualizado.");

                    entidade.Ativo = ativo;
                    await _db.SaveChangesAsync();

                    // Marca o horário da última mudança para este registro
                    _lastChange[rowKey] = now;

                    // Log apenas quando realmente mudou
                    _logger.LogInformation("Usuario.UpdateAtivo OK {@info}", new
                    {
                        Entidade = "tuse1",
                        Id = id,
                        Campo = "flativo",
                        Valor = ativo ? "S" : "N",
                        Usuario = userId
                    });

                    return ApiResponse.Ok(ativo ? "Usuário ativado com sucesso!" : "Usuário desativado com sucesso!");
                }
                finally
                {
                    gate.Release();
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concorrência ao atualizar Ativo em Usuario {Id}", id);
                return ApiResponse.Fail("Registro modificado por outro usuário. Recarregue a página.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco ao atualizar Ativo em Usuario {Id}", id);
                return ApiResponse.Fail("Erro ao salvar no banco de dados. Tente novamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao atualizar Ativo em Usuario {Id}", id);
                return ApiResponse.Fail("Erro interno do servidor.");
            }
        }




        // ===== CREATE =====
        public async Task<ApiResponse> CreateAsync(Tuse1 usuario, ModelStateDictionary modelState)
        {
            if (!modelState.IsValid)
                return ApiResponse.Fail("Verifique os campos destacados.", modelState.ToErrorsDictionary()); // << FIX

            try
            {
                var id = (usuario.Cdusuario ?? string.Empty).Trim();
                var existe = await _db.Tuse1.AsNoTracking().AnyAsync(x => x.Cdusuario == id);
                if (existe)
                {
                    modelState.AddModelError(nameof(Tuse1.Cdusuario), "Já existe um usuário com este código.");
                    return ApiResponse.Fail("Já existe um usuário com este código.", modelState.ToErrorsDictionary()); // << FIX
                }

                usuario.Cdusuario = id; // normaliza
                _db.Tuse1.Add(usuario);
                await _db.SaveChangesAsync();

                return ApiResponse.Ok("Usuário criado com sucesso!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar Usuario {Id}", usuario.Cdusuario);
                return ApiResponse.Fail("Erro ao salvar o usuário. Tente novamente.");
            }
        }

        // ===== EDIT =====
        public async Task<ApiResponse> EditAsync(string id, Tuse1 usuario, ModelStateDictionary modelState)
        {
            id = (id ?? string.Empty).Trim();
            if (id != (usuario.Cdusuario ?? string.Empty).Trim())
                return ApiResponse.Fail("Registro inválido (ID divergente).");

            if (!modelState.IsValid)
                return ApiResponse.Fail("Verifique os campos destacados.", modelState.ToErrorsDictionary()); // << FIX

            try
            {
                _db.Tuse1.Update(usuario);
                await _db.SaveChangesAsync();
                return ApiResponse.Ok("Usuário atualizado com sucesso!");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var exists = await _db.Tuse1.AnyAsync(e => e.Cdusuario == usuario.Cdusuario);
                if (!exists)
                    return ApiResponse.Fail("Registro não encontrado.");

                _logger.LogError(ex, "Concorrência ao editar Usuario {Id}", id);
                return ApiResponse.Fail("O registro foi modificado por outro usuário. Recarregue a página.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao editar Usuario {Id}", id);
                return ApiResponse.Fail("Erro ao salvar as alterações. Tente novamente.");
            }
        }

        // ===== SAFE EDIT (token opaco) =====
        public async Task<(ApiResponse resp, Tuse1? entidade)> GetForSafeEditAsync(string token, string userId)
        {
            try
            {
                var (keys, purpose, tokenUser) = _rowToken.Unprotect<RowKey>(token); // << RowKey
                if (purpose != PurposeEdit || tokenUser != userId)
                    return (ApiResponse.Fail("Token inválido."), null);

                var entidade = await _db.Tuse1.FindAsync(keys.Id); // << usa Id
                if (entidade is null)
                    return (ApiResponse.Fail("Usuário não encontrado."), null);

                return (ApiResponse.Ok(), entidade);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar token de edição em Usuario");
                return (ApiResponse.Fail("Falha ao validar token de edição."), null);
            }
        }

        // ===== DELETE BY TOKEN =====
        public async Task<ApiResponse> DeleteByTokenAsync(string token, string userId)
        {
            try
            {
                var (keys, purpose, tokenUser) = _rowToken.Unprotect<RowKey>(token); // << RowKey
                if (purpose != PurposeDelete || tokenUser != userId)
                    return ApiResponse.Fail("Token inválido.");

                var entidade = await _db.Tuse1.FindAsync(keys.Id); // << usa Id
                if (entidade is null)
                    return ApiResponse.Fail("Usuário não encontrado.");

                _db.Tuse1.Remove(entidade);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Usuário excluído com sucesso: {Id}", keys.Id);
                return ApiResponse.Ok("Excluído com sucesso.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco ao excluir Usuario via token");
                return ApiResponse.Fail("Erro ao excluir. Verifique dependências.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao excluir Usuario via token");
                return ApiResponse.Fail("Erro interno do servidor.");
            }
        }

        // ===== SUPORTE A DELETE/DETAILS tradicional =====
        public async Task<Tuse1?> GetByIdAsync(string id)
        {
            id = (id ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id)) return null;
            return await _db.Tuse1.AsNoTracking().FirstOrDefaultAsync(x => x.Cdusuario == id);
        }

        public async Task<ApiResponse> DeleteByIdAsync(string id)
        {
            try
            {
                id = (id ?? string.Empty).Trim();
                var entidade = await _db.Tuse1.FindAsync(id);
                if (entidade is null)
                    return ApiResponse.Fail("Usuário não encontrado.");

                _db.Tuse1.Remove(entidade);
                await _db.SaveChangesAsync();
                return ApiResponse.Ok("Usuário excluído com sucesso!");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco ao excluir Usuario {Id}", id);
                return ApiResponse.Fail("Erro ao excluir. Verifique dependências.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao excluir Usuario {Id}", id);
                return ApiResponse.Fail("Erro interno do servidor.");
            }
        }

        // ===== HEALTH CHECK =====
        public async Task<ApiResponse<int>> HealthCheckAsync()
        {
            try
            {
                var count = await _db.Tuse1.CountAsync();
                return ApiResponse<int>.Ok(count, "Conexão com banco OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no HealthCheck de Usuario");
                return ApiResponse<int>.Fail("Erro na conexão com o banco de dados");
            }
        }
    }
}
