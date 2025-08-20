using RhSensoWeb.DTOs;

namespace RhSensoWeb.Models
{
    public class UserPermissions
    {
        public List<UserPermissionDto> Permissions { get; set; } = new List<UserPermissionDto>();

        public bool HasPermission(string functionCode, string actionCode = "", string systemCode = "")
        {
            if (string.IsNullOrEmpty(actionCode) && string.IsNullOrEmpty(systemCode))
            {
                return Permissions.Any(p => p.FunctionCode == functionCode);
            }
            else if (string.IsNullOrEmpty(systemCode))
            {
                return Permissions.Any(p => p.FunctionCode == functionCode && p.ActionCode == actionCode);
            }
            else
            {
                return Permissions.Any(p => p.FunctionCode == functionCode && p.ActionCode == actionCode && p.SystemCode == systemCode);
            }
        }
    }
}


