using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore; // <- para [PrimaryKey]

namespace RhSensoWeb.Models
{
    [Table("fucn1")]
    [PrimaryKey(nameof(CdSistema), nameof(CdFuncao))] // PK composta (cdsistema, cdfuncao)
    public class Fucn1
    {
        [Required(ErrorMessage = "O c�digo da fun��o � obrigat�rio.")]
        [Column("cdfuncao")]
        [StringLength(30, ErrorMessage = "Use no m�ximo {1} caracteres.")]
        [Display(Name = "C�digo da Fun��o")]
        public string CdFuncao { get; set; } = string.Empty;

        [Column("dcfuncao")]
        [StringLength(80, ErrorMessage = "Use no m�ximo {1} caracteres.")]
        [Display(Name = "Descri��o da Fun��o")]
        public string? DcFuncao { get; set; }

        [Required(ErrorMessage = "O c�digo do sistema � obrigat�rio.")]
        [Column("cdsistema", TypeName = "char(10)")]
        [StringLength(10, ErrorMessage = "Use no m�ximo {1} caracteres.")]
        [Display(Name = "Sistema")]
        public string CdSistema { get; set; } = string.Empty;

        [Column("dcmodulo")]
        [StringLength(100, ErrorMessage = "Use no m�ximo {1} caracteres.")]
        [Display(Name = "M�dulo")]
        public string? DcModulo { get; set; }

        [Column("descricaomodulo")]
        [StringLength(100, ErrorMessage = "Use no m�ximo {1} caracteres.")]
        [Display(Name = "Descri��o do M�dulo")]
        public string? DescricaoModulo { get; set; }

        // Navega��o
        [ForeignKey(nameof(CdSistema))]
        [Display(Name = "Sistema")]
        public virtual Tsistema? Tsistema { get; set; }
    }
}
