using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class CommandeService : ICommandeService
    {
        private readonly AppDbContext _db;
        private readonly IDashboardNotifier _notifier;
        private readonly IAuditLogService _audit;
        private readonly IArticleBiographieService _biographie;

        public CommandeService(
            AppDbContext db,
            IDashboardNotifier notifier,
            IAuditLogService audit,
            IArticleBiographieService biographie)
        {
            _db         = db;
            _notifier   = notifier;
            _audit      = audit;
            _biographie = biographie;
        }

        // ── Helper : force UTC ────────────────────────────────────────────────
        private static DateTime  ToUtc(DateTime  dt) => DateTime.SpecifyKind(dt,       DateTimeKind.Utc);
        private static DateTime? ToUtc(DateTime? dt) => dt.HasValue ? DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc) : null;
        // ─────────────────────────────────────────────────────────────────────

        private static ArticleDto ToArticleDto(ArticleIndividuel a) => new()
        {
            Id             = a.Id,
            NumeroSerie    = a.NumeroSerie,
            Statut         = a.Statut.ToString(),
            CommandeId     = a.CommandeId,
            NumeroCommande = a.Commande?.NumeroCommande ?? string.Empty
        };

        private static CommandeDto ToCommandeDto(Commande c) => new()
        {
            Id                = c.Id,
            NumeroCommande    = c.NumeroCommande,
            MaterielId        = c.MaterielId,
            NomMateriel       = c.Materiel?.Designation ?? string.Empty,
            ReferenceMateriel = c.Materiel?.Reference   ?? string.Empty,
            FournisseurId     = c.FournisseurId ?? 0,
            NomFournisseur    = c.Fournisseur?.Nom ?? string.Empty,
            QuantiteAchetee   = c.QuantiteAchetee,
            DateAchat         = c.DateAchat,
            DateLivraison     = c.DateLivraison,
            DateFinGarantie   = c.DateFinGarantie,
            Articles          = c.Articles.Select(a => new ArticleDto
            {
                Id             = a.Id,
                NumeroSerie    = a.NumeroSerie,
                Statut         = a.Statut.ToString(),
                CommandeId     = a.CommandeId,
                NumeroCommande = c.NumeroCommande
            }).ToList()
        };

        public async Task<IEnumerable<CommandeDto>> GetAllAsync()
        {
            var list = await _db.Commandes
                .Include(c => c.Materiel).Include(c => c.Fournisseur).Include(c => c.Articles)
                .OrderByDescending(c => c.DateAchat).AsNoTracking().ToListAsync();
            return list.Select(ToCommandeDto);
        }

        public async Task<IEnumerable<CommandeDto>> GetByMaterielAsync(int materielId)
        {
            var list = await _db.Commandes
                .Where(c => c.MaterielId == materielId)
                .Include(c => c.Materiel).Include(c => c.Fournisseur).Include(c => c.Articles)
                .OrderByDescending(c => c.DateAchat).AsNoTracking().ToListAsync();
            return list.Select(ToCommandeDto);
        }

        public async Task<CommandeDto?> GetByIdAsync(int id)
        {
            var c = await _db.Commandes
                .Include(x => x.Materiel).Include(x => x.Fournisseur).Include(x => x.Articles)
                .AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return c is null ? null : ToCommandeDto(c);
        }

        public async Task<IEnumerable<ArticleDto>> GetArticlesByMaterielAsync(int materielId)
        {
            var arts = await _db.ArticlesIndividuels
                .Where(a => a.MaterielId == materielId)
                .Include(a => a.Commande).AsNoTracking().ToListAsync();
            return arts.Select(ToArticleDto);
        }

        public async Task<IEnumerable<ArticleDto>> GetArticlesByCommandeAsync(int commandeId)
        {
            var arts = await _db.ArticlesIndividuels
                .Where(a => a.CommandeId == commandeId)
                .Include(a => a.Commande).AsNoTracking().ToListAsync();
            return arts.Select(ToArticleDto);
        }

        public async Task<IEnumerable<LigneMaterielDto>> GetLignesMaterielsAsync()
        {
            var materiels = await _db.Materiels.AsNoTracking().OrderBy(m => m.Designation).ToListAsync();
            var commandes = await _db.Commandes.Include(c => c.Fournisseur).Include(c => c.Articles).AsNoTracking().ToListAsync();

            return materiels.Select(m => new LigneMaterielDto
            {
                MaterielId    = m.Id,
                Reference     = m.Reference,
                Designation   = m.Designation,
                Description   = m.Description,
                Categorie     = m.Categorie,
                QuantiteStock = m.QuantiteStock,
                QuantiteMin   = m.QuantiteMin,
                Unite         = m.Unite,
                ImageUrl      = m.ImageUrl,
                DateAjout     = m.DateAjout,
                Commandes     = commandes
                    .Where(c => c.MaterielId == m.Id)
                    .OrderByDescending(c => c.DateAchat)
                    .Select(c => new CommandeDto
                    {
                        Id              = c.Id,
                        NumeroCommande  = c.NumeroCommande,
                        MaterielId      = c.MaterielId,
                        FournisseurId   = c.FournisseurId ?? 0,
                        NomFournisseur  = c.Fournisseur?.Nom ?? string.Empty,
                        QuantiteAchetee = c.QuantiteAchetee,
                        DateAchat       = c.DateAchat,
                        DateLivraison   = c.DateLivraison,
                        DateFinGarantie = c.DateFinGarantie,
                        Articles        = c.Articles.Select(a => new ArticleDto
                        {
                            Id             = a.Id,
                            NumeroSerie    = a.NumeroSerie,
                            Statut         = a.Statut.ToString(),
                            CommandeId     = a.CommandeId,
                            NumeroCommande = c.NumeroCommande
                        }).ToList()
                    }).ToList()
            }).ToList();
        }

        public async Task<IEnumerable<LigneCommandeMaterielDto>> GetLignesCommandesAsync()
        {
            var commandes = await _db.Commandes
                .Include(c => c.Materiel).Include(c => c.Fournisseur).Include(c => c.Articles)
                .AsNoTracking().ToListAsync();

            var materielIdsAvecCommande = commandes.Select(c => c.MaterielId).Distinct().ToHashSet();
            var materielsSeuls = await _db.Materiels.Where(m => !materielIdsAvecCommande.Contains(m.Id)).AsNoTracking().ToListAsync();

            var result = new List<LigneCommandeMaterielDto>();

            foreach (var c in commandes)
            {
                var m = c.Materiel;
                result.Add(new LigneCommandeMaterielDto
                {
                    MaterielId = m.Id, Reference = m.Reference, Designation = m.Designation,
                    Description = m.Description, Categorie = m.Categorie, QuantiteStock = m.QuantiteStock,
                    QuantiteMin = m.QuantiteMin, Unite = m.Unite, ImageUrl = m.ImageUrl, DateAjout = m.DateAjout,
                    CommandeId = c.Id, NumeroCommande = c.NumeroCommande, FournisseurId = c.FournisseurId ?? 0,
                    NomFournisseur = c.Fournisseur?.Nom ?? string.Empty, QuantiteAchetee = c.QuantiteAchetee,
                    DateAchat = c.DateAchat, DateLivraison = c.DateLivraison, DateFinGarantie = c.DateFinGarantie,
                    NbArticles = c.Articles.Count, NbDisponibles = c.Articles.Count(a => a.Statut == StatutArticle.Disponible)
                });
            }

            foreach (var m in materielsSeuls)
            {
                result.Add(new LigneCommandeMaterielDto
                {
                    MaterielId = m.Id, Reference = m.Reference, Designation = m.Designation,
                    Description = m.Description, Categorie = m.Categorie, QuantiteStock = m.QuantiteStock,
                    QuantiteMin = m.QuantiteMin, Unite = m.Unite, ImageUrl = m.ImageUrl, DateAjout = m.DateAjout,
                    CommandeId = 0, NumeroCommande = string.Empty, NomFournisseur = string.Empty,
                    QuantiteAchetee = 0, DateAchat = DateTime.MinValue, NbArticles = 0, NbDisponibles = 0
                });
            }

            return result.OrderBy(r => r.Designation).ThenByDescending(r => r.DateAchat);
        }

        public async Task<CommandeReponseDto> CreerAsync(CreerCommandeDto dto)
        {
            // ── Matériel obligatoire ──────────────────────────────────────────
            var materiel = await _db.Materiels.FindAsync(dto.MaterielId);
            if (materiel is null)
                return new CommandeReponseDto { Succes = false, Message = "Matériel introuvable." };

            // ── Fournisseur OPTIONNEL ─────────────────────────────────────────
            int? fournisseurIdFinal = null;
            Fournisseur? fournisseur = null;

            if (dto.FournisseurId > 0)
            {
                fournisseur = await _db.Fournisseurs.FindAsync(dto.FournisseurId);
                if (fournisseur is null)
                    return new CommandeReponseDto
                    {
                        Succes  = false,
                        Message = $"Fournisseur id={dto.FournisseurId} introuvable. Laissez le champ vide ou saisissez un nom."
                    };
                fournisseurIdFinal = fournisseur.IdFournisseur;
            }
            else if (!string.IsNullOrWhiteSpace(dto.NomFournisseurLibre))
            {
                var nom = dto.NomFournisseurLibre.Trim();
                fournisseur = await _db.Fournisseurs
                    .FirstOrDefaultAsync(f => f.Nom.ToLower() == nom.ToLower());

                if (fournisseur is null)
                {
                    fournisseur = new Fournisseur { Nom = nom };
                    _db.Fournisseurs.Add(fournisseur);
                    await _db.SaveChangesAsync();
                }
                fournisseurIdFinal = fournisseur.IdFournisseur;
            }

            // ── Unicité numéro commande ───────────────────────────────────────
            if (await _db.Commandes.AnyAsync(c => c.NumeroCommande == dto.NumeroCommande.Trim()))
                return new CommandeReponseDto { Succes = false, Message = "Ce numéro de commande existe déjà." };

            // ── Validation numéros de série ───────────────────────────────────
            var numerosSerieFournis = dto.NumerosSerie
                .Where(ns => !string.IsNullOrWhiteSpace(ns)).Select(ns => ns!.Trim()).ToList();

            var doublonsInternes = numerosSerieFournis
                .GroupBy(ns => ns, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (doublonsInternes.Any())
                return new CommandeReponseDto
                {
                    Succes  = false,
                    Message = $"Numéro(s) de série en double : {string.Join(", ", doublonsInternes)}."
                };

            if (numerosSerieFournis.Any())
            {
                var existants = await _db.ArticlesIndividuels
                    .Where(a => numerosSerieFournis.Contains(a.NumeroSerie!))
                    .Select(a => a.NumeroSerie!).ToListAsync();
                if (existants.Any())
                    return new CommandeReponseDto
                    {
                        Succes  = false,
                        Message = $"Numéro(s) de série déjà utilisé(s) : {string.Join(", ", existants)}."
                    };
            }

            // ── Transaction principale ────────────────────────────────────────
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // ── Normalisation UTC de toutes les dates entrantes ───────────
                var dateAchat       = dto.DateAchat == default
                                        ? DateTime.UtcNow
                                        : ToUtc(dto.DateAchat);          // ← UTC
                var dateLivraison   = ToUtc(dto.DateLivraison);           // ← UTC
                var dateFinGarantie = ToUtc(dto.DateFinGarantie);         // ← UTC
                var dateRef         = dateLivraison ?? dateAchat;         // ← déjà UTC
                // ─────────────────────────────────────────────────────────────

                var commande = new Commande
                {
                    NumeroCommande  = dto.NumeroCommande.Trim(),
                    MaterielId      = dto.MaterielId,
                    FournisseurId   = fournisseurIdFinal,
                    QuantiteAchetee = dto.QuantiteAchetee,
                    DateAchat       = dateAchat,         // ← UTC
                    DateLivraison   = dateLivraison,     // ← UTC
                    DateFinGarantie = dateFinGarantie    // ← UTC
                };
                _db.Commandes.Add(commande);
                await _db.SaveChangesAsync();

                // ── Articles individuels ──────────────────────────────────────
                var nouveauxArticles = new List<ArticleIndividuel>();
                for (int i = 0; i < dto.QuantiteAchetee; i++)
                {
                    var ns = (i < dto.NumerosSerie.Count) ? dto.NumerosSerie[i] : null;
                    var article = new ArticleIndividuel
                    {
                        NumeroSerie = string.IsNullOrWhiteSpace(ns) ? null : ns.Trim(),
                        Statut      = StatutArticle.Disponible,
                        MaterielId  = dto.MaterielId,
                        CommandeId  = commande.Id
                    };
                    _db.ArticlesIndividuels.Add(article);
                    nouveauxArticles.Add(article);
                }

                materiel.QuantiteStock += dto.QuantiteAchetee;
                await _db.SaveChangesAsync();

                // ── Biographie ────────────────────────────────────────────────
                var nomFourn = fournisseur?.Nom ?? "—";

                foreach (var article in nouveauxArticles)
                {
                    _db.ArticleHistoriques.Add(new ArticleHistorique
                    {
                        ArticleId     = article.Id,
                        TypeEvenement = TypeEvenementArticle.Acquisition,
                        UtilisateurId = null,
                        DateEvenement = dateRef,     // ← déjà UTC
                        Description   = $"Commande {commande.NumeroCommande} — {nomFourn}"
                    });

                    _db.ArticleHistoriques.Add(new ArticleHistorique
                    {
                        ArticleId     = article.Id,
                        TypeEvenement = TypeEvenementArticle.MiseEnStock,
                        UtilisateurId = null,
                        DateEvenement = dateRef,     // ← déjà UTC
                        Description   = $"Mis en stock — {materiel.Designation}"
                    });
                }

                await _db.SaveChangesAsync();

                await _notifier.NotifyAsync();
                await _notifier.NotifyITAsync();
                await _notifier.NotifyMemoryAsync("GraphNodeUpdated",
                    new { Type = "materiel", NodeId = $"m-{dto.MaterielId}" });
                await transaction.CommitAsync();

                await _audit.LogAsync(new CreateAuditLogDto
                {
                    Utilisateur = dto.Utilisateur,
                    Email       = "system",
                    Action      = IAuditLogService.Actions.Creation,
                    Categorie   = IAuditLogService.Categories.Commande,
                    Entite      = $"Commande #{commande.NumeroCommande}",
                    Details     = $"Nouvelle commande créée : \"{commande.NumeroCommande}\" (Qté: {commande.QuantiteAchetee})"
                });

                return new CommandeReponseDto
                {
                    Succes     = true,
                    Message    = $"Commande {commande.NumeroCommande} créée avec {dto.QuantiteAchetee} article(s).",
                    IdCommande = commande.Id
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var inner = ex.InnerException?.Message ?? ex.Message;
                var msg   = inner.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                         || inner.Contains("unique", StringComparison.OrdinalIgnoreCase)
                         || inner.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                    ? "Un numéro de série est déjà utilisé dans la base de données."
                    : $"Erreur lors de la création : {inner}";
                return new CommandeReponseDto { Succes = false, Message = msg };
            }
        }

        public async Task<CommandeReponseDto> ModifierAsync(ModifierCommandeDto dto)
        {
            var commande = await _db.Commandes.FindAsync(dto.Id);
            if (commande is null)
                return new CommandeReponseDto { Succes = false, Message = "Commande introuvable." };

            var numTrimmed = dto.NumeroCommande.Trim();
            if (await _db.Commandes.AnyAsync(c => c.NumeroCommande == numTrimmed && c.Id != dto.Id))
                return new CommandeReponseDto { Succes = false, Message = "Ce numéro de commande est déjà utilisé." };

            // ── Résolution fournisseur (optionnel, même logique) ──────────────
            int? fournisseurIdFinal = commande.FournisseurId;

            if (dto.FournisseurId > 0)
            {
                var f = await _db.Fournisseurs.FindAsync(dto.FournisseurId);
                if (f is not null) fournisseurIdFinal = f.IdFournisseur;
            }
            else if (!string.IsNullOrWhiteSpace(dto.NomFournisseurLibre))
            {
                var nom = dto.NomFournisseurLibre.Trim();
                var existing = await _db.Fournisseurs
                    .FirstOrDefaultAsync(f => f.Nom.ToLower() == nom.ToLower());
                if (existing is not null)
                {
                    fournisseurIdFinal = existing.IdFournisseur;
                }
                else
                {
                    var newF = new Fournisseur { Nom = nom };
                    _db.Fournisseurs.Add(newF);
                    await _db.SaveChangesAsync();
                    fournisseurIdFinal = newF.IdFournisseur;
                }
            }

            commande.NumeroCommande  = numTrimmed;
            commande.FournisseurId   = fournisseurIdFinal;
            commande.DateAchat       = ToUtc(dto.DateAchat);        // ← UTC
            commande.DateLivraison   = ToUtc(dto.DateLivraison);    // ← UTC
            commande.DateFinGarantie = ToUtc(dto.DateFinGarantie);  // ← UTC

            await _db.SaveChangesAsync();
            await _notifier.NotifyAsync();
            await _notifier.NotifyITAsync();
            await _notifier.NotifyMemoryAsync("GraphNodeUpdated",
                new { Type = "materiel", NodeId = $"m-{commande.MaterielId}" });

            await _audit.LogAsync(new CreateAuditLogDto
            {
                Utilisateur = dto.Utilisateur,
                Email       = "system",
                Action      = IAuditLogService.Actions.Modification,
                Categorie   = IAuditLogService.Categories.Commande,
                Entite      = $"Commande #{commande.NumeroCommande}",
                Details     = $"Commande modifiée : \"{commande.NumeroCommande}\" (Qté: {commande.QuantiteAchetee})"
            });

            return new CommandeReponseDto
            {
                Succes     = true,
                Message    = $"Commande {commande.NumeroCommande} modifiée.",
                IdCommande = commande.Id
            };
        }

        public async Task<CommandeReponseDto> SupprimerAsync(string utilisateur, int id)
        {
            var commande = await _db.Commandes.Include(c => c.Articles).FirstOrDefaultAsync(c => c.Id == id);
            if (commande is null)
                return new CommandeReponseDto { Succes = false, Message = "Commande introuvable." };

            var articleIds = commande.Articles.Select(a => a.Id).ToList();
            if (articleIds.Any())
            {
                var incidents = await _db.Incidents
                    .Where(i => i.ArticleId.HasValue && articleIds.Contains(i.ArticleId.Value)).ToListAsync();
                if (incidents.Any()) _db.Incidents.RemoveRange(incidents);

                var historiques = await _db.ArticleHistoriques
                    .Where(h => articleIds.Contains(h.ArticleId)).ToListAsync();
                if (historiques.Any()) _db.ArticleHistoriques.RemoveRange(historiques);

                var articlesAvecAffect = await _db.ArticlesIndividuels
                    .Where(a => articleIds.Contains(a.Id) && a.AffectationId.HasValue).ToListAsync();
                foreach (var art in articlesAvecAffect)
                    art.AffectationId = null;
            }

            var materiel = await _db.Materiels.FindAsync(commande.MaterielId);
            if (materiel is not null)
                materiel.QuantiteStock = Math.Max(0, materiel.QuantiteStock - commande.QuantiteAchetee);

            _db.ArticlesIndividuels.RemoveRange(commande.Articles);
            _db.Commandes.Remove(commande);
            await _db.SaveChangesAsync();

            await _notifier.NotifyMemoryAsync("GraphNodeUpdated",
                new { Type = "materiel", NodeId = $"m-{commande.MaterielId}" });

            await _audit.LogAsync(new CreateAuditLogDto
            {
                Utilisateur = utilisateur,
                Email       = "system",
                Action      = IAuditLogService.Actions.Suppression,
                Categorie   = IAuditLogService.Categories.Commande,
                Entite      = $"Commande #{commande.NumeroCommande}",
                Details     = $"Commande supprimée : \"{commande.NumeroCommande}\" (Qté: {commande.QuantiteAchetee})"
            });

            return new CommandeReponseDto
            {
                Succes  = true,
                Message = $"Commande {commande.NumeroCommande} supprimée."
            };
        }
    }
}