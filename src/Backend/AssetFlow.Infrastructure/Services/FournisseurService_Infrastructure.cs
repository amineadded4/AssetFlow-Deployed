using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class FournisseurService : IFournisseurService
    {
        private readonly AppDbContext _context;

        public FournisseurService(AppDbContext context)
        {
            _context = context;
        }

        // GET ALL — Retourne tous les fournisseurs
        public async Task<List<Fournisseur>> GetAllAsync()
        {
            var fournisseurs = await _context.Fournisseurs
                .OrderBy(f => f.Nom)
                .AsNoTracking()
                .ToListAsync();

            // Calculer le nombre réel de commandes par fournisseur
            var comptes = await _context.Commandes
                .Where(c => c.FournisseurId != null)
                .GroupBy(c => c.FournisseurId)
                .Select(g => new { FournisseurId = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var f in fournisseurs)
            {
                var compte = comptes.FirstOrDefault(c => c.FournisseurId == f.IdFournisseur);
                f.CommandesTotales = compte?.Count ?? 0;
            }

            return fournisseurs;
        }
        // GET BY ID — Retourne un fournisseur par son IdFournisseur
        public async Task<Fournisseur?> GetByIdAsync(int id)
        {
            return await _context.Fournisseurs
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.IdFournisseur == id);
        }
        // RECHERCHER — Filtre sur Nom, Telephone, Adresse, Mail
        public async Task<List<Fournisseur>> RechercherAsync(string terme)
        {
            var t = terme.Trim().ToLower();

            return await _context.Fournisseurs
                .Where(f =>
                    f.Nom.ToLower().Contains(t)                          ||
                    (f.Telephone != null && f.Telephone.Contains(t))     ||
                    (f.Adresse   != null && f.Adresse.ToLower().Contains(t)) ||
                    (f.Mail      != null && f.Mail.ToLower().Contains(t)))
                .OrderBy(f => f.Nom)
                .AsNoTracking()
                .ToListAsync();
        }

        // AJOUTER — Insère un nouveau fournisseur
        public async Task<Fournisseur> AjouterAsync(Fournisseur fournisseur)
        {
            _context.Fournisseurs.Add(fournisseur);
            await _context.SaveChangesAsync();
            return fournisseur;   // IdFournisseur est maintenant peuplé
        }

        // MODIFIER — Met à jour un fournisseur existant
        public async Task ModifierAsync(Fournisseur fournisseur)
        {
            // Récupérer l'enregistrement avec tracking (pour détecter les changements)
            var existant = await _context.Fournisseurs
                .FirstOrDefaultAsync(f => f.IdFournisseur == fournisseur.IdFournisseur);

            if (existant == null)
                throw new KeyNotFoundException(
                    $"Fournisseur ID {fournisseur.IdFournisseur} introuvable.");

            // Mettre à jour uniquement les champs modifiables
existant.Nom = fournisseur.Nom;
existant.Telephone = fournisseur.Telephone;
existant.Adresse = fournisseur.Adresse;
existant.Mail = fournisseur.Mail;

existant.TauxLivraisonATemps = fournisseur.TauxLivraisonATemps;
existant.ScoreFiabilite = fournisseur.ScoreFiabilite;
existant.DerniereCommande = fournisseur.DerniereCommande;

            await _context.SaveChangesAsync();
        }

        // SUPPRIMER — Supprime un fournisseur par son ID
        public async Task SupprimerAsync(int id)
        {
            var fournisseur = await _context.Fournisseurs
                .FirstOrDefaultAsync(f => f.IdFournisseur == id);

            if (fournisseur == null)
                throw new KeyNotFoundException($"Fournisseur ID {id} introuvable.");

            _context.Fournisseurs.Remove(fournisseur);
            await _context.SaveChangesAsync();
        }
    }
}
