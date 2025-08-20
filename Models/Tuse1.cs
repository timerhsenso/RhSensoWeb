using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhSensoWeb.Models
{
    [Table("tuse1")]
    public class Tuse1
    {
        // PK
        [Key]
        [Column("cdusuario")]
        [Required(ErrorMessage = "O c�digo do usu�rio � obrigat�rio.")]
        [StringLength(30, ErrorMessage = "Use no m�ximo {1} caracteres.")]
        [Display(Name = "C�digo do Usu�rio")]
        public string Cdusuario { get; set; } = string.Empty;

        // Nome/Descri��o do usu�rio
        [Column("dcusuario")]
        [Required(ErrorMessage = "O nome do usu�rio � obrigat�rio.")]
        [StringLength(50, ErrorMessage = "Use no m�ximo {1} caracteres.")]
        [Display(Name = "Usu�rio / Nome")]
        public string Dcusuario { get; set; } = string.Empty;

        // Senha (se for manter na tabela)
        [Column("senhauser")]
        [StringLength(20, ErrorMessage = "Use no m�ximo {1} caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string? Senhauser { get; set; }

        // Nome para impress�o de cheque (campo legado)
        [Column("nmimpcche")]
        [StringLength(50, ErrorMessage = "Use no m�ximo {1} caracteres.")]
        [Display(Name = "Nome p/ Cheque")]
        public string? Nmimpcche { get; set; }

        // Tipo de usu�rio (1 caractere)
        [Column("tpusuario")]
        [Required(ErrorMessage = "O tipo de usu�rio � obrigat�rio.")]
        [StringLength(1, ErrorMessage = "Use 1 caractere.")]
        [Display(Name = "Tipo de Usu�rio")]
        public string Tpusuario { get; set; } = string.Empty;

        // Matr�cula (char(8))
        [Column("nomatric")]
        [StringLength(8, ErrorMessage = "Use no m�ximo {1} caracteres.")]
        [Display(Name = "Matr�cula")]
        public string? Nomatric { get; set; }

        // Empresa/Filial
        [Column("cdempresa")]
        [Display(Name = "Empresa")]
        public int? Cdempresa { get; set; }

        [Column("cdfilial")]
        [Display(Name = "Filial")]
        public int? Cdfilial { get; set; }

        // N�mero interno do usu�rio (n�o-nulo)
        [Column("nouser")]
        [Required(ErrorMessage = "O N� do usu�rio � obrigat�rio.")]
        [Display(Name = "N� Usu�rio")]
        public int Nouser { get; set; }

        // E-mail
        [Column("email_usuario")]
        [StringLength(100, ErrorMessage = "Use no m�ximo {1} caracteres.")]
        [EmailAddress(ErrorMessage = "E-mail inv�lido.")]
        [Display(Name = "E-mail")]
        public string? Email_usuario { get; set; }

        // Flag de ativo (S/N) � mant�m compatibilidade com a base
        [Column("flativo")]
        [Required]
        [StringLength(1)]
        [Display(Name = "Ativo (S/N)")]
        public string Flativo { get; set; } = "S";

        // Propriedade auxiliar para trabalhar com bool como no Tsistema
        [NotMapped]
        [Display(Name = "Ativo")]
        public bool Ativo
        {
            get => string.Equals(Flativo, "S", StringComparison.OrdinalIgnoreCase)
                || Flativo == "1" || string.Equals(Flativo, "T", StringComparison.OrdinalIgnoreCase);
            set => Flativo = value ? "S" : "N";
        }

        // Identificador (Guid)
        [Column("id")]
        [Display(Name = "Id (Guid)")]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Username normalizado (opcional)
        [Column("normalizedusername")]
        [StringLength(30, ErrorMessage = "Use no m�ximo {1} caracteres.")]
        [Display(Name = "Usu�rio Normalizado")]
        public string? NormalizedUsername { get; set; }

        // Relacionamento opcional com funcion�rio (Guid)
        [Column("idfuncionario")]
        [Display(Name = "Id do Funcion�rio")]
        public Guid? IdFuncionario { get; set; }

        // Flag de n�o receber e-mail (S/N)
        [Column("flnaorecebeemail")]
        [StringLength(1)]
        [Display(Name = "N�o recebe e-mail (S/N)")]
        public string? FlNaoRecebeEmail { get; set; }

        // Propriedade auxiliar em bool
        [NotMapped]
        [Display(Name = "N�o recebe e-mail")]
        public bool NaoRecebeEmail
        {
            get => string.Equals(FlNaoRecebeEmail ?? "N", "S", StringComparison.OrdinalIgnoreCase)
                || FlNaoRecebeEmail == "1" || string.Equals(FlNaoRecebeEmail ?? "N", "T", StringComparison.OrdinalIgnoreCase);
            set => FlNaoRecebeEmail = value ? "S" : "N";
        }
    }
}
