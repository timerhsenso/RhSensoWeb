using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RhSensoWeb.Areas.SEG.Models
{
    /// <summary>
    /// Estabelecimento / Filial (tabela test1)
    /// </summary>
    [Table("test1")]
    [PrimaryKey(nameof(Cdempresa), nameof(Cdfilial))]
    public class Test1
    {
        // ===== PK composta =====
        [Column("cdempresa")]
        [Display(Name = "Empresa (código)")]
        [Required(ErrorMessage = "O campo Empresa é obrigatório.")]
        public int Cdempresa { get; set; }

        [Column("cdfilial")]
        [Display(Name = "Filial (código)")]
        [Required(ErrorMessage = "O campo Filial é obrigatório.")]
        public int Cdfilial { get; set; }

        // ===== Identificação / Endereço =====
        [Column("nmfantasia")]
        [StringLength(30, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Nome fantasia")]
        public string? Nmfantasia { get; set; }

        [Column("dcestab")]
        [StringLength(60, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Descrição do estabelecimento")]
        public string? Dcestab { get; set; }

        [Column("dcendereco")]
        [StringLength(60, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Endereço")]
        public string? Dcendereco { get; set; }

        [Column("dcbairro")]
        [StringLength(60, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Bairro")]
        public string? Dcbairro { get; set; }

        [Column("sgestado")]
        [StringLength(2, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "UF")]
        public string? Sgestado { get; set; }

        [Column("nocep")]
        [StringLength(9, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "CEP")]
        public string? Nocep { get; set; }

        [Column("notelefone")]
        [StringLength(15, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Telefone")]
        public string? Notelefone { get; set; }

        [Column("nofax")]
        [StringLength(10, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Fax")]
        public string? Nofax { get; set; }

        [Column("nomatinps")]
        [StringLength(15, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Matrícula INPS")]
        public string? Nomatinps { get; set; }

        [Column("cdcgc")]
        [StringLength(15, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "CNPJ/CGC")]
        public string? Cdcgc { get; set; }

        [Column("cdmunicip")]
        [StringLength(5, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Município (cód.)")]
        public string? Cdmunicip { get; set; }

        [Column("cdatvinps")]
        [StringLength(7, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Atividade INPS (cód.)")]
        public string? Cdatvinps { get; set; }

        [Column("cdativibge")]
        [StringLength(5, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Atividade IBGE (cód.)")]
        public string? Cdativibge { get; set; }

        [Column("cdnatjus")]
        [StringLength(4, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Natureza Jurídica (cód.)")]
        public string? Cdnatjus { get; set; }

        [Column("noinscriest")]
        [StringLength(15, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Inscrição Estadual")]
        public string? Noinscriest { get; set; }

        [Column("cdmunirais")]
        [StringLength(7, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Município RAIS (cód.)")]
        public string? Cdmunirais { get; set; }

        [Column("noproprie")]
        [StringLength(2, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Propriedade (cód.)")]
        public string? Noproprie { get; set; }

        [Column("noinscricei")]
        [StringLength(15, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Inscrição CEI")]
        public string? Noinscricei { get; set; }

        [Column("cdativir")]
        [StringLength(4, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Atividade RFB (cód.)")]
        public string? Cdativir { get; set; }

        [Column("noinscrimun")]
        [StringLength(15, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Inscrição Municipal")]
        public string? Noinscrimun { get; set; }

        [Column("cdtbsal")]
        [StringLength(2, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Tabela Salarial (cód.)")]
        public string? Cdtbsal { get; set; }

        [Column("cdcalcdig")]
        [StringLength(2, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Cálculo Dígito (cód.)")]
        public string? Cdcalcdig { get; set; }

        [Column("numero")]
        [StringLength(6, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Número")]
        public string? Numero { get; set; }

        [Column("cdbcofgts")]
        [StringLength(3, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Banco FGTS (cód.)")]
        public string? Cdbcofgts { get; set; }

        [Column("cdagefgts")]
        [StringLength(4, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Agência FGTS (cód.)")]
        public string? Cdagefgts { get; set; }

        [Column("cdidempcef")]
        [StringLength(13, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "ID Empregador (CEF)")]
        public string? Cdidempcef { get; set; }

        [Column("flrecfgts")]
        [StringLength(2, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Recebe FGTS (flag)")]
        public string? Flrecfgts { get; set; }

        [Column("cdsimples")]
        [StringLength(2, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Simples (cód.)")]
        public string? Cdsimples { get; set; }

        [Column("flcnae")]
        [StringLength(1, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "CNAE (flag)")]
        public string? Flcnae { get; set; }

        [Column("cdfpas")]
        [StringLength(3, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "FPAS (cód.)")]
        public string? Cdfpas { get; set; }

        [Column("cdactrab")]
        [StringLength(7, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Atividade Trabalho (cód.)")]
        public string? Cdactrab { get; set; }

        [Column("cdterc")]
        [StringLength(4, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Terceiros (cód.)")]
        public string? Cdterc { get; set; }

        [Column("cdgps")]
        [StringLength(4, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "GPS (cód.)")]
        public string? Cdgps { get; set; }

        // ===== Percentuais (numeric 12,2) =====
        [Column("pcconv")]
        [Precision(12, 2)]
        [Display(Name = "Percentual Conv.")]
        [Required(ErrorMessage = "O Percentual Conv. é obrigatório.")]
        public decimal Pcconv { get; set; }

        [Column("pcsat")]
        [Precision(12, 2)]
        [Display(Name = "Percentual SAT")]
        [Required(ErrorMessage = "O Percentual SAT é obrigatório.")]
        public decimal Pcsat { get; set; }

        [Column("pcterc")]
        [Precision(12, 2)]
        [Display(Name = "Percentual Terceiros")]
        [Required(ErrorMessage = "O Percentual Terceiros é obrigatório.")]
        public decimal Pcterc { get; set; }

        [Column("pcemp")]
        [Precision(12, 2)]
        [Display(Name = "Percentual Empresa")]
        [Required(ErrorMessage = "O Percentual Empresa é obrigatório.")]
        public decimal Pcemp { get; set; }

        [Column("nocaged")]
        [StringLength(7, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "CAGED (nº)")]
        public string? Nocaged { get; set; }

        [Column("declara")]
        [StringLength(2, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Declara (flag)")]
        public string? Declara { get; set; }

        [Column("alteracao")]
        [StringLength(2, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Alteração (flag)")]
        public string? Alteracao { get; set; }

        [Column("noinscter")]
        [StringLength(14, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Inscrição Terceiros")]
        public string? Noinscter { get; set; }

        [Column("nmtservico")]
        [StringLength(40, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Nome do Tomador de Serviço")]
        public string? Nmtservico { get; set; }

        [Column("dcendetser")]
        [StringLength(60, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Endereço do Tomador")]
        public string? Dcendetser { get; set; }

        [Column("dcbairtser")]
        [StringLength(60, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Bairro do Tomador")]
        public string? Dcbairtser { get; set; }

        [Column("noceptser")]
        [StringLength(8, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "CEP do Tomador")]
        public string? Noceptser { get; set; }

        [Column("dccidtser")]
        [StringLength(20, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Cidade do Tomador")]
        public string? Dccidtser { get; set; }

        [Column("sgesttser")]
        [StringLength(2, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "UF do Tomador")]
        public string? Sgesttser { get; set; }

        [Column("tpinscter")]
        [StringLength(1, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Tipo Inscrição Terceiros")]
        public string? Tpinscter { get; set; }

        [Column("noultmatric")]
        [StringLength(8, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Última Matrícula")]
        public string? Noultmatric { get; set; }

        [Column("noultrficha")]
        [StringLength(6, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Última Ficha")]
        public string? Noultrficha { get; set; }

        [Column("chmatric")]
        [StringLength(1, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Controle Matrícula (flag)")]
        public string? Chmatric { get; set; }

        [Column("chnoregist")]
        [StringLength(1, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Controle Nº Registro (flag)")]
        public string? Chnoregist { get; set; }

        [Column("cdcontaadn")]
        [StringLength(4, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Conta AD Noturno (cód.)")]
        public string? Cdcontaadn { get; set; }

        // ===== Percentuais (float) =====
        [Column("pc_conv")]
        [Display(Name = "Percentual Conv. (float)")]
        public double? PcConv { get; set; }

        [Column("pc_sat")]
        [Display(Name = "Percentual SAT (float)")]
        public double? PcSat { get; set; }

        [Column("pc_terc")]
        [Display(Name = "Percentual Terceiros (float)")]
        public double? PcTerc { get; set; }

        [Column("pc_emp")]
        [Display(Name = "Percentual Empresa (float)")]
        public double? PcEmp { get; set; }

        // ===== Chaves/Referências =====
        [Column("cdfornec")]
        [Display(Name = "Fornecedor (cód.)")]
        public int? Cdfornec { get; set; }

        [Column("cdtpinscri")]
        [Display(Name = "Tipo Inscrição (cód.)")]
        public int? Cdtpinscri { get; set; }

        [Column("cdmotocHE")]
        [StringLength(4, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Motivo HE (cód.)")]
        public string? CdmotocHe { get; set; }

        [Column("cdmotocFALTA")]
        [StringLength(4, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Motivo Falta (cód.)")]
        public string? CdmotocFalta { get; set; }

        [Column("tpocorrHE")]
        [Display(Name = "Tipo Ocorrência HE")]
        public int? TpocorrHe { get; set; }

        [Column("tpocorrFALTA")]
        [Display(Name = "Tipo Ocorrência Falta")]
        public int? TpocorrFalta { get; set; }

        // ===== Flags e Regras =====
        [Column("FLADTNOT")]
        [Display(Name = "Adic. Noturno (flag)")]
        [Required(ErrorMessage = "O campo Adic. Noturno (flag) é obrigatório.")]
        public int Fladtnot { get; set; }

        [Column("INIADTNOT")]
        [StringLength(5, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Início Adic. Noturno (HH:mm)")]
        public string? IniAdtnot { get; set; }

        [Column("FIMADTNOT")]
        [StringLength(5, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Fim Adic. Noturno (HH:mm)")]
        public string? FimAdtnot { get; set; }

        [Column("FLLIMTROCA")]
        [Display(Name = "Limitar Troca (flag)")]
        [Required(ErrorMessage = "O campo Limitar Troca (flag) é obrigatório.")]
        public int Fllimtroca { get; set; }

        [Column("LIMTROCA")]
        [Display(Name = "Limite de Troca")]
        public int? Limtroca { get; set; }

        [Column("cdempctb")]
        [Display(Name = "Empresa Contábil (cód.)")]
        public short? Cdempctb { get; set; }

        [Column("cdfilctb")]
        [Display(Name = "Filial Contábil (cód.)")]
        public short? Cdfilctb { get; set; }

        [Column("VALORHORAADN")]
        [Precision(12, 2)]
        [Display(Name = "Valor Hora AD Noturno")]
        public decimal? ValorHoraAdn { get; set; }

        [Column("FLDESCONTAALMOCO")]
        [Display(Name = "Desconta Almoço (flag)")]
        public int? FldescontaAlmoco { get; set; }

        [Column("FLMINHE")]
        [Display(Name = "Mínimo HE (flag)")]
        [Required(ErrorMessage = "O campo Mínimo HE (flag) é obrigatório.")]
        public int Flminhe { get; set; }

        [Column("VLMINHE")]
        [Display(Name = "Valor Mínimo HE (min.)")]
        public int? Vlminhe { get; set; }

        [Column("TPOCORRATRAZO")]
        [Display(Name = "Tipo Ocorrência Atraso")]
        public int? TpocorrAtrazo { get; set; }

        [Column("CDMOTOCATRAZO")]
        [StringLength(4, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Motivo Atraso (cód.)")]
        public string? CdmotocAtrazo { get; set; }

        [Column("EMAIL")]
        [StringLength(30, ErrorMessage = "Use no máximo {1} caracteres.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [Display(Name = "E-mail")]
        public string? Email { get; set; }

        [Column("CQTHORAMAX")]
        [StringLength(30, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Carga Horária Máxima")]
        public string? CqtHoraMax { get; set; }

        [Column("CLGN")]
        [StringLength(30, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Login Correio")]
        public string? Clgn { get; set; }

        [Column("USESENH")]
        [StringLength(30, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Senha Correio")]
        public string? UseSenh { get; set; }

        [Column("CHOST")]
        [StringLength(30, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Host Correio")]
        public string? Chost { get; set; }

        [Column("pc_fap")]
        [Precision(12, 6)]
        [Display(Name = "FAP")]
        public decimal? PcFap { get; set; }

        [Column("cdsindicatres")]
        [StringLength(2, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Sindicato Res. (cód.)")]
        public string? Cdsindicatres { get; set; }

        [Column("flativofilial")]
        [Display(Name = "Filial Ativa (flag)")]
        public int? FlativoFilial { get; set; }

        [Column("noddd")]
        [StringLength(3, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "DDD")]
        public string? Noddd { get; set; }

        [Column("cod_tipo_sisponto_rais")]
        [StringLength(2, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Tipo SisPonto RAIS (cód.)")]
        public string? CodTipoSispontoRais { get; set; }

        [Column("dtinivalidade")]
        [Display(Name = "Início de Validade")]
        public DateTime? Dtinivalidade { get; set; }

        [Column("pcfilantropia")]
        [Precision(5, 2)]
        [Display(Name = "Filantropia (%)")]
        public decimal? Pcfilantropia { get; set; }

        [Column("DCEND_COMP")]
        [StringLength(60, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Endereço (compl.)")]
        public string? DcendComp { get; set; }

        [Column("idempresa")]
        [Display(Name = "Empresa (ID)")]
        public Guid? Idempresa { get; set; }

        // UK com default newsequentialid()
        [Column("id")]
        [Display(Name = "Identificador Único")]
        [Required(ErrorMessage = "O Identificador Único é obrigatório.")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Column("idmunicipioendereco")]
        [Display(Name = "Município Endereço (ID)")]
        public Guid? IdMunicipioEndereco { get; set; }

        [Column("idsindicato")]
        [Display(Name = "Sindicato (ID)")]
        public Guid? IdSindicato { get; set; }

        [Column("idlotacaotributaria")]
        [Display(Name = "Lotação Tributária (ID)")]
        public Guid? IdLotacaoTributaria { get; set; }

        [Column("indsubstpatrobra")]
        [Display(Name = "Ind. Substituição Patrobral")]
        public short? IndSubstPatrobra { get; set; }

        [Column("numeroprocessoapcd")]
        [StringLength(20, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Nº Processo APCD")]
        public string? NumeroProcessoApcd { get; set; }

        [Column("numeroprocessoaprendiz")]
        [StringLength(20, ErrorMessage = "Use no máximo {1} caracteres.")]
        [Display(Name = "Nº Processo Aprendiz")]
        public string? NumeroProcessoAprendiz { get; set; }

        [Column("tpcaepf")]
        [Display(Name = "Tipo CAEPF")]
        public short? Tpcaepf { get; set; }

        // ===== Navegações (adicione classes se existirem) =====
        // public virtual Temp1? Temp1ByEmpresa { get; set; }
        // public virtual Temp1? Temp1ByEmpresaId { get; set; }
        // public virtual LotacoesTributarias? LotacaoTributaria { get; set; }
        // public virtual Muni1? MunicipioEndereco { get; set; }
        // public virtual Sind1? Sindicato { get; set; }
        // public virtual Mfre1? MotivoHe { get; set; }
        // public virtual Mfre1? MotivoFalta { get; set; }
    }
}
