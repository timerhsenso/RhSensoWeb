using Microsoft.AspNetCore.Http;
using RhSensoWeb.DTOs;

namespace RhSensoWeb.Helpers
{
    public static class PermissionAccessHelper
    {
        private const string ClaimKey = "PermsAgg";
        private const string SessionKey = "UserPermissions";

        public static bool HasAccess(HttpContext ctx, string systemCode, string functionCode)
        {
            var agg = ctx.User.FindFirst(ClaimKey)?.Value;
            if (!string.IsNullOrWhiteSpace(agg))
            {
                var key = $"{systemCode}|{functionCode}=";
                return agg.Split(';', StringSplitOptions.RemoveEmptyEntries)
                          .Any(x => x.StartsWith(key, StringComparison.OrdinalIgnoreCase));
            }

            var list = ctx.Session.GetObject<List<UserPermissionDto>>(SessionKey) ?? new();
            return list.Any(p =>
                string.Equals(p.SystemCode, systemCode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.FunctionCode, functionCode, StringComparison.OrdinalIgnoreCase));
        }

        public static string[] GetActions(HttpContext ctx, string systemCode, string functionCode)
        {
            var agg = ctx.User.FindFirst(ClaimKey)?.Value;
            if (!string.IsNullOrWhiteSpace(agg))
            {
                var key = $"{systemCode}|{functionCode}=";
                var hit = agg.Split(';', StringSplitOptions.RemoveEmptyEntries)
                             .FirstOrDefault(x => x.StartsWith(key, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(hit))
                {
                    var i = hit.IndexOf('=');
                    if (i > 0 && i + 1 < hit.Length)
                        return hit[(i + 1)..].Distinct().Select(c => c.ToString()).ToArray();
                }
                return Array.Empty<string>();
            }

            var list = ctx.Session.GetObject<List<UserPermissionDto>>(SessionKey) ?? new();
            return list.Where(p =>
                        string.Equals(p.SystemCode, systemCode, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(p.FunctionCode, functionCode, StringComparison.OrdinalIgnoreCase))
                       .Select(p => (p.ActionCode ?? "").Trim().ToUpperInvariant())
                       .SelectMany(s => s)
                       .Distinct()
                       .Select(c => c.ToString())
                       .ToArray();
        }
    }
}
