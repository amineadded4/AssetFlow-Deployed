// ============================================================
// AssetFlow.Infrastructure / Data / AppDbContext.cs
// MISE À JOUR : Ajout de la table Incidents
// ============================================================

using AssetFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // === TABLES ===
        public DbSet<User> Users { get; set; }
        public DbSet<Materiel> Materiels { get; set; }
        public DbSet<Affectation> Affectations { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<Fournisseur> Fournisseurs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // === CONFIGURATION USER ===
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
                entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // === CONFIGURATION MATERIEL ===
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

            // === CONFIGURATION AFFECTATION ===
            modelBuilder.Entity<Affectation>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Statut).HasConversion<string>().HasMaxLength(50);

                // Relation Affectation -> Materiel (1-N)
                // un matériel a plusieurs affectations, mais une affectation concerne un seul matériel
                entity.HasOne(a => a.Materiel)
                      .WithMany(m => m.Affectations)
                      .HasForeignKey(a => a.MaterielId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relation Affectation -> User (1-N)
                entity.HasOne(a => a.Utilisateur)
                      .WithMany()
                      .HasForeignKey(a => a.UtilisateurId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // === CONFIGURATION INCIDENT ===
            modelBuilder.Entity<Incident>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.TypeIncident).IsRequired().HasMaxLength(50);
                entity.Property(i => i.Description).IsRequired().HasMaxLength(1000);
                entity.Property(i => i.Statut).HasConversion<string>().HasMaxLength(50);
                entity.Property(i => i.CommentairesResolution).HasMaxLength(1000);

                // Relation Incident -> Affectation (N-1)
                // une affectation a plusieurs incidents, mais un incident concerne une seule affectation
                entity.HasOne(i => i.Affectation)
                      .WithMany()
                      .HasForeignKey(i => i.AffectationId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            // === CONFIGURATION Fournisseur ===
modelBuilder.Entity<Fournisseur>(entity =>
{
    entity.ToTable("Fournisseur");

    entity.HasKey(f => f.IdFournisseur);
    entity.Property(f => f.IdFournisseur).ValueGeneratedOnAdd();

    entity.Property(f => f.Nom).IsRequired().HasColumnType("varchar(100)");
    entity.Property(f => f.Telephone).HasColumnType("varchar(20)");
    entity.Property(f => f.Adresse).HasColumnType("varchar(255)");
    entity.Property(f => f.Mail).HasColumnType("varchar(150)");

    // === NOUVEAUX CHAMPS ===
    entity.Property(f => f.CommandesTotales)
          .HasColumnType("int")
          .HasDefaultValue(0);

    entity.Property(f => f.TauxLivraisonATemps)
          .HasColumnType("decimal(5,2)")
          .HasDefaultValue(0);

    entity.Property(f => f.ScoreFiabilite)
          .HasColumnType("decimal(5,2)")
          .HasDefaultValue(0);

    entity.Property(f => f.DerniereCommande)
          .HasColumnType("datetime")
          .IsRequired(false);
});
        }
    }
}