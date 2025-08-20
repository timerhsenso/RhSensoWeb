using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhSensoWeb.Models
{
    [Table("const1")]
    public class Const1
    {
        // OBS: no seu banco não há PK explícita; mantive Cdconstante como [Key]
        // (igual estava no seu código). O campo "id" é UNIQUE no DB.

        [Key]
        [Column("cdconstante")]
        [Required(ErrorMessage = "O código é obrigatório.")]
        [StringLength(60, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Código")]
        public string Cdconstante { get; set; } = "";

        [Column("dcconstante")]
        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(255, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Descrição")]
        public string Dcconstante { get; set; } = "";

        [Column("dcconteudo")]
        [StringLength(200, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Conteúdo")]
        public string? Dcconteudo { get; set; }

        [Column("tpcampo")]
        [Required(ErrorMessage = "O tipo de campo é obrigatório.")]
        [StringLength(1, ErrorMessage = "Use 1 caractere.")]
        [Display(Name = "Tipo de Campo")]
        public string Tpcampo { get; set; } = "";

        [Column("flalterar")]
        [StringLength(1, ErrorMessage = "Use 1 caractere.")]
        [Display(Name = "Pode Alterar")]
        public string? Flalterar { get; set; }

        [Column("cdfuncao")]
        [StringLength(30, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Função")]
        public string? Cdfuncao { get; set; }

        [Column("cdsistema")]
        [StringLength(10, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Sistema")]
        public string? Cdsistema { get; set; }

        [Column("txdescricao")]
        [StringLength(4000, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Descrição longa")]
        public string? Txdescricao { get; set; }

        [Column("id")]
        [Required]
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [Column("config")]
        [Required]
        [Display(Name = "Configuração")]
        public bool Config { get; set; } = false;

        [Column("tipo")]
        [StringLength(1, ErrorMessage = "Use 1 caractere.")]
        [Display(Name = "Tipo do Valor")]
        [RegularExpression("^[TNDH]$", ErrorMessage = "Valores permitidos: T, N, D ou H.")]
        public string? Tipo { get; set; }
    }
}
