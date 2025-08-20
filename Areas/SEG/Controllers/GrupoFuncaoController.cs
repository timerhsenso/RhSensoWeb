using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RhSensoWeb.Data;
using RhSensoWeb.Models;

namespace RhSensoWeb.Areas.SEG.Controllers
{
    [Area("SEG")]
    public class GrupoFuncaoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GrupoFuncaoController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index() => View();

        #region TAB 1 - Grupos de Usu�rios

        /// <summary>
        /// Carrega todos os grupos de usu�rios para o DataTable do TAB 1
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetGrupos()
        {
            try
            {
                var grupos = await _context.Gurh1
                    .Select(g => new
                    {
                        cdgruser = g.CdGrUser,
                        dcgruser = g.DcGrUser ?? "",
                        cdsistema = g.CdSistema ?? ""
                    })
                    .OrderBy(g => g.dcgruser)
                    .ToListAsync();

                return Json(new { success = true, data = grupos });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region TAB 2 - Gest�o de Fun��es

        /// <summary>
        /// Carrega fun��es dispon�veis (que o grupo ainda N�O tem acesso)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFuncoesDisponiveis(string cdGrUser, string cdSistema)
        {
            try
            {
                if (string.IsNullOrEmpty(cdGrUser) || string.IsNullOrEmpty(cdSistema))
                {
                    return Json(new { success = false, message = "Grupo e Sistema s�o obrigat�rios" });
                }

                // Busca fun��es que N�O est�o no grupo
                var funcoesDisponiveis = await _context.Fucn1
                    .Where(f => f.CdSistema == cdSistema &&
                                !_context.Hbrh1.Any(h => h.CdGrUser == cdGrUser &&
                                                        h.CdFuncao == f.CdFuncao &&
                                                        h.CdSistema == cdSistema))
                    .Select(f => new
                    {
                        codigo = f.CdFuncao,
                        desc = f.DcFuncao ?? "",
                        modulo = f.DcModulo ?? ""
                    })
                    .OrderBy(f => f.desc)
                    .ToListAsync();

                return Json(new { success = true, data = funcoesDisponiveis });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Carrega fun��es que o grupo J� tem acesso
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFuncoesGrupo(string cdGrUser, string cdSistema)
        {
            try
            {
                if (string.IsNullOrEmpty(cdGrUser) || string.IsNullOrEmpty(cdSistema))
                {
                    return Json(new { success = false, message = "Grupo e Sistema s�o obrigat�rios" });
                }

                // Busca fun��es que J� est�o no grupo com JOIN na tabela de fun��es
                var funcoesGrupo = await _context.Hbrh1
                    .Where(h => h.CdGrUser == cdGrUser && h.CdSistema == cdSistema)
                    .Join(_context.Fucn1,
                        h => new { h.CdSistema, h.CdFuncao },
                        f => new { f.CdSistema, f.CdFuncao },
                        (h, f) => new
                        {
                            codigo = h.CdFuncao,
                            desc = f.DcFuncao ?? "",
                            modulo = f.DcModulo ?? "",
                            cdacoes = h.CdAcoes ?? "",
                            cdrestric = h.CdRestric ?? "L"
                        })
                    .OrderBy(h => h.desc)
                    .ToListAsync();

                return Json(new { success = true, data = funcoesGrupo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Carrega bot�es dispon�veis para uma fun��o espec�fica
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetBotoesFuncao(string cdFuncao, string cdSistema, string? cdGrUser = null)
        {
            try
            {
                if (string.IsNullOrEmpty(cdFuncao) || string.IsNullOrEmpty(cdSistema))
                {
                    return Json(new { success = false, message = "Fun��o e Sistema s�o obrigat�rios" });
                }

                // Busca todos os bot�es da fun��o
                var botoes = await _context.Btfuncao
                    .Where(b => b.Cdfuncao == cdFuncao && b.Cdsistema == cdSistema)
                    .Select(b => new
                    {
                        codigo = b.Nmbotao,
                        desc = b.Dcbotao ?? "",
                        acao = b.Cdacao ?? ""
                    })
                    .OrderBy(b => b.desc)
                    .ToListAsync();

                // Se fornecido o grupo, busca quais a��es est�o habilitadas
                string acoesHabilitadas = "";
                string tipoRestricao = "L";

                if (!string.IsNullOrEmpty(cdGrUser))
                {
                    var habilitacao = await _context.Hbrh1
                        .Where(h => h.CdGrUser == cdGrUser &&
                                   h.CdFuncao == cdFuncao &&
                                   h.CdSistema == cdSistema)
                        .Select(h => new { h.CdAcoes, h.CdRestric })
                        .FirstOrDefaultAsync();

                    if (habilitacao != null)
                    {
                        acoesHabilitadas = habilitacao.CdAcoes ?? "";
                        tipoRestricao = habilitacao.CdRestric ?? "L";
                    }
                }

                // Monta resultado com status de habilita��o
                var resultado = botoes.Select(b => new
                {
                    b.codigo,
                    b.desc,
                    b.acao,
                    habilitado = !string.IsNullOrEmpty(b.acao) && acoesHabilitadas.Contains(b.acao)
                }).ToList();

                return Json(new
                {
                    success = true,
                    data = resultado,
                    tipoRestricao = tipoRestricao
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Adiciona fun��es ao grupo (insere registros na hbrh1)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarFuncoes([FromBody] MovimentarFuncoesRequest request)
        {
            try
            {
                if (request?.Funcoes == null || !request.Funcoes.Any())
                {
                    return Json(new { success = false, message = "Nenhuma fun��o selecionada" });
                }

                if (string.IsNullOrEmpty(request.CdGrUser) || string.IsNullOrEmpty(request.CdSistema))
                {
                    return Json(new { success = false, message = "Grupo e Sistema s�o obrigat�rios" });
                }

                var novasHabilitacoes = new List<Hbrh1>();

                foreach (var cdFuncao in request.Funcoes)
                {
                    // Verifica se j� existe
                    var existeHabilitacao = await _context.Hbrh1
                        .AnyAsync(h => h.CdGrUser == request.CdGrUser &&
                                      h.CdFuncao == cdFuncao &&
                                      h.CdSistema == request.CdSistema);

                    if (!existeHabilitacao)
                    {
                        novasHabilitacoes.Add(new Hbrh1
                        {
                            CdGrUser = request.CdGrUser,
                            CdFuncao = cdFuncao,
                            CdSistema = request.CdSistema,
                            CdAcoes = "C", // Padr�o: apenas Consultar
                            CdRestric = "L" // Padr�o: Livre
                        });
                    }
                }

                if (novasHabilitacoes.Any())
                {
                    _context.Hbrh1.AddRange(novasHabilitacoes);
                    await _context.SaveChangesAsync();
                }

                return Json(new
                {
                    success = true,
                    message = $"{novasHabilitacoes.Count} fun��o(�es) adicionada(s) com sucesso!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao adicionar fun��es: {ex.Message}" });
            }
        }

        /// <summary>
        /// Remove fun��es do grupo (remove registros da hbrh1)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverFuncoes([FromBody] MovimentarFuncoesRequest request)
        {
            try
            {
                if (request?.Funcoes == null || !request.Funcoes.Any())
                {
                    return Json(new { success = false, message = "Nenhuma fun��o selecionada" });
                }

                if (string.IsNullOrEmpty(request.CdGrUser) || string.IsNullOrEmpty(request.CdSistema))
                {
                    return Json(new { success = false, message = "Grupo e Sistema s�o obrigat�rios" });
                }

                var habilitacoes = await _context.Hbrh1
                    .Where(h => h.CdGrUser == request.CdGrUser &&
                               h.CdSistema == request.CdSistema &&
                               request.Funcoes.Contains(h.CdFuncao))
                    .ToListAsync();

                if (habilitacoes.Any())
                {
                    _context.Hbrh1.RemoveRange(habilitacoes);
                    await _context.SaveChangesAsync();
                }

                return Json(new
                {
                    success = true,
                    message = $"{habilitacoes.Count} fun��o(�es) removida(s) com sucesso!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao remover fun��es: {ex.Message}" });
            }
        }

        /// <summary>
        /// Atualiza as permiss�es (a��es) de uma fun��o espec�fica para o grupo
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtualizarPermissoesFuncao([FromBody] AtualizarPermissoesRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.CdGrUser) ||
                    string.IsNullOrEmpty(request.CdFuncao) ||
                    string.IsNullOrEmpty(request.CdSistema))
                {
                    return Json(new { success = false, message = "Grupo, Fun��o e Sistema s�o obrigat�rios" });
                }

                var habilitacao = await _context.Hbrh1
                    .FirstOrDefaultAsync(h => h.CdGrUser == request.CdGrUser &&
                                             h.CdFuncao == request.CdFuncao &&
                                             h.CdSistema == request.CdSistema);

                if (habilitacao == null)
                {
                    return Json(new { success = false, message = "Habilita��o n�o encontrada" });
                }

                // Monta string de a��es ordenada
                var acoesHabilitadas = string.Join("",
                    (request.BotoesHabilitados ?? new List<string>())
                    .Where(a => !string.IsNullOrEmpty(a))
                    .Distinct()
                    .OrderBy(a => a));

                habilitacao.CdAcoes = acoesHabilitadas;
                habilitacao.CdRestric = request.TipoAcesso ?? "L";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Permiss�es atualizadas com sucesso!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao atualizar permiss�es: {ex.Message}" });
            }
        }

        #endregion

        #region TAB 3 - Usu�rios do Grupo

        /// <summary>
        /// Carrega usu�rios vinculados ao grupo
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUsuariosGrupo(string cdGrUser, string? cdSistema = null)
        {
            try
            {
                if (string.IsNullOrEmpty(cdGrUser))
                {
                    return Json(new { success = false, message = "Grupo � obrigat�rio" });
                }

                var query = _context.Usrh1.Where(u => u.CdGrUser == cdGrUser);

                // Filtro opcional por sistema
                if (!string.IsNullOrEmpty(cdSistema))
                {
                    query = query.Where(u => u.CdSistema == cdSistema);
                }

                var usuarios = await query
                    .Select(u => new
                    {
                        cdusuario = u.CdUsuario,
                        cdgruser = u.CdGrUser,
                        cdsistema = u.CdSistema
                    })
                    .OrderBy(u => u.cdusuario)
                    .ToListAsync();

                return Json(new { success = true, data = usuarios });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao carregar usu�rios: {ex.Message}" });
            }
        }

        #endregion
    }

    #region DTOs

    public class MovimentarFuncoesRequest
    {
        public string CdGrUser { get; set; } = string.Empty;
        public string CdSistema { get; set; } = string.Empty;
        public List<string> Funcoes { get; set; } = new List<string>();
    }

    public class AtualizarPermissoesRequest
    {
        public string CdGrUser { get; set; } = string.Empty;
        public string CdFuncao { get; set; } = string.Empty;
        public string CdSistema { get; set; } = string.Empty;
        public List<string>? BotoesHabilitados { get; set; }
        public string TipoAcesso { get; set; } = "L"; // L=Livre, P=Pessoal, C=Coordenador
    }

    #endregion
}