// ============================================================
// AssetFlow.Infrastructure / Services / CommandeService.cs — v4
// CreerAsync : validation N° série + transaction atomique
// ============================================================

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
        public CommandeService(AppDbContext db) => _db = db;

        // ── Helpers ───────────────────────────────────────────────
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

        // ── Lecture basique ───────────────────────────────────────
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

        // ── Vue principale : UNE LIGNE PAR COMMANDE ───────────────
        public async Task<IEnumerable<LigneCommandeMaterielDto>> GetLignesCommandesAsync()
        {
            // 1. Toutes les commandes avec leurs articles et leur matériel
            var commandes = await _db.Commandes
                .Include(c => c.Materiel)
                .Include(c => c.Fournisseur)
                .Include(c => c.Articles)
                .AsNoTracking()
                .ToListAsync();

            // 2. Matériels sans aucune commande
            var materielIdsAvecCommande = commandes.Select(c => c.MaterielId).Distinct().ToHashSet();
            var materielsSeuls = await _db.Materiels
                .Where(m => !materielIdsAvecCommande.Contains(m.Id))
                .AsNoTracking().ToListAsync();

            var result = new List<LigneCommandeMaterielDto>();

            // 3. Une ligne par commande
            foreach (var c in commandes)
            {
                var m = c.Materiel;
                result.Add(new LigneCommandeMaterielDto
                {
                    // Matériel
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
                    // Commande
                    CommandeId      = c.Id,
                    NumeroCommande  = c.NumeroCommande,
                    FournisseurId   = c.FournisseurId ?? 0,
                    NomFournisseur  = c.Fournisseur?.Nom ?? string.Empty,
                    QuantiteAchetee = c.QuantiteAchetee,
                    DateAchat       = c.DateAchat,
                    DateLivraison   = c.DateLivraison,
                    DateFinGarantie = c.DateFinGarantie,
                    // Articles de CETTE commande
                    NbArticles    = c.Articles.Count,
                    NbDisponibles = c.Articles.Count(a => a.Statut == StatutArticle.Disponible)
                });
            }

            // 4. Une ligne pour les matériels sans commande
            foreach (var m in materielsSeuls)
            {
                result.Add(new LigneCommandeMaterielDto
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
                    CommandeId    = 0,
                    NumeroCommande  = string.Empty,
                    NomFournisseur  = string.Empty,
                    QuantiteAchetee = 0,
                    DateAchat       = DateTime.MinValue,
                    NbArticles      = 0,
                    NbDisponibles   = 0
                });
            }

            // Trier : produit alphabétique, puis date achat décroissante
            return result
                .OrderBy(r => r.Designation)
                .ThenByDescending(r => r.DateAchat);
        }

        // ── Création ───────────────────────────────────────────────
        public async Task<CommandeReponseDto> CreerAsync(CreerCommandeDto dto)
        {
            // 1. Vérifications préalables
            var materiel = await _db.Materiels.FindAsync(dto.MaterielId);
            if (materiel is null)
                return new CommandeReponseDto { Succes = false, Message = "Matériel introuvable." };

            var fournisseur = await _db.Fournisseurs.FindAsync(dto.FournisseurId);
            if (fournisseur is null)
                return new CommandeReponseDto { Succes = false, Message = "Fournisseur introuvable." };

            if (await _db.Commandes.AnyAsync(c => c.NumeroCommande == dto.NumeroCommande.Trim()))
                return new CommandeReponseDto { Succes = false, Message = "Ce numéro de commande existe déjà." };

            // 2. Vérification des numéros de série
            var numerosSerieFournis = dto.NumerosSerie
                .Where(ns => !string.IsNullOrWhiteSpace(ns))
                .Select(ns => ns!.Trim())
                .ToList();

            // Doublons dans la saisie elle-même
            var doublonsInternes = numerosSerieFournis
                .GroupBy(ns => ns, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (doublonsInternes.Any())
                return new CommandeReponseDto
                {
                    Succes  = false,
                    Message = $"Numéro(s) de série en double dans la saisie : {string.Join(", ", doublonsInternes)}."
                };

            // Doublons avec la base de données
            if (numerosSerieFournis.Any())
            {
                var existants = await _db.ArticlesIndividuels
                    .Where(a => numerosSerieFournis.Contains(a.NumeroSerie!))
                    .Select(a => a.NumeroSerie!)
                    .ToListAsync();

                if (existants.Any())
                    return new CommandeReponseDto
                    {
                        Succes  = false,
                        Message = $"Numéro(s) de série déjà utilisé(s) : {string.Join(", ", existants)}."
                    };
            }

            // 3. Tout est valide → création dans une transaction atomique
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var commande = new Commande
                {
                    NumeroCommande  = dto.NumeroCommande.Trim(),
                    MaterielId      = dto.MaterielId,
                    FournisseurId   = dto.FournisseurId,
                    QuantiteAchetee = dto.QuantiteAchetee,
                    DateAchat       = dto.DateAchat,
                    DateLivraison   = dto.DateLivraison,
                    DateFinGarantie = dto.DateFinGarantie
                };
                _db.Commandes.Add(commande);
                await _db.SaveChangesAsync();

                for (int i = 0; i < dto.QuantiteAchetee; i++)
                {
                    var ns = (i < dto.NumerosSerie.Count) ? dto.NumerosSerie[i] : null;
                    _db.ArticlesIndividuels.Add(new ArticleIndividuel
                    {
                        NumeroSerie = string.IsNullOrWhiteSpace(ns) ? null : ns.Trim(),
                        Statut      = StatutArticle.Disponible,
                        MaterielId  = dto.MaterielId,
                        CommandeId  = commande.Id
                    });
                }

                materiel.QuantiteStock += dto.QuantiteAchetee;
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

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
                // Message lisible si violation de contrainte unique (filet de sécurité)
                var inner = ex.InnerException?.Message ?? ex.Message;
                var msg   = inner.Contains("UNIQUE",    StringComparison.OrdinalIgnoreCase)
                         || inner.Contains("unique",    StringComparison.OrdinalIgnoreCase)
                         || inner.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                    ? "Un numéro de série est déjà utilisé dans la base de données."
                    : $"Erreur lors de la création : {inner}";

                return new CommandeReponseDto { Succes = false, Message = msg };
            }
        }

        // ── Suppression ───────────────────────────────────────────
        public async Task<CommandeReponseDto> SupprimerAsync(int id)
        {
            var commande = await _db.Commandes
                .Include(c => c.Articles)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (commande is null)
                return new CommandeReponseDto { Succes = false, Message = "Commande introuvable." };

            var materiel = await _db.Materiels.FindAsync(commande.MaterielId);
            if (materiel is not null)
                materiel.QuantiteStock = Math.Max(0, materiel.QuantiteStock - commande.QuantiteAchetee);

            _db.ArticlesIndividuels.RemoveRange(commande.Articles);
            _db.Commandes.Remove(commande);
            await _db.SaveChangesAsync();

            return new CommandeReponseDto { Succes = true, Message = $"Commande {commande.NumeroCommande} supprimée." };
        }
    }
}