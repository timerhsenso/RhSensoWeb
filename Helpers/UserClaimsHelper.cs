using System.Security.Claims;

namespace RhSensoWeb.Helpers
{
    public static class UserClaimsHelper
    {
        public static string? GetCdUsuario(this ClaimsPrincipal user) =>
            user.FindFirst(ClaimTypes.Name)?.Value;

        public static string? GetDcUsuario(this ClaimsPrincipal user) =>
            user.FindFirst(ClaimTypes.GivenName)?.Value;

        public static string? GetTpUsuario(this ClaimsPrincipal user) =>
            user.FindFirst("TpUsuario")?.Value;

        public static string? GetNoMatric(this ClaimsPrincipal user) =>
            user.FindFirst("NoMatric")?.Value;

        public static string? GetCdEmpresa(this ClaimsPrincipal user) =>
            user.FindFirst("CdEmpresa")?.Value;

        public static string? GetCdFilial(this ClaimsPrincipal user) =>
            user.FindFirst("CdFilial")?.Value;

        public static string? GetEmail(this ClaimsPrincipal user) =>
            user.FindFirst(ClaimTypes.Email)?.Value;
    }
}
