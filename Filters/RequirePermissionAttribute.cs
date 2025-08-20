using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RhSensoWeb.Helpers;

namespace RhSensoWeb.Filters
{
    /// Ex.: [RequirePermission("SEG","SEG_USUARIO")]        -> precisa ter a função
    ///      [RequirePermission("SEG","SEG_USUARIO","C")]     -> precisa ter AÇÃO C (consultar)
    ///      [RequirePermission("SEG","SEG_USUARIO","AEI")]   -> precisa ter pelo menos 1 das ações
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _system;
        private readonly string _function;
        private readonly string _actions; // ex: "A", "E", "I", "C" (qualquer combinação)

        public RequirePermissionAttribute(string systemCode, string functionCode, string actions = "")
        {
            _system = systemCode?.Trim().ToUpperInvariant() ?? "";
            _function = functionCode?.Trim().ToUpperInvariant() ?? "";
            _actions = (actions ?? "").Trim().ToUpperInvariant();
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var http = context.HttpContext;

            if (!PermissionAccessHelper.HasAccess(http, _system, _function))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }

            if (!string.IsNullOrEmpty(_actions))
            {
                var allowed = PermissionAccessHelper.GetActions(http, _system, _function);
                if (allowed.Length == 0 || !allowed.Any(a => _actions.Contains(a)))
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                    return;
                }
            }
        }
    }
}
