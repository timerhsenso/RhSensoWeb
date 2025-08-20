using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhSensoWeb.Models
{
    [Table("btfuncao")]
    public class Btfuncao
    {
        [Required(ErrorMessage = "O sistema é obrigatório.")]
        [StringLength(10, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Sistema")]
        [Column("cdsistema")]
        public string Cdsistema { get; set; } = "";

        [Required(ErrorMessage = "A função é obrigatória.")]
        [StringLength(30, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Função")]
        [Column("cdfuncao")]
        public string Cdfuncao { get; set; } = "";

        [Required(ErrorMessage = "O nome do botão é obrigatório.")]
        [StringLength(30, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Nome do botão")]
        [Column("nmbotao")]
        public string Nmbotao { get; set; } = "";

        [Required(ErrorMessage = "A descrição do botão é obrigatória.")]
        [StringLength(60, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Descrição do botão")]
        [Column("dcbotao")]
        public string Dcbotao { get; set; } = "";

        [Required(ErrorMessage = "A ação é obrigatória.")]
        [StringLength(1, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Ação")]
        [Column("cdacao")]
        public string Cdacao { get; set; } = "";

        // Navegações (opcionais; configuradas no OnModelCreating)
        [Display(Name = "Sistema")]
        public Tsistema? Tsistema { get; set; }

        [Display(Name = "Função do sistema")]
        public Fucn1? Fucn1 { get; set; }
    }
}
