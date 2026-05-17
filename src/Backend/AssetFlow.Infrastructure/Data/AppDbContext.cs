using AssetFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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
        public DbSet<LigneDemande> LigneDemande { get; set; }
        public DbSet<CommentaireMateriel> CommentairesMateriel => Set<CommentaireMateriel>();
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ArticleHistorique> ArticleHistoriques { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // === USER ===
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Utilisateurs");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
                entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.PasswordResetToken).HasMaxLength(6).IsRequired(false);
                entity.Property(u => u.PasswordResetTokenExpiry).IsRequired(false);
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
                    .WithMany(p => p.Affectations)
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
                entity.Property(f => f.Nom).IsRequired().HasMaxLength(100);
                entity.Property(f => f.Telephone).HasMaxLength(20);
                entity.Property(f => f.Adresse).HasMaxLength(255);
                entity.Property(f => f.Mail).HasMaxLength(150);
                entity.Property(f => f.CommandesTotales).HasDefaultValue(0);
                entity.Property(f => f.TauxLivraisonATemps).HasColumnType("numeric(5,2)").HasDefaultValue(0);
                entity.Property(f => f.ScoreFiabilite).HasColumnType("numeric(5,2)").HasDefaultValue(0);
                // DerniereCommande : PostgreSQL gère DateTime? nativement
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

                // ✅ PostgreSQL : index unique sans filtre SQL Server
                entity.HasIndex(a => a.NumeroSerie)
                    .IsUnique()
                    .HasFilter(null); // supprime le filtre SQL Server

                entity.HasOne(a => a.Materiel)
                    .WithMany()
                    .HasForeignKey(a => a.MaterielId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.Commande)
                    .WithMany(c => c.Articles)
                    .HasForeignKey(a => a.CommandeId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.Affectation)
                    .WithMany(af => af.Articles)
                    .HasForeignKey(a => a.AffectationId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
            });

            // === DEMANDE ACHAT ===
            modelBuilder.Entity<DemandeAchat>(entity =>
            {
                entity.ToTable("DemandeAchat");
                entity.HasKey(d => d.IdDemande);

                // ✅ PostgreSQL : NOW() au lieu de GETDATE()
                entity.Property(d => d.DateCreation)
                    .HasDefaultValueSql("NOW()");

                entity.Property(d => d.DemandeurNom)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(d => d.MotifRefus)
                    .HasMaxLength(500);

                entity.Property(d => d.UserId)
                    .IsRequired(false)
                    .HasDefaultValue(0);

                entity.HasMany(d => d.Offres)
                    .WithOne(o => o.Demande)
                    .HasForeignKey(o => o.IdDemande)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(d => d.Lignes)
                    .WithOne(l => l.Demande)
                    .HasForeignKey(l => l.IdDemande)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
            });

            // === OFFRE ACHAT ===
            modelBuilder.Entity<OffreAchat>(entity =>
            {
                entity.ToTable("OffreAchat");
                entity.HasKey(o => o.IdOffre);

                // ✅ PostgreSQL : uuid + gen_random_uuid() au lieu de uniqueidentifier + NEWID()
                entity.Property(o => o.IdOffre)
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(o => o.NomFichier)
                    .IsRequired()
                    .HasMaxLength(300);

                // ✅ PostgreSQL : bigint reste pareil
                entity.Property(o => o.Taille)
                    .HasColumnType("bigint");

                // ✅ PostgreSQL : bytea au lieu de varbinary(max)
                entity.Property(o => o.ContenuPdf)
                    .HasColumnType("bytea");

                entity.Property(o => o.EstChoisie)
                    .HasDefaultValue(false);
            });

            // === CHAT MESSAGE ===
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.Property(m => m.Content)
                    .IsRequired(false)
                    .HasMaxLength(4000)
                    .HasDefaultValue(string.Empty);

                // ✅ PostgreSQL : text au lieu de nvarchar(max)
                entity.Property(m => m.AudioData)
                    .HasColumnType("text")
                    .IsRequired(false);

                entity.Property(m => m.AudioDurationSeconds)
                    .HasDefaultValue(0);

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

            // === PROJECT ===
            modelBuilder.Entity<Project>(entity =>
            {
                entity.ToTable("Projets");
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
                entity.Property(p => p.Budget).HasColumnType("numeric(18,2)");

                // ✅ PostgreSQL : NOW() au lieu de GETUTCDATE()
                entity.Property(p => p.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(p => p.UpdatedAt).HasDefaultValueSql("NOW()");

                entity.HasMany(p => p.Affectations)
                    .WithOne(a => a.Projet)
                    .HasForeignKey(a => a.ProjetId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // === LIGNE DEMANDE ===
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

            // === COMMENTAIRE MATERIEL ===
            modelBuilder.Entity<CommentaireMateriel>(e =>
            {
                e.ToTable("CommentairesMateriel");
                e.HasKey(c => c.Id);
                e.Property(c => c.Contenu).HasMaxLength(1000).IsRequired();
                e.HasOne(c => c.Materiel)
                    .WithMany()
                    .HasForeignKey(c => c.MaterielId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(c => c.Utilisateur)
                    .WithMany()
                    .HasForeignKey(c => c.UtilisateurId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // === NOTIFICATION ===
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Titre).IsRequired().HasMaxLength(200);
                entity.Property(n => n.Message).IsRequired().HasMaxLength(1000);
                entity.Property(n => n.RoleDestinataire).HasMaxLength(50);
                entity.HasIndex(n => new { n.EstLue, n.RoleDestinataire });
                entity.HasIndex(n => n.DateCreation);
                entity.HasOne(n => n.Affectation)
                    .WithMany()
                    .HasForeignKey(n => n.AffectationId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(n => n.Utilisateur)
                    .WithMany()
                    .HasForeignKey(n => n.UtilisateurId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // === AUDIT LOG ===
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Utilisateur).IsRequired().HasMaxLength(200);
                entity.Property(a => a.Email).IsRequired().HasMaxLength(200);
                entity.Property(a => a.Action).IsRequired().HasMaxLength(100);
                entity.Property(a => a.Categorie).IsRequired().HasMaxLength(100);
                entity.Property(a => a.Entite).IsRequired().HasMaxLength(200);
                entity.Property(a => a.Details).HasMaxLength(2000);
                entity.HasIndex(a => a.Timestamp);
                entity.HasIndex(a => a.UserId);
                entity.HasOne(a => a.User)
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
            });

            // === ARTICLE HISTORIQUE ===
            modelBuilder.Entity<ArticleHistorique>(entity =>
            {
                entity.HasKey(h => h.Id);
                entity.Property(h => h.TypeEvenement)
                    .HasConversion<string>()
                    .HasMaxLength(50);
                entity.Property(h => h.Description).HasMaxLength(500);

                // ✅ PostgreSQL : NOW() au lieu de GETUTCDATE()
                entity.Property(h => h.DateEvenement).HasDefaultValueSql("NOW()");

                entity.HasOne(h => h.Article)
                    .WithMany(a => a.Historiques)
                    .HasForeignKey(h => h.ArticleId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(h => h.Utilisateur)
                    .WithMany()
                    .HasForeignKey(h => h.UtilisateurId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
                entity.HasIndex(h => h.ArticleId);
                entity.HasIndex(h => h.DateEvenement);
            });
        }
    }
}