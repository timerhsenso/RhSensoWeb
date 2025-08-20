using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhSensoWeb.Models
{
    [Table("tsistema")]
    public class Tsistema
    {
        [Key]
        [Required(ErrorMessage = "O código é obrigatório.")]
        [StringLength(10, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Código")]
        public string Cdsistema { get; set; } = "";


        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(100, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Descrição")]
        public string Dcsistema { get; set; } = "";


        [Column("ativo")]
        [Display(Name = "Ativo")]
        [Required]
        public bool Ativo { get; set; } = true;
    }
}


