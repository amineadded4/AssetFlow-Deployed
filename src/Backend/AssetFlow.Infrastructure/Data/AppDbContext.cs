// ============================================================
// AssetFlow.Infrastructure / Data / AppDbContext.cs
// MISE À JOUR : Ajout Commande + ArticleIndividuel
// ============================================================

using AssetFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // === TABLES ===
        public DbSet<User>              Users               { get; set; }
        public DbSet<Materiel>          Materiels           { get; set; }
        public DbSet<Affectation>       Affectations        { get; set; }
        public DbSet<Incident>          Incidents           { get; set; }
        public DbSet<Fournisseur>       Fournisseurs        { get; set; }
        public DbSet<Commande>          Commandes           { get; set; }
        public DbSet<ArticleIndividuel> ArticlesIndividuels { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // === USER ===
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
                entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // === MATERIEL ===
            modelBuilder.Entity<Materiel>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Reference).IsRequired().HasMaxLength(100);
                entity.Property(m => m.Designation).IsRequired().HasMaxLength(200);
                entity.Property(m => m.Categorie).IsRequired().HasMaxLength(100);
                entity.Property(m => m.Unite).HasMaxLength(50);
                entity.Property(m => m.Etat).HasConversion<string>().HasMaxLength(50);
                entity.HasIndex(m => m.Reference).IsUnique();
            });

            // === AFFECTATION ===
            modelBuilder.Entity<Affectation>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Statut).HasConversion<string>().HasMaxLength(50);
                entity.HasOne(a => a.Materiel)
                      .WithMany(m => m.Affectations)
                      .HasForeignKey(a => a.MaterielId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.Utilisateur)
                      .WithMany()
                      .HasForeignKey(a => a.UtilisateurId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // === INCIDENT ===
            modelBuilder.Entity<Incident>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.TypeIncident).IsRequired().HasMaxLength(50);
                entity.Property(i => i.Description).IsRequired().HasMaxLength(1000);
                entity.Property(i => i.Statut).HasConversion<string>().HasMaxLength(50);
                entity.Property(i => i.CommentairesResolution).HasMaxLength(1000);
                entity.HasOne(i => i.Affectation)
                      .WithMany()
                      .HasForeignKey(i => i.AffectationId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // === FOURNISSEUR ===
            modelBuilder.Entity<Fournisseur>(entity =>
            {
                entity.ToTable("Fournisseur");
                entity.HasKey(f => f.IdFournisseur);
                entity.Property(f => f.IdFournisseur).ValueGeneratedOnAdd();
                entity.Property(f => f.Nom).IsRequired().HasColumnType("varchar(100)");
                entity.Property(f => f.Telephone).HasColumnType("varchar(20)");
                entity.Property(f => f.Adresse).HasColumnType("varchar(255)");
                entity.Property(f => f.Mail).HasColumnType("varchar(150)");
                entity.Property(f => f.CommandesTotales).HasColumnType("int").HasDefaultValue(0);
                entity.Property(f => f.TauxLivraisonATemps).HasColumnType("decimal(5,2)").HasDefaultValue(0);
                entity.Property(f => f.ScoreFiabilite).HasColumnType("decimal(5,2)").HasDefaultValue(0);
                entity.Property(f => f.DerniereCommande).HasColumnType("datetime").IsRequired(false);
            });

            // === COMMANDE ===
            modelBuilder.Entity<Commande>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.NumeroCommande).IsRequired().HasMaxLength(100);
                entity.HasIndex(c => c.NumeroCommande).IsUnique();

                entity.HasOne(c => c.Materiel)
                      .WithMany()
                      .HasForeignKey(c => c.MaterielId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Fournisseur)
                      .WithMany()
                      .HasForeignKey(c => c.FournisseurId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // === ARTICLE INDIVIDUEL ===
            modelBuilder.Entity<ArticleIndividuel>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.NumeroSerie).HasMaxLength(200);
                entity.Property(a => a.Statut).HasConversion<string>().HasMaxLength(50);

                // Index unique sur NumeroSerie (uniquement si non null)
                entity.HasIndex(a => a.NumeroSerie)
                      .IsUnique()
                      .HasFilter("[NumeroSerie] IS NOT NULL");

                entity.HasOne(a => a.Materiel)
                      .WithMany()
                      .HasForeignKey(a => a.MaterielId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Commande)
                      .WithMany(c => c.Articles)
                      .HasForeignKey(a => a.CommandeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}