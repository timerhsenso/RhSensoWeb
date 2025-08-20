#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhSensoWeb.Models
{
    [Table("temp1")]
    public class Temp1
    {
        // ===== Chaves =====
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Display(Name = "Código da Empresa")]
        public int Cdempresa { get; set; }

        // UNIQUE, gerado por default (NEWSEQUENTIALID()) no banco
        [Required]
        [Display(Name = "Id (rowguid)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        // ===== Campos de Identificação =====
        /// <summary>
        /// Nome/Descrição da empresa
        /// </summary>
        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(100, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Descrição")]
        public string? Nmempresa { get; set; }

        [StringLength(30)]
        [Display(Name = "Nome Fantasia")]
        public string? Nmfantasia { get; set; }

        // ===== Códigos/Tipos (char) =====
        [StringLength(2)]
        [Display(Name = "Tipo C.CHE")]
        [Column(TypeName = "char(2)")]
        public string? Chtpcche { get; set; }

        [StringLength(2)]
        [Display(Name = "Tipo DARF")]
        [Column(TypeName = "char(2)")]
        public string? Chtpdarf { get; set; }

        [StringLength(2)]
        [Display(Name = "Tipo GRPS")]
        [Column(TypeName = "char(2)")]
        public string? Chtpgrps { get; set; }

        [StringLength(2)]
        [Display(Name = "Tipo TRES")]
        [Column(TypeName = "char(2)")]
        public string? Chtptres { get; set; }

        [StringLength(1)]
        [Display(Name = "Browser Funcionários")]
        [Column(TypeName = "char(1)")]
        public string? Chbrwfunc { get; set; }

        [StringLength(1)]
        [Display(Name = "Tolerância 1")]
        [Column(TypeName = "char(1)")]
        public string? Chtorc1 { get; set; }

        [StringLength(1)]
        [Display(Name = "Férias")]
        [Column(TypeName = "char(1)")]
        public string? Chferias { get; set; }

        [StringLength(80)]
        [Display(Name = "Arquivo do Logo (nome)")]
        [Column(TypeName = "char(80)")]
        public string? Nmarqlogo { get; set; }

        [StringLength(80)]
        [Display(Name = "Arquivo do Logo (cracha) - nome")]
        public string? Nmarqlogocra { get; set; }

        // ===== Indicadores / Flags numéricos =====
        [Display(Name = "FAP eSocial")]
        public int? Flfapesocial { get; set; }

        [StringLength(1)]
        [Display(Name = "Tp. Inscrição Empregador")]
        [Column(TypeName = "char(1)")]
        public string? Tpinscempregador { get; set; }

        [StringLength(15)]
        [Display(Name = "Nº Inscrição Empregador")]
        public string? Nrinscempregador { get; set; }

        [StringLength(1)]
        [Display(Name = "Ativo (S/N)")]
        [Column(TypeName = "char(1)")]
        [RegularExpression("(?i)^[SN]$", ErrorMessage = "Use 'S' para Sim ou 'N' para Não.")]
        public string? Flativo { get; set; }

        // ===== Arquivos / Binários =====
        [StringLength(42)]
        [Display(Name = "Arquivo do Logo")]
        public string? Arquivologo { get; set; }

        [Display(Name = "Logo (binário)")]
        [Column(TypeName = "image")]
        public byte[]? Logo { get; set; }

        [StringLength(42)]
        [Display(Name = "Arquivo do Logo do Crachá")]
        public string? Arquivologocracha { get; set; }

        [Display(Name = "Logo Crachá (binário)")]
        [Column(TypeName = "image")]
        public byte[]? Logocracha { get; set; }

        // ===== Classificação / Documentos =====
        [StringLength(2)]
        [Display(Name = "Classificação Tributária")]
        [Column(TypeName = "char(2)")]
        public string? Classtrib { get; set; }

        [StringLength(14)]
        [Display(Name = "CNPJ EFR")]
        [Column(TypeName = "char(14)")]
        public string? Cnpjefr { get; set; }

        // ===== Datas =====
        [DataType(DataType.Date)]
        [Display(Name = "Data DOU")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? Dtdou { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Emissão Certificado")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? Dtemissaocertificado { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Próx. Renovação")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? Dtprotrenovacao { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Vencimento Certificado")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? Dtvenctocertificado { get; set; }

        // ===== Outros Campos =====
        [StringLength(70)]
        [Display(Name = "ID Emissor na Lei")]
        public string? Ideminlei { get; set; }

        [Display(Name = "Acordo Isento Multa (0/1)")]
        public int? Indacordoisenmulta { get; set; }

        [Display(Name = "Construtora (0/1)")]
        public int? Indconstrutora { get; set; }

        [Display(Name = "Cooperativa (0/1)")]
        public int? Indcooperativa { get; set; }

        [Display(Name = "Desoneração Folha (0/1)")]
        public int? Inddesfolha { get; set; }

        [Display(Name = "OPC/CPP (0/1)")]
        public int? Indopccp { get; set; }

        [StringLength(1)]
        [Display(Name = "Porte")]
        [Column(TypeName = "char(1)")]
        public string? Indporte { get; set; }

        [Display(Name = "Optou Reg. Eletrônico (0/1)")]
        public int? Indoptregeletronico { get; set; }

        [StringLength(4)]
        [Display(Name = "Natureza Jurídica")]
        [Column(TypeName = "char(4)")]
        public string? Natjuridica { get; set; }

        [StringLength(40)]
        [Display(Name = "Nº Certificado")]
        public string? Nrcertificado { get; set; }

        [StringLength(40)]
        [Display(Name = "Nº Proc. Renovação")]
        public string? Nrprotrenovacao { get; set; }

        [StringLength(30)]
        [Display(Name = "Nº Registro TT")]
        public string? Nrregett { get; set; }

        [StringLength(5)]
        [Display(Name = "Página DOU")]
        public string? Paginadou { get; set; }
    }
}
