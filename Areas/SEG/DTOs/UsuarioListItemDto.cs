// ==============================================
// File: Areas/SEG/DTOs/UsuarioListItemDto.cs
// ==============================================
namespace RhSensoWeb.Areas.SEG.DTOs
{
    public sealed record UsuarioListItemDto(
    string cdusuario,
    string dcusuario,
    string email_usuario,
    string tpusuario,
    string tipo_desc,
    bool ativo,
    string editToken,
    string deleteToken
    );
}