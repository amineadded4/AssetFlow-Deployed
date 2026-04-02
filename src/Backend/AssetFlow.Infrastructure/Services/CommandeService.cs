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

        // ── UNE LIGNE PAR MATERIEL ────────────────────────────────
        public async Task<IEnumerable<LigneMaterielDto>> GetLignesMaterielsAsync()
        {
            var materiels = await _db.Materiels
                .AsNoTracking()
                .OrderBy(m => m.Designation)
                .ToListAsync();

            var commandes = await _db.Commandes
                .Include(c => c.Fournisseur)
                .Include(c => c.Articles)
                .AsNoTracking()
                .ToListAsync();

            var result = materiels.Select(m => new LigneMaterielDto
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

            return result;
        }
        public async Task<IEnumerable<LigneCommandeMaterielDto>> GetLignesCommandesAsync()
        {
            var commandes = await _db.Commandes
                .Include(c => c.Materiel)
                .Include(c => c.Fournisseur)
                .Include(c => c.Articles)
                .AsNoTracking()
                .ToListAsync();

            var materielIdsAvecCommande = commandes.Select(c => c.MaterielId).Distinct().ToHashSet();
            var materielsSeuls = await _db.Materiels
                .Where(m => !materielIdsAvecCommande.Contains(m.Id))
                .AsNoTracking().ToListAsync();

            var result = new List<LigneCommandeMaterielDto>();

            foreach (var c in commandes)
            {
                var m = c.Materiel;
                result.Add(new LigneCommandeMaterielDto
                {
                    MaterielId      = m.Id,
                    Reference       = m.Reference,
                    Designation     = m.Designation,
                    Description     = m.Description,
                    Categorie       = m.Categorie,
                    QuantiteStock   = m.QuantiteStock,
                    QuantiteMin     = m.QuantiteMin,
                    Unite           = m.Unite,
                    ImageUrl        = m.ImageUrl,
                    DateAjout       = m.DateAjout,
                    CommandeId      = c.Id,
                    NumeroCommande  = c.NumeroCommande,
                    FournisseurId   = c.FournisseurId ?? 0,
                    NomFournisseur  = c.Fournisseur?.Nom ?? string.Empty,
                    QuantiteAchetee = c.QuantiteAchetee,
                    DateAchat       = c.DateAchat,
                    DateLivraison   = c.DateLivraison,
                    DateFinGarantie = c.DateFinGarantie,
                    NbArticles      = c.Articles.Count,
                    NbDisponibles   = c.Articles.Count(a => a.Statut == StatutArticle.Disponible)
                });
            }

            foreach (var m in materielsSeuls)
            {
                result.Add(new LigneCommandeMaterielDto
                {
                    MaterielId      = m.Id,
                    Reference       = m.Reference,
                    Designation     = m.Designation,
                    Description     = m.Description,
                    Categorie       = m.Categorie,
                    QuantiteStock   = m.QuantiteStock,
                    QuantiteMin     = m.QuantiteMin,
                    Unite           = m.Unite,
                    ImageUrl        = m.ImageUrl,
                    DateAjout       = m.DateAjout,
                    CommandeId      = 0,
                    NumeroCommande  = string.Empty,
                    NomFournisseur  = string.Empty,
                    QuantiteAchetee = 0,
                    DateAchat       = DateTime.MinValue,
                    NbArticles      = 0,
                    NbDisponibles   = 0
                });
            }

            return result.OrderBy(r => r.Designation).ThenByDescending(r => r.DateAchat);
        }
        public async Task<CommandeReponseDto> CreerAsync(CreerCommandeDto dto)
        {
            var materiel = await _db.Materiels.FindAsync(dto.MaterielId);
            if (materiel is null)
                return new CommandeReponseDto { Succes = false, Message = "Matériel introuvable." };

            var fournisseur = await _db.Fournisseurs.FindAsync(dto.FournisseurId);
            if (fournisseur is null)
                return new CommandeReponseDto { Succes = false, Message = "Fournisseur introuvable." };

            if (await _db.Commandes.AnyAsync(c => c.NumeroCommande == dto.NumeroCommande.Trim()))
                return new CommandeReponseDto { Succes = false, Message = "Ce numéro de commande existe déjà." };

            var numerosSerieFournis = dto.NumerosSerie
                .Where(ns => !string.IsNullOrWhiteSpace(ns))
                .Select(ns => ns!.Trim())
                .ToList();

            var doublonsInternes = numerosSerieFournis
                .GroupBy(ns => ns, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToList();

            if (doublonsInternes.Any())
                return new CommandeReponseDto { Succes = false, Message = $"Numéro(s) de série en double : {string.Join(", ", doublonsInternes)}." };

            if (numerosSerieFournis.Any())
            {
                var existants = await _db.ArticlesIndividuels
                    .Where(a => numerosSerieFournis.Contains(a.NumeroSerie!))
                    .Select(a => a.NumeroSerie!).ToListAsync();

                if (existants.Any())
                    return new CommandeReponseDto { Succes = false, Message = $"Numéro(s) de série déjà utilisé(s) : {string.Join(", ", existants)}." };
            }

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
                var inner = ex.InnerException?.Message ?? ex.Message;
                var msg = inner.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
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

            // Vérifier unicité du nouveau numéro
            var numTrimmed = dto.NumeroCommande.Trim();
            if (await _db.Commandes.AnyAsync(c => c.NumeroCommande == numTrimmed && c.Id != dto.Id))
                return new CommandeReponseDto { Succes = false, Message = "Ce numéro de commande est déjà utilisé." };

            // Résoudre le fournisseur
            int fournisseurId = dto.FournisseurId;
            if (fournisseurId == 0 && !string.IsNullOrWhiteSpace(dto.NomFournisseurLibre))
            {
                var existing = await _db.Fournisseurs
                    .FirstOrDefaultAsync(f => f.Nom.ToLower() == dto.NomFournisseurLibre.Trim().ToLower());
                if (existing is not null)
                {
                    fournisseurId = existing.IdFournisseur;
                }
                else
                {
                    var newF = new AssetFlow.Domain.Entities.Fournisseur { Nom = dto.NomFournisseurLibre.Trim() };
                    _db.Fournisseurs.Add(newF);
                    await _db.SaveChangesAsync();
                    fournisseurId = newF.IdFournisseur;
                }
            }

            commande.NumeroCommande  = numTrimmed;
            commande.FournisseurId   = fournisseurId > 0 ? fournisseurId : commande.FournisseurId;
            commande.DateAchat       = dto.DateAchat;
            commande.DateLivraison   = dto.DateLivraison;
            commande.DateFinGarantie = dto.DateFinGarantie;

            await _db.SaveChangesAsync();

            return new CommandeReponseDto
            {
                Succes     = true,
                Message    = $"Commande {commande.NumeroCommande} modifiée.",
                IdCommande = commande.Id
            };
        }
        public async Task<CommandeReponseDto> SupprimerAsync(int id)
        {
            var commande = await _db.Commandes
                .Include(c => c.Articles)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (commande is null)
                return new CommandeReponseDto { Succes = false, Message = "Commande introuvable." };

            // Supprimer incidents liés aux articles
            var articleIds = commande.Articles.Select(a => a.Id).ToList();
            if (articleIds.Any())
            {
                var incidents = await _db.Incidents
                    .Where(i => i.ArticleId.HasValue && articleIds.Contains(i.ArticleId.Value))
                    .ToListAsync();
                if (incidents.Any()) _db.Incidents.RemoveRange(incidents);

                // Détacher les articles des affectations
                var articlesAvecAffect = await _db.ArticlesIndividuels
                    .Where(a => articleIds.Contains(a.Id) && a.AffectationId.HasValue)
                    .ToListAsync();
                foreach (var art in articlesAvecAffect)
                    art.AffectationId = null;
            }

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