using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhSensoWeb.Models
{
    /// <summary>
    /// Entidade que representa a tabela de grupos de usuários (gurh1)
    /// Define os grupos de permissões disponíveis no sistema
    /// </summary>
    [Table("gurh1")]
    public class Gurh1
    {
        /// <summary>
        /// Código único do grupo - Chave primária
        /// </summary>
        [Key]
        [Column("cdgruser")]
        [StringLength(20)]
        public string CdGrUser { get; set; } = string.Empty;

        /// <summary>
        /// Descrição do grupo de usuários
        /// </summary>
        [Column("dcgruser")]
        [StringLength(100)]
        public string? DcGrUser { get; set; }

        /// <summary>
        /// Descrição do grupo de usuários
        /// </summary>
        [Column("cdsistema")]
        [StringLength(10)]
        public string? CdSistema { get; set; }

        // Navegação para usuários do grupo
        public virtual ICollection<Usrh1> UsuariosGrupo { get; set; } = new List<Usrh1>();

        // Navegação para habilitações do grupo
        public virtual ICollection<Hbrh1> HabilitacoesGrupo { get; set; } = new List<Hbrh1>();
    }
}

