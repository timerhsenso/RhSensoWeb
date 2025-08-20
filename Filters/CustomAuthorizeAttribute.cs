using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RhSensoWeb.Filters
{
    public class CustomAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _functionCode;
        private readonly string _actionCode;

        public CustomAuthorizeAttribute(string functionCode, string actionCode = "")
        {
            _functionCode = functionCode;
            _actionCode = actionCode;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity.IsAuthenticated)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var userPermissions = context.HttpContext.Session.GetObject<List<RhSensoWeb.DTOs.UserPermissionDto>>("UserPermissions");

            if (userPermissions == null || !userPermissions.Any(p => p.FunctionCode == _functionCode && (string.IsNullOrEmpty(_actionCode) || p.ActionCode == _actionCode)))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }
        }
    }
}


