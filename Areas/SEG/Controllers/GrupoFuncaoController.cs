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

        #region TAB 1 - Grupos de Usuários

        /// <summary>
        /// Carrega todos os grupos de usuários para o DataTable do TAB 1
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

        #region TAB 2 - Gestão de Funções

        /// <summary>
        /// Carrega funções disponíveis (que o grupo ainda NÃO tem acesso)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFuncoesDisponiveis(string cdGrUser, string cdSistema)
        {
            try
            {
                if (string.IsNullOrEmpty(cdGrUser) || string.IsNullOrEmpty(cdSistema))
                {
                    return Json(new { success = false, message = "Grupo e Sistema são obrigatórios" });
                }

                // Busca funções que NÃO estão no grupo
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
        /// Carrega funções que o grupo JÁ tem acesso
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFuncoesGrupo(string cdGrUser, string cdSistema)
        {
            try
            {
                if (string.IsNullOrEmpty(cdGrUser) || string.IsNullOrEmpty(cdSistema))
                {
                    return Json(new { success = false, message = "Grupo e Sistema são obrigatórios" });
                }

                // Busca funções que JÁ estão no grupo com JOIN na tabela de funções
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
        /// Carrega botões disponíveis para uma função específica
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetBotoesFuncao(string cdFuncao, string cdSistema, string? cdGrUser = null)
        {
            try
            {
                if (string.IsNullOrEmpty(cdFuncao) || string.IsNullOrEmpty(cdSistema))
                {
                    return Json(new { success = false, message = "Função e Sistema são obrigatórios" });
                }

                // Busca todos os botões da função
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

                // Se fornecido o grupo, busca quais ações estão habilitadas
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

                // Monta resultado com status de habilitação
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
        /// Adiciona funções ao grupo (insere registros na hbrh1)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarFuncoes([FromBody] MovimentarFuncoesRequest request)
        {
            try
            {
                if (request?.Funcoes == null || !request.Funcoes.Any())
                {
                    return Json(new { success = false, message = "Nenhuma função selecionada" });
                }

                if (string.IsNullOrEmpty(request.CdGrUser) || string.IsNullOrEmpty(request.CdSistema))
                {
                    return Json(new { success = false, message = "Grupo e Sistema são obrigatórios" });
                }

                var novasHabilitacoes = new List<Hbrh1>();

                foreach (var cdFuncao in request.Funcoes)
                {
                    // Verifica se já existe
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
                            CdAcoes = "C", // Padrão: apenas Consultar
                            CdRestric = "L" // Padrão: Livre
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
                    message = $"{novasHabilitacoes.Count} função(ões) adicionada(s) com sucesso!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao adicionar funções: {ex.Message}" });
            }
        }

        /// <summary>
        /// Remove funções do grupo (remove registros da hbrh1)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverFuncoes([FromBody] MovimentarFuncoesRequest request)
        {
            try
            {
                if (request?.Funcoes == null || !request.Funcoes.Any())
                {
                    return Json(new { success = false, message = "Nenhuma função selecionada" });
                }

                if (string.IsNullOrEmpty(request.CdGrUser) || string.IsNullOrEmpty(request.CdSistema))
                {
                    return Json(new { success = false, message = "Grupo e Sistema são obrigatórios" });
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
                    message = $"{habilitacoes.Count} função(ões) removida(s) com sucesso!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao remover funções: {ex.Message}" });
            }
        }

        /// <summary>
        /// Atualiza as permissões (ações) de uma função específica para o grupo
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
                    return Json(new { success = false, message = "Grupo, Função e Sistema são obrigatórios" });
                }

                var habilitacao = await _context.Hbrh1
                    .FirstOrDefaultAsync(h => h.CdGrUser == request.CdGrUser &&
                                             h.CdFuncao == request.CdFuncao &&
                                             h.CdSistema == request.CdSistema);

                if (habilitacao == null)
                {
                    return Json(new { success = false, message = "Habilitação não encontrada" });
                }

                // Monta string de ações ordenada
                var acoesHabilitadas = string.Join("",
                    (request.BotoesHabilitados ?? new List<string>())
                    .Where(a => !string.IsNullOrEmpty(a))
                    .Distinct()
                    .OrderBy(a => a));

                habilitacao.CdAcoes = acoesHabilitadas;
                habilitacao.CdRestric = request.TipoAcesso ?? "L";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Permissões atualizadas com sucesso!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao atualizar permissões: {ex.Message}" });
            }
        }

        #endregion

        #region TAB 3 - Usuários do Grupo

        /// <summary>
        /// Carrega usuários vinculados ao grupo
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUsuariosGrupo(string cdGrUser, string? cdSistema = null)
        {
            try
            {
                if (string.IsNullOrEmpty(cdGrUser))
                {
                    return Json(new { success = false, message = "Grupo é obrigatório" });
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
                return Json(new { success = false, message = $"Erro ao carregar usuários: {ex.Message}" });
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