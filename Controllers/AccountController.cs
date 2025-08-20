using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RhSensoWeb.Data;
using RhSensoWeb.DTOs;
using RhSensoWeb.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http; // Session extensions (GetObject/SetObject)
using System.Linq;

namespace RhSensoWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        // evita estouro do cookie de autenticação
        private const int MAX_PERMSAGG_LEN = 3000;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Tentativa de login falha do IP {IP}. Motivo: Modelo inválido.", HttpContext.Connection.RemoteIpAddress);
                return View(model);
            }

            try
            {
                // 1) Busca usuário
                var user = await _context.Tuse1
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Cdusuario == model.Username);

                if (user == null || user.Flativo != "S")
                {
                    _logger.LogWarning("Tentativa de login falha para {User} do IP {IP}. Usuário inexistente ou inativo.",
                        model.Username, HttpContext.Connection.RemoteIpAddress);
                    ModelState.AddModelError(string.Empty, "Usuário ou senha inválidos ou usuário inativo.");
                    return View(model);
                }

                // 2) Validação de senha — compatível com SQL (CI) + fallback no banco
                var inputPwd = (model.Password ?? string.Empty).Trim();
                var dbPwd = (user.Senhauser ?? string.Empty).Trim();

                var passwordOk =
                    string.Equals(dbPwd, inputPwd, StringComparison.Ordinal) ||
                    string.Equals(dbPwd, inputPwd, StringComparison.OrdinalIgnoreCase);

                if (!passwordOk)
                {
                    // respeita collation/semântica do banco
                    passwordOk =
                        await _context.Tuse1.AsNoTracking()
                            .AnyAsync(u => u.Cdusuario == user.Cdusuario && u.Senhauser == model.Password)
                        || await _context.Tuse1.AsNoTracking()
                            .AnyAsync(u => u.Cdusuario == user.Cdusuario && u.Senhauser == inputPwd);
                }

                if (!passwordOk)
                {
                    _logger.LogWarning("Tentativa de login falha para {User} do IP {IP}. Senha inválida.",
                        model.Username, HttpContext.Connection.RemoteIpAddress);
                    ModelState.AddModelError(string.Empty, "Usuário ou senha inválidos ou usuário inativo.");
                    return View(model);
                }

                // 3) Constantes na sessão
                var constantes = await _context.Const1.AsNoTracking().ToListAsync();
                HttpContext.Session.SetObject("Constantes", constantes);

                // 4) Grupos ativos do usuário (roles)
                var userGroups = await _context.Usrh1
                    .AsNoTracking()
                    .Where(u => u.CdUsuario == user.Cdusuario && (u.DtFimVal == null || u.DtFimVal > DateTime.Now))
                    .Select(u => u.CdGrUser)
                    .ToListAsync();

                // 5) Permissões do usuário — JOIN com Usrh1 para evitar OPENJSON/IN local
                var userPermissions = await (
                    from h in _context.Hbrh1.AsNoTracking()
                    join u in _context.Usrh1.AsNoTracking()
                        on h.CdGrUser equals u.CdGrUser
                    join f in _context.Fucn1.AsNoTracking()
                        on new { h.CdFuncao, h.CdSistema } equals new { f.CdFuncao, f.CdSistema }
                    join s in _context.Tsistema.AsNoTracking()
                        on h.CdSistema equals s.Cdsistema
                    where u.CdUsuario == user.Cdusuario
                          && (u.DtFimVal == null || u.DtFimVal > DateTime.Now)
                          //&& f.DcModulo != null
                          && s.Ativo
                    select new UserPermissionDto
                    {
                        FunctionCode = (h.CdFuncao ?? "").Trim(),
                        ActionCode = (h.CdAcoes ?? "").Trim().ToUpper(),
                        SystemCode = (h.CdSistema ?? "").Trim(),
                        ModuleName = (f.DcModulo ?? "").Trim(),
                        ModuleDescription = (f.DescricaoModulo ?? "").Trim(),
                        ActionRestric = (h.CdRestric ?? "").Trim()                        
                    }
                ).ToListAsync();

                // 6) Grava na sessão (usado pelos helpers/filters)
                HttpContext.Session.SetObject("UserPermissions", userPermissions);

                // 7) Claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Cdusuario ?? ""),
                    new Claim(ClaimTypes.GivenName, user.Dcusuario ?? ""),
                    new Claim("TpUsuario", user.Tpusuario ?? ""),
                    new Claim("NoMatric", user.Nomatric ?? ""),
                    new Claim("CdEmpresa", user.Cdempresa?.ToString() ?? ""),
                    new Claim("CdFilial",  user.Cdfilial?.ToString()  ?? ""),
                    new Claim(ClaimTypes.Email, user.Email_usuario ?? "")
                };

                foreach (var g in userGroups.Where(g => !string.IsNullOrWhiteSpace(g)))
                    claims.Add(new Claim(ClaimTypes.Role, g.Trim()));

                // 8) Claim compacto "SIST|FUNCAO=ACEI;..."
                var permsAggSeq = userPermissions
                    .GroupBy(p => new { p.SystemCode, p.FunctionCode })
                    .Select(g =>
                    {
                        var actions = string.Concat(
                            g.Select(x => (x.ActionCode ?? "").Trim().ToUpperInvariant())
                             .SelectMany(s => s)
                             .Distinct()
                             .OrderBy(c => c)
                        );
                        return $"{g.Key.SystemCode}|{g.Key.FunctionCode}={actions}";
                    });

                var permsAgg = string.Join(';', permsAggSeq);
                var len = permsAgg.Length;

                _logger.LogInformation("Login {User}: perms={PermsCount}, pairs={Pairs}, permsAggLen={Len}",
                    user.Cdusuario, userPermissions.Count,
                    string.IsNullOrEmpty(permsAgg) ? 0 : permsAgg.Count(c => c == ';') + 1, len);

                if (len <= MAX_PERMSAGG_LEN && !string.IsNullOrEmpty(permsAgg))
                    claims.Add(new Claim("PermsAgg", permsAgg));
                else if (len > MAX_PERMSAGG_LEN)
                    _logger.LogWarning("PermsAgg omitido (len={Len} > {Max}). Session será usada como fallback.", len, MAX_PERMSAGG_LEN);

                // 9) Sign-in
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProps = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(model.RememberMe ? 480 : 30),
                    AllowRefresh = true
                };

                _logger.LogDebug("Pronto para SignIn. claims={Claims}, roles={Roles}, hasPermsAgg={HasAgg}",
                    claims.Count, userGroups.Count, claims.Any(c => c.Type == "PermsAgg"));

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    authProps);

                _logger.LogInformation("Login bem-sucedido para {User} do IP {IP}", model.Username, HttpContext.Connection.RemoteIpAddress);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no login de {User} do IP {IP}", model.Username, HttpContext.Connection.RemoteIpAddress);
                ModelState.AddModelError(string.Empty, "001 - Erro interno do sistema.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        public IActionResult AccessDenied() => View();
    }
}
