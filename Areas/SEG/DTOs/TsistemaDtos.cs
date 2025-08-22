// Areas/SEG/DTOs/TsistemaDtos.cs
namespace RhSensoWeb.Areas.SEG.DTOs
{
    public sealed record TsistemaListItemDto(
        string cdsistema,
        string dcsistema,
        bool ativo,
        string editToken,
        string deleteToken
    );
}
