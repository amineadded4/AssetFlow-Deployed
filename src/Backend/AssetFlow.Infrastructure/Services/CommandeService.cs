// ============================================================
// AssetFlow.Infrastructure / Services / CommandeService.cs
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
            Id            = a.Id,
            NumeroSerie   = a.NumeroSerie,
            Statut        = a.Statut.ToString(),
            CommandeId    = a.CommandeId,
            NumeroCommande = a.Commande?.NumeroCommande ?? string.Empty
        };

        private static CommandeDto ToCommandeDto(Commande c) => new()
        {
            Id               = c.Id,
            NumeroCommande   = c.NumeroCommande,
            MaterielId       = c.MaterielId,
            NomMateriel      = c.Materiel?.Designation ?? string.Empty,
            ReferenceMateriel = c.Materiel?.Reference ?? string.Empty,
            FournisseurId    = c.FournisseurId,
            NomFournisseur   = c.Fournisseur?.Nom ?? string.Empty,
            QuantiteAchetee  = c.QuantiteAchetee,
            DateAchat        = c.DateAchat,
            DateLivraison    = c.DateLivraison,
            DateFinGarantie  = c.DateFinGarantie,
            Articles         = c.Articles.Select(a => new ArticleDto
            {
                Id             = a.Id,
                NumeroSerie    = a.NumeroSerie,
                Statut         = a.Statut.ToString(),
                CommandeId     = a.CommandeId,
                NumeroCommande = c.NumeroCommande
            }).ToList()
        };

        // ── Lecture ───────────────────────────────────────────────
        public async Task<IEnumerable<CommandeDto>> GetAllAsync()
        {
            var list = await _db.Commandes
                .Include(c => c.Materiel)
                .Include(c => c.Fournisseur)
                .Include(c => c.Articles)
                .OrderByDescending(c => c.DateAchat)
                .AsNoTracking()
                .ToListAsync();
            return list.Select(ToCommandeDto);
        }

        public async Task<IEnumerable<CommandeDto>> GetByMaterielAsync(int materielId)
        {
            var list = await _db.Commandes
                .Where(c => c.MaterielId == materielId)
                .Include(c => c.Materiel)
                .Include(c => c.Fournisseur)
                .Include(c => c.Articles)
                .OrderByDescending(c => c.DateAchat)
                .AsNoTracking()
                .ToListAsync();
            return list.Select(ToCommandeDto);
        }

        public async Task<CommandeDto?> GetByIdAsync(int id)
        {
            var c = await _db.Commandes
                .Include(x => x.Materiel)
                .Include(x => x.Fournisseur)
                .Include(x => x.Articles)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            return c is null ? null : ToCommandeDto(c);
        }

        public async Task<IEnumerable<ArticleDto>> GetArticlesByMaterielAsync(int materielId)
        {
            var articles = await _db.ArticlesIndividuels
                .Where(a => a.MaterielId == materielId)
                .Include(a => a.Commande)
                .AsNoTracking()
                .ToListAsync();
            return articles.Select(ToArticleDto);
        }

        // ── Vue enrichie pour tableau Matériel ────────────────────
        public async Task<IEnumerable<MaterielAvecCommandeDto>> GetMaterielsAvecDerniereCommandeAsync()
        {
            var materiels = await _db.Materiels
                .AsNoTracking()
                .OrderBy(m => m.Designation)
                .ToListAsync();

            var result = new List<MaterielAvecCommandeDto>();

            foreach (var m in materiels)
            {
                // Dernière commande pour ce matériel
                var derniereCommande = await _db.Commandes
                    .Where(c => c.MaterielId == m.Id)
                    .Include(c => c.Fournisseur)
                    .OrderByDescending(c => c.DateAchat)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                // Stats articles
                var nbArticles    = await _db.ArticlesIndividuels.CountAsync(a => a.MaterielId == m.Id);
                var nbDisponibles = await _db.ArticlesIndividuels.CountAsync(a => a.MaterielId == m.Id && a.Statut == StatutArticle.Disponible);

                result.Add(new MaterielAvecCommandeDto
                {
                    Id               = m.Id,
                    Reference        = m.Reference,
                    Designation      = m.Designation,
                    Description      = m.Description,
                    Categorie        = m.Categorie,
                    QuantiteStock    = m.QuantiteStock,
                    QuantiteMin      = m.QuantiteMin,
                    Unite            = m.Unite,
                    Etat             = m.Etat.ToString(),
                    ImageUrl         = m.ImageUrl,
                    DateAjout        = m.DateAjout,
                    NumeroCommande   = derniereCommande?.NumeroCommande,
                    NomFournisseur   = derniereCommande?.Fournisseur?.Nom,
                    FournisseurId    = derniereCommande?.FournisseurId,
                    QuantiteAchetee  = derniereCommande?.QuantiteAchetee ?? 0,
                    DateAchat        = derniereCommande?.DateAchat,
                    DateLivraison    = derniereCommande?.DateLivraison,
                    DateFinGarantie  = derniereCommande?.DateFinGarantie,
                    NbArticles       = nbArticles,
                    NbDisponibles    = nbDisponibles
                });
            }

            return result;
        }

        // ── Création ───────────────────────────────────────────────
        public async Task<CommandeReponseDto> CreerAsync(CreerCommandeDto dto)
        {
            // Vérifier matériel
            var materiel = await _db.Materiels.FindAsync(dto.MaterielId);
            if (materiel is null)
                return new CommandeReponseDto { Succes = false, Message = "Matériel introuvable." };

            // Vérifier fournisseur
            var fournisseur = await _db.Fournisseurs.FindAsync(dto.FournisseurId);
            if (fournisseur is null)
                return new CommandeReponseDto { Succes = false, Message = "Fournisseur introuvable." };

            // Vérifier unicité numéro de commande
            if (await _db.Commandes.AnyAsync(c => c.NumeroCommande == dto.NumeroCommande.Trim()))
                return new CommandeReponseDto { Succes = false, Message = "Ce numéro de commande existe déjà." };

            // Créer la commande
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
            await _db.SaveChangesAsync(); // On a besoin de l'ID de commande

            // Générer les articles individuels
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

            // Mettre à jour QuantiteStock du matériel
            materiel.QuantiteStock += dto.QuantiteAchetee;
            await _db.SaveChangesAsync();

            return new CommandeReponseDto
            {
                Succes     = true,
                Message    = $"Commande {commande.NumeroCommande} créée avec {dto.QuantiteAchetee} article(s).",
                IdCommande = commande.Id
            };
        }

        // ── Suppression ───────────────────────────────────────────
        public async Task<CommandeReponseDto> SupprimerAsync(int id)
        {
            var commande = await _db.Commandes
                .Include(c => c.Articles)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (commande is null)
                return new CommandeReponseDto { Succes = false, Message = "Commande introuvable." };

            // Décrémenter QuantiteStock
            var materiel = await _db.Materiels.FindAsync(commande.MaterielId);
            if (materiel is not null)
                materiel.QuantiteStock = Math.Max(0, materiel.QuantiteStock - commande.QuantiteAchetee);

            _db.ArticlesIndividuels.RemoveRange(commande.Articles);
            _db.Commandes.Remove(commande);
            await _db.SaveChangesAsync();

            return new CommandeReponseDto { Succes = true, Message = "Commande supprimée." };
        }
    }
}