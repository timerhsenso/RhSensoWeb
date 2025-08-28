using Microsoft.EntityFrameworkCore;
using RhSensoWeb.Models;

namespace RhSensoWeb.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Gurh1> Gurh1 { get; set; }
        public DbSet<Tuse1> Tuse1 { get; set; }
        public DbSet<Usrh1> Usrh1 { get; set; }
        public DbSet<Fucn1> Fucn1 { get; set; }
        public DbSet<Hbrh1> Hbrh1 { get; set; }
        public DbSet<Tsistema> Tsistema { get; set; }
        public DbSet<Const1> Const1 { get; set; }

        /* SEG */
        public DbSet<Btfuncao> Btfuncao { get; set; }

        /**/

        public DbSet<RhSensoWeb.Models.Taux1> Taux1 { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Usrh1
            modelBuilder.Entity<Usrh1>(entity =>
            {
                entity.HasKey(u => new { u.CdUsuario, u.CdGrUser, u.CdSistema });

                entity.Property(u => u.CdUsuario).IsUnicode(false);
                entity.Property(u => u.CdGrUser).IsUnicode(false);
                entity.Property(u => u.CdSistema).HasMaxLength(10).IsFixedLength().IsUnicode(false);

                entity.HasOne(u => u.Gurh1)
                      .WithMany(g => g.UsuariosGrupo)
                      .HasForeignKey(u => u.CdGrUser)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(u => u.Tsistema)
                      .WithMany()
                      .HasForeignKey(u => u.CdSistema)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Fucn1 (PK na ordem correta: CdSistema, CdFuncao)
            modelBuilder.Entity<Fucn1>(entity =>
            {
                entity.ToTable("fucn1");

                entity.HasKey(f => new { f.CdSistema, f.CdFuncao });

                entity.Property(f => f.CdSistema)
                      .HasColumnName("cdsistema")
                      .HasMaxLength(10).IsFixedLength().IsUnicode(false);

                entity.Property(f => f.CdFuncao)
                      .HasColumnName("cdfuncao")
                      .HasMaxLength(30).IsUnicode(false);

                // Se seu model tem DcFuncao (varchar(80)):
                entity.Property(f => f.DcFuncao)
                      .HasColumnName("dcfuncao")
                      .HasMaxLength(80).IsUnicode(false);

                entity.Property(f => f.DcModulo)
                      .HasColumnName("dcmodulo")
                      .HasMaxLength(100).IsUnicode(false);

                entity.Property(f => f.DescricaoModulo)
                      .HasColumnName("descricaomodulo")
                      .HasMaxLength(100).IsUnicode(false);

                entity.HasOne(f => f.Tsistema)
                      .WithMany()
                      .HasForeignKey(f => f.CdSistema)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Hbrh1
            modelBuilder.Entity<Hbrh1>(entity =>
            {
                entity.HasKey(h => new { h.CdGrUser, h.CdFuncao, h.CdAcoes, h.CdRestric, h.CdSistema });

                entity.Property(h => h.CdGrUser).IsUnicode(false);
                entity.Property(h => h.CdFuncao).HasMaxLength(30).IsUnicode(false);
                entity.Property(h => h.CdAcoes).IsUnicode(false);
                entity.Property(h => h.CdRestric).IsUnicode(false);
                entity.Property(h => h.CdSistema).HasMaxLength(10).IsFixedLength().IsUnicode(false);

                entity.HasOne(h => h.Gurh1)
                      .WithMany(g => g.HabilitacoesGrupo)
                      .HasForeignKey(h => h.CdGrUser)
                      .OnDelete(DeleteBehavior.Restrict);

                // FK para Fucn1 deve seguir a MESMA ORDEM da PK de Fucn1 (CdSistema, CdFuncao)
                entity.HasOne(h => h.Fucn1)
                      .WithMany()
                      .HasForeignKey(h => new { h.CdSistema, h.CdFuncao })
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(h => h.Tsistema)
                      .WithMany()
                      .HasForeignKey(h => h.CdSistema)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Tsistema
            modelBuilder.Entity<Tsistema>(entity =>
            {
                entity.ToTable("tsistema");

                entity.HasKey(e => e.Cdsistema);

                entity.Property(e => e.Cdsistema)
                      .HasColumnName("cdsistema")
                      .HasMaxLength(10).IsFixedLength().IsUnicode(false);

                entity.Property(e => e.Dcsistema)
                      .HasColumnName("dcsistema")
                      .HasMaxLength(100).IsUnicode(false);

                // bool mapeado para bit no SQL (coluna "ativo")
                entity.Property(e => e.Ativo)
                      .HasColumnName("ativo");
            });

            // Btfuncao
            modelBuilder.Entity<Btfuncao>(entity =>
            {
                entity.ToTable("btfuncao");

                // PK composta: (cdsistema, cdfuncao, nmbotao)
                entity.HasKey(e => new { e.Cdsistema, e.Cdfuncao, e.Nmbotao })
                      .HasName("PK_btfuncao");

                // Colunas (varchar/char => IsUnicode(false); char(10) => IsFixedLength)
                entity.Property(e => e.Cdsistema)
                      .HasColumnName("cdsistema")
                      .HasMaxLength(10)
                      .IsFixedLength()
                      .IsUnicode(false);

                entity.Property(e => e.Cdfuncao)
                      .HasColumnName("cdfuncao")
                      .HasMaxLength(30)
                      .IsUnicode(false);

                entity.Property(e => e.Nmbotao)
                      .HasColumnName("nmbotao")
                      .HasMaxLength(30)
                      .IsUnicode(false);

                entity.Property(e => e.Dcbotao)
                      .HasColumnName("dcbotao")
                      .HasMaxLength(60)
                      .IsUnicode(false);

                entity.Property(e => e.Cdacao)
                      .HasColumnName("cdacao")
                      .HasMaxLength(1)
                      .IsUnicode(false);

                // FK para Tsistema (sem cascade)
                entity.HasOne(e => e.Tsistema)
                      .WithMany()
                      .HasForeignKey(e => e.Cdsistema)
                      .OnDelete(DeleteBehavior.Restrict);

                // FK composto para Fucn1 (mesma ordem da PK de Fucn1: CdSistema, CdFuncao) e sem cascade
                entity.HasOne(e => e.Fucn1)
                      .WithMany()
                      .HasForeignKey(e => new { e.Cdsistema, e.Cdfuncao })
                      .HasConstraintName("FK_btfuncao_fucn1_cdsistema_cdfuncao")
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
