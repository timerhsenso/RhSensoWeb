using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhSensoWeb.Models
{
    [Table("tsistema")]
    public class Tsistema
    {
        [Key]
        [Required(ErrorMessage = "O c�digo � obrigat�rio.")]
        [StringLength(10, ErrorMessage = "Use no m�ximo {1} caracteres.")]
        [Display(Name = "C�digo")]
        public string Cdsistema { get; set; } = "";


        [Required(ErrorMessage = "A descri��o � obrigat�ria.")]
        [StringLength(100, ErrorMessage = "Use no m�ximo {1} caracteres.")]
        [Display(Name = "Descri��o")]
        public string Dcsistema { get; set; } = "";


        [Column("ativo")]
        [Display(Name = "Ativo")]
        [Required]
        public bool Ativo { get; set; } = true;
    }
}


