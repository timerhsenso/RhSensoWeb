using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhSensoWeb.Models
{
    [Table("usrh1")]
    public class Usrh1
    {
        [Key]
        [Column("cdusuario")]
        [StringLength(20)]
        public string CdUsuario { get; set; } = string.Empty;

        [Key]
        [Column("cdgruser")]
        [StringLength(20)]
        public string CdGrUser { get; set; } = string.Empty;

        [Key]
        [Column("cdsistema")]
        [StringLength(20)]
        public string CdSistema { get; set; } = string.Empty;

        [Column("dtinival")]
        public DateTime? DtIniVal { get; set; }

        [Column("dtfimval")]
        public DateTime? DtFimVal { get; set; }

        [ForeignKey("CdGrUser")]
        public virtual Gurh1? Gurh1 { get; set; }

        [ForeignKey("CdSistema")]
        public virtual Tsistema? Tsistema { get; set; }
    }
}


