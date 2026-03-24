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
            public DbSet<User> Users { get; set; }
            public DbSet<Materiel> Materiels { get; set; }
            public DbSet<Affectation> Affectations { get; set; }
            public DbSet<Incident> Incidents { get; set; }
            public DbSet<Fournisseur> Fournisseurs { get; set; }
            public DbSet<Commande> Commandes { get; set; }
            public DbSet<ArticleIndividuel> ArticlesIndividuels { get; set; }
            public DbSet<DemandeAchat> DemandeAchat { get; set; }
            public DbSet<OffreAchat> OffreAchat { get; set; }
            public DbSet<ChatMessage> ChatMessages { get; set; }
            public DbSet<Project> Projects { get; set; }
            public DbSet<LigneDemande>  LigneDemande  { get; set; } 
            public DbSet<CommentaireMateriel> CommentairesMateriel => Set<CommentaireMateriel>();

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
                        entity.HasIndex(m => m.Reference).IsUnique();
                  });

                  // === AFFECTATION ===
                  modelBuilder.Entity<Affectation>(entity =>
                  {
                        entity.HasKey(a => a.Id);
                        entity.HasOne(a => a.Materiel)
                        .WithMany(m => m.Affectations)
                        .HasForeignKey(a => a.MaterielId)
                        .OnDelete(DeleteBehavior.Restrict);
                        entity.HasOne(a => a.Utilisateur)
                        .WithMany()
                        .HasForeignKey(a => a.UtilisateurId)
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired(false);
                        entity.Property(a => a.Etat)
                          .HasConversion<string>()
                          .HasMaxLength(20)
                          .HasDefaultValue(EtatAffectation.Courante);
                        entity.HasOne(a => a.Projet)
                          .WithMany()
                          .HasForeignKey(a => a.ProjetId)
                          .OnDelete(DeleteBehavior.SetNull)
                          .IsRequired(false);
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
                        entity.HasOne(i => i.Article)
                        .WithMany()
                        .HasForeignKey(i => i.ArticleId)
                        .OnDelete(DeleteBehavior.SetNull)
                        .IsRequired(false);
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
                        .OnDelete(DeleteBehavior.SetNull);
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
                        // Relation optionnelle vers Affectation
                        entity.HasOne(a => a.Affectation)
                      .WithMany(af => af.Articles)
                      .HasForeignKey(a => a.AffectationId)
                      .OnDelete(DeleteBehavior.SetNull)  // si affectation supprimée → null
                      .IsRequired(false);
                  });
                  modelBuilder.Entity<DemandeAchat>(entity =>
                  {
                        entity.ToTable("DemandeAchat");
                        entity.HasKey(d => d.IdDemande);
                        entity.Property(d => d.IdDemande).ValueGeneratedOnAdd();

                        entity.Property(d => d.Reference)
                              .IsRequired()
                              .HasColumnType("varchar(30)");

                        entity.Property(d => d.NomProduit)
                              .IsRequired()
                              .HasColumnType("varchar(200)");

                        entity.Property(d => d.Quantite)
                              .HasDefaultValue(1);

                        entity.Property(d => d.Description)
                              .HasColumnType("nvarchar(max)");

                        entity.Property(d => d.Statut)
                              .IsRequired()
                              .HasColumnType("varchar(20)")
                              .HasDefaultValue("en_attente");

                        entity.Property(d => d.DateCreation)
                              .HasColumnType("datetime2")
                              .HasDefaultValueSql("GETDATE()");

                        entity.Property(d => d.DemandeurNom)
                              .IsRequired()
                              .HasColumnType("varchar(150)");

                        entity.Property(d => d.MotifRefus)
                              .HasColumnType("nvarchar(500)");

                  // Relation 1→N vers OffreAchat (CASCADE DELETE)
                  entity.HasMany(d => d.Offres)
                        .WithOne(o => o.Demande)
                        .HasForeignKey(o => o.IdDemande)
                        .OnDelete(DeleteBehavior.Cascade);
                        entity.HasMany(d => d.Lignes)
                        .WithOne(l => l.Demande)
                        .HasForeignKey(l => l.IdDemande)
                        .OnDelete(DeleteBehavior.Cascade);
                  });

                  modelBuilder.Entity<OffreAchat>(entity =>
{
      entity.ToTable("OffreAchat");
      entity.HasKey(o => o.IdOffre);

      entity.Property(o => o.IdOffre)
            .HasColumnType("uniqueidentifier")
            .HasDefaultValueSql("NEWID()");

      entity.Property(o => o.NomFichier)
            .IsRequired()
            .HasColumnType("varchar(300)");

      entity.Property(o => o.Taille)
            .HasColumnType("bigint");

      entity.Property(o => o.ContenuPdf)
            .HasColumnType("varbinary(max)");

      entity.Property(o => o.EstChoisie)
            .HasDefaultValue(false);


});
                  modelBuilder.Entity<ChatMessage>(entity =>
                      {
                            entity.HasKey(m => m.Id);
                            entity.Property(m => m.Content).IsRequired().HasMaxLength(4000);
                            entity.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
                            entity.HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
                            entity.HasIndex(m => new { m.SenderId, m.ReceiverId, m.SentAt });
                      });

                      modelBuilder.Entity<Project>(entity =>
                        {
                        entity.HasKey(p => p.Id);
                        entity.Property(p => p.Nom).IsRequired().HasMaxLength(200);
                        entity.Property(p => p.Description).HasMaxLength(2000);
                        entity.Property(p => p.Statut)
                              .HasConversion<string>()
                              .HasMaxLength(30)
                              .HasDefaultValue(StatutProjet.Planifie);
                        entity.Property(p => p.Priorite)
                              .HasConversion<string>()
                              .HasMaxLength(20)
                              .HasDefaultValue(PrioriteProjet.Moyenne);
                        entity.Property(p => p.Responsable).HasMaxLength(150);
                        entity.Property(p => p.Budget).HasColumnType("decimal(18,2)");
                        entity.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                        entity.Property(p => p.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                        });
 modelBuilder.Entity<LigneDemande>(entity =>
    {
        entity.HasKey(l => l.IdLigne);
        entity.Property(l => l.NomProduit).IsRequired().HasMaxLength(200);
        entity.Property(l => l.Quantite).HasDefaultValue(1);

        entity.HasOne(l => l.Demande)
              .WithMany(d => d.Lignes)
              .HasForeignKey(l => l.IdDemande)
              .OnDelete(DeleteBehavior.Cascade);
    });
// ── BLOC 2 : Ajouter dans OnModelCreating ───────────────────
modelBuilder.Entity<CommentaireMateriel>(e =>
{
    e.ToTable("CommentairesMateriel");
    e.HasKey(c => c.Id);
 
    e.Property(c => c.Contenu)
     .HasMaxLength(1000)
     .IsRequired();
 
    e.HasOne(c => c.Materiel)
     .WithMany()
     .HasForeignKey(c => c.MaterielId)
     .OnDelete(DeleteBehavior.Cascade);
 
    e.HasOne(c => c.Utilisateur)
     .WithMany()
     .HasForeignKey(c => c.UtilisateurId)
     .OnDelete(DeleteBehavior.Restrict);
});

            }
      }
}