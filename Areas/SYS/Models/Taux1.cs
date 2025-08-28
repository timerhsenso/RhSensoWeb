using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhSensoWeb.Models
{
    [Table("taux1")]
    public class Taux1
    {
        [Key]
        [Required(ErrorMessage = "O código do tipo de tabela é obrigatório.")]
        [StringLength(2, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Código Tipo Tabela")]
        public string Cdtptabela { get; set; } = "";

        [Required(ErrorMessage = "A descrição da tabela é obrigatória.")]
        [StringLength(60, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Descrição")]
        public string Dctabela { get; set; } = "";
    }
}
