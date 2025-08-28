using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhSensoWeb.Models
{
    [Table("taux2")]
    public class Taux2
    {
        [Required(ErrorMessage = "O código do tipo de tabela é obrigatório.")]
        [StringLength(2, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Código Tipo Tabela")]
        public string Cdtptabela { get; set; } = "";

        [Required(ErrorMessage = "O código da situação é obrigatório.")]
        [StringLength(2, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Código Situação")]
        public string Cdsituacao { get; set; } = "";

        [Required(ErrorMessage = "A descrição da situação é obrigatória.")]
        [StringLength(60, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Descrição Situação")]
        public string Dcsituacao { get; set; } = "";

        [Display(Name = "Ordem")]
        public int? Noordem { get; set; }

        [Column("flativoaux")]
        [StringLength(1)]
        [Display(Name = "Ativo (S/N)")]
        public string? Flativoaux { get; set; } = "S";

        [NotMapped]
        [Display(Name = "Ativo")]
        public bool Ativo
        {
            get => string.Equals(Flativoaux, "S", StringComparison.OrdinalIgnoreCase)
                || Flativoaux == "1"
                || string.Equals(Flativoaux, "T", StringComparison.OrdinalIgnoreCase);
            set => Flativoaux = value ? "S" : "N";
        }

        [ForeignKey(nameof(Cdtptabela))]
        public Taux1? Taux1 { get; set; }
    }
}
