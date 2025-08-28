using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RhSensoWeb.Areas.SYS.Taux1.DTOs;
using RhSensoWeb.Areas.SYS.Taux1.Repositories;

namespace RhSensoWeb.Areas.SYS.Taux1.Services
{
    public class Taux1Service : ITaux1Service
    {
        private readonly ITaux1Repository _repo;

        public Taux1Service(ITaux1Repository repo)
        {
            _repo = repo;
        }

        public async Task<(IEnumerable<Taux1Dto> items, int total, int filtered)> GetPageAsync(DataTableRequest req)
        {
            var q = _repo.Query();

            var total = await q.CountAsync();

            if (!string.IsNullOrWhiteSpace(req.Search))
            {
                var s = req.Search.Trim();
                q = q.Where(x =>
                    (x.Cdtptabela != null && x.Cdtptabela.Contains(s)) ||
                    (x.Dctabela != null && x.Dctabela.Contains(s))
                );
            }

            // Ordenação simples (por nome de coluna)
            if (!string.IsNullOrWhiteSpace(req.OrderColumn))
            {
                var col = req.OrderColumn.ToLowerInvariant();
                var dir = (req.OrderDir ?? "asc").ToLowerInvariant();

                q = (col) switch
                {
                    "cdtptabela" => (dir == "desc") ? q.OrderByDescending(x => x.Cdtptabela) : q.OrderBy(x => x.Cdtptabela),
                    "dctabela"   => (dir == "desc") ? q.OrderByDescending(x => x.Dctabela)   : q.OrderBy(x => x.Dctabela),
                    _ => q.OrderBy(x => x.Cdtptabela)
                };
            }
            else
            {
                q = q.OrderBy(x => x.Cdtptabela);
            }

            var filtered = await q.CountAsync();

            var page = await q.Skip(req.Start).Take(req.Length > 0 ? req.Length : 10)
                .Select(x => new Taux1Dto
                {
                    Cdtptabela = x.Cdtptabela!,
                    Dctabela = x.Dctabela!
                })
                .ToListAsync();

            return (page, total, filtered);
        }

        public async Task<Taux1Dto?> GetAsync(string id)
        {
            var e = await _repo.GetByIdAsync(id);
            if (e == null) return null;
            return new Taux1Dto
            {
                Cdtptabela = e.Cdtptabela,
                Dctabela = e.Dctabela
            };
        }

        public async Task CreateAsync(Taux1Dto dto)
        {
            if (await _repo.ExistsAsync(dto.Cdtptabela))
                throw new InvalidOperationException("Já existe um registro com esse código.");

            var e = new RhSensoWeb.Models.Taux1
            {
                Cdtptabela = dto.Cdtptabela.Trim(),
                Dctabela   = dto.Dctabela.Trim()
            };
            await _repo.AddAsync(e);
            await _repo.SaveChangesAsync();
        }

        public async Task UpdateAsync(string id, Taux1Dto dto)
        {
            var e = await _repo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Registro não encontrado.");

            // Se o usuário alterou a PK: remover antiga e inserir nova (ou decidir bloquear)
            if (!string.Equals(id, dto.Cdtptabela, StringComparison.OrdinalIgnoreCase))
            {
                // Estratégia simples: deletar antiga e inserir nova
                await _repo.DeleteAsync(id);
                await _repo.AddAsync(new RhSensoWeb.Models.Taux1
                {
                    Cdtptabela = dto.Cdtptabela.Trim(),
                    Dctabela   = dto.Dctabela.Trim()
                });
            }
            else
            {
                e.Dctabela = dto.Dctabela.Trim();
                await _repo.UpdateAsync(e);
            }

            await _repo.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            await _repo.DeleteAsync(id);
            await _repo.SaveChangesAsync();
        }

        public async Task DeleteBatchAsync(IEnumerable<string> ids)
        {
            await _repo.DeleteBatchAsync(ids);
            await _repo.SaveChangesAsync();
        }
    }
}
