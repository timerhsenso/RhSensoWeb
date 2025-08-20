using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhSensoWeb.Models
{
    [Table("hbrh1")]
    public class Hbrh1
    {
        [Key]
        [Column("cdgruser")]
        [StringLength(20)]
        public string CdGrUser { get; set; } = string.Empty;

        [Key]
        [Column("cdfuncao")]
        [StringLength(20)]
        public string CdFuncao { get; set; } = string.Empty;

        [Key]
        [Column("cdacoes")]
        [StringLength(20)]
        public string CdAcoes { get; set; } = string.Empty;

        [Key]
        [Column("cdrestric")]
        [StringLength(20)]
        public string CdRestric { get; set; } = string.Empty;

        [Key]
        [Column("cdsistema")]
        [StringLength(20)]
        public string CdSistema { get; set; } = string.Empty;

        [ForeignKey("CdGrUser")]
        public virtual Gurh1? Gurh1 { get; set; }

        [ForeignKey("CdFuncao")]
        public virtual Fucn1? Fucn1 { get; set; }

        [ForeignKey("CdSistema")]
        public virtual Tsistema? Tsistema { get; set; }
    }
}


