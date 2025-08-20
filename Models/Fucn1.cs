using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore; // <- para [PrimaryKey]

namespace RhSensoWeb.Models
{
    [Table("fucn1")]
    [PrimaryKey(nameof(CdSistema), nameof(CdFuncao))] // PK composta (cdsistema, cdfuncao)
    public class Fucn1
    {
        [Required(ErrorMessage = "O código da função é obrigatório.")]
        [Column("cdfuncao")]
        [StringLength(30, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Código da Função")]
        public string CdFuncao { get; set; } = string.Empty;

        [Column("dcfuncao")]
        [StringLength(80, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Descrição da Função")]
        public string? DcFuncao { get; set; }

        [Required(ErrorMessage = "O código do sistema é obrigatório.")]
        [Column("cdsistema", TypeName = "char(10)")]
        [StringLength(10, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Sistema")]
        public string CdSistema { get; set; } = string.Empty;

        [Column("dcmodulo")]
        [StringLength(100, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Módulo")]
        public string? DcModulo { get; set; }

        [Column("descricaomodulo")]
        [StringLength(100, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Descrição do Módulo")]
        public string? DescricaoModulo { get; set; }

        // Navegação
        [ForeignKey(nameof(CdSistema))]
        [Display(Name = "Sistema")]
        public virtual Tsistema? Tsistema { get; set; }
    }
}
