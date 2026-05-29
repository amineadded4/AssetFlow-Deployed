using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    // Construit les graphes contextuels de la mémoire intelligente.
    public class GraphService : IGraphService
    {
        private readonly AppDbContext _db;

        public GraphService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<GraphStatsDto> GetStatsAsync()
        {
            return new GraphStatsDto
            {
                TotalMateriel   = await _db.Materiels.CountAsync(),
                TotalIncidents  = await _db.Incidents.CountAsync(i => i.Statut != StatutIncident.Resolu),
                TotalUsers      = await _db.Users.CountAsync(),
                ActiveAnomalies = await _db.Incidents.CountAsync(i => i.Urgence > 50 && i.Statut != StatutIncident.Resolu)
            };
        }

        // Listes pour le panneau gauche
        public async Task<List<GraphEntitySummaryDto>> GetMaterielsAsync()
        {
            var materiels = await _db.Materiels
                .AsNoTracking()
                .OrderBy(m => m.Reference)
                .Take(50)
                .ToListAsync();

            var result = new List<GraphEntitySummaryDto>();
            foreach (var m in materiels)
            {
                var incCount = await _db.Incidents
                    .CountAsync(i => i.Affectation!.MaterielId == m.Id && i.Statut != StatutIncident.Resolu);

                result.Add(new GraphEntitySummaryDto
                {
                    Id     = $"m-{m.Id}",
                    Label  = m.Reference,
                    Detail = $"{m.Designation} · Stock: {m.QuantiteStock}",
                    Type   = "materiel",
                    Status = incCount > 2 ? "critical" : incCount > 0 ? "warning" : "normal",
                    Count  = incCount
                });
            }
            return result;
        }

        public async Task<List<GraphEntitySummaryDto>> GetUtilisateursAsync()
        {
            var users = await _db.Users
                .AsNoTracking()
                .OrderBy(u => u.FirstName)
                .Take(50)
                .ToListAsync();

            var result = new List<GraphEntitySummaryDto>();
            foreach (var u in users)
            {
                var incCount = await _db.Incidents
                    .CountAsync(i => i.Affectation!.UtilisateurId == u.Id && i.Statut != StatutIncident.Resolu);

                result.Add(new GraphEntitySummaryDto
                {
                    Id     = $"u-{u.Id}",
                    Label  = $"{u.FirstName} {u.LastName[..1]}.",
                    Detail = $"{u.Role}",
                    Type   = "utilisateur",
                    Status = "normal",
                    Count  = incCount
                });
            }
            return result;
        }

        public async Task<List<GraphEntitySummaryDto>> GetDemandesAsync()
        {
            var demandes = await _db.DemandeAchat
                .AsNoTracking()
                .Include(d => d.Offres)
                .OrderByDescending(d => d.DateCreation)
                .Take(50)
                .ToListAsync();

            return demandes.Select(d => new GraphEntitySummaryDto
            {
                Id     = $"d-{d.IdDemande}",
                Label  = d.Reference,
                Detail = $"{d.NomProduit} · {d.Statut}",
                Type   = "demande",
                Status = "normal",
                Count  = d.Offres.Count
            }).ToList();
        }

        public async Task<List<GraphEntitySummaryDto>> GetProjetsAsync()
        {
            var projets = await _db.Projects
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .ToListAsync();

            var result = new List<GraphEntitySummaryDto>();
            foreach (var p in projets)
            {
                var matCount = await _db.Affectations
                    .CountAsync(a => a.ProjetId == p.Id && a.Etat == EtatAffectation.Courante);

                result.Add(new GraphEntitySummaryDto
                {
                    Id     = $"p-{p.Id}",
                    Label  = p.Nom,
                    Detail = p.Description ?? p.Statut.ToString(),
                    Type   = "projet",
                    Status = "normal",
                    Count  = matCount
                });
            }
            return result;
        }

        // Graphes contextuels

        // Graphe d'un matériel : incidents, utilisateurs affectés, projets, commandes
        public async Task<GraphResponseDto> GetGraphForMaterielAsync(int materielId)
        {
            var materiel = await _db.Materiels.FindAsync(materielId);
            if (materiel == null) return EmptyGraph();

            var nodes = new List<GraphNodeDto>();
            var links = new List<GraphLinkDto>();

            // Nœud central
            nodes.Add(new GraphNodeDto
            {
                Id       = $"m-{materiel.Id}",
                Type     = "materiel",
                Label    = materiel.Reference,
                Detail   = $"{materiel.Designation} · Stock: {materiel.QuantiteStock} {materiel.Unite}",
                Status   = "normal",
                Weight   = 5,
                IsCenter = true
            });

            // Incidents actifs
            var incidents = await _db.Incidents
                .AsNoTracking()
                .Include(i => i.Affectation)
                .Where(i => i.Affectation!.MaterielId == materielId && i.Statut != StatutIncident.Resolu)
                .OrderByDescending(i => i.DateIncident)
                .Take(10)
                .ToListAsync();

            foreach (var inc in incidents)
            {
                var iId = $"i-{inc.Id}";
                nodes.Add(new GraphNodeDto
                {
                    Id     = iId,
                    Type   = "incident",
                    Label  = inc.TypeIncident,
                    Detail = $"Urgence {inc.Urgence}/100 · {inc.Statut}",
                    Status = inc.Urgence > 50 ? "critical" : "warning",
                    Weight = inc.Urgence
                });
                links.Add(new GraphLinkDto { Source = $"m-{materiel.Id}", Target = iId, Label = "incident signalé", Strength = 0.8 });
            }

            // Utilisateurs affectés (affectations courantes)
            var affectations = await _db.Affectations
                .AsNoTracking()
                .Include(a => a.Utilisateur)
                .Where(a => a.MaterielId == materielId && a.Etat == EtatAffectation.Courante && a.UtilisateurId.HasValue)
                .OrderByDescending(a => a.DateAffectation)
                .Take(8)
                .ToListAsync();

            var addedUsers = new HashSet<int>();
            foreach (var aff in affectations)
            {
                if (aff.Utilisateur == null || addedUsers.Contains(aff.Utilisateur.Id)) continue;
                addedUsers.Add(aff.Utilisateur.Id);
                var uId = $"u-{aff.Utilisateur.Id}";
                nodes.Add(new GraphNodeDto
                {
                    Id     = uId,
                    Type   = "utilisateur",
                    Label  = $"{aff.Utilisateur.FirstName} {aff.Utilisateur.LastName[..1]}.",
                    Detail = $"{aff.Utilisateur.Department} · {aff.Utilisateur.Role}",
                    Status = "normal",
                    Weight = 2
                });
                links.Add(new GraphLinkDto { Source = $"m-{materiel.Id}", Target = uId, Label = "affecté à", Strength = 0.6 });
            }

            // Projets liés
            var projets = await _db.Affectations
                .AsNoTracking()
                .Include(a => a.Projet)
                .Where(a => a.MaterielId == materielId && a.ProjetId.HasValue && a.Projet != null)
                .OrderByDescending(a => a.DateAffectation)
                .Select(a => a.Projet!)
                .Distinct()
                .Take(5)
                .ToListAsync();

            foreach (var p in projets)
            {
                var pId = $"p-{p.Id}";
                if (nodes.Any(n => n.Id == pId)) continue;
                nodes.Add(new GraphNodeDto { Id = pId, Type = "projet", Label = p.Nom, Detail = p.Description ?? p.Statut.ToString(), Status = "normal", Weight = 2 });
                links.Add(new GraphLinkDto { Source = $"m-{materiel.Id}", Target = pId, Label = "utilisé dans", Strength = 0.4 });
            }

            // Commandes
            var commandes = await _db.Commandes
                .AsNoTracking()
                .Where(c => c.MaterielId == materielId)
                .OrderByDescending(c => c.DateAchat)
                .Take(5)
                .ToListAsync();

            foreach (var c in commandes)
            {
                var cId = $"cmd-{c.Id}";
                nodes.Add(new GraphNodeDto { Id = cId, Type = "commande", Label = c.NumeroCommande, Detail = $"Qté: {c.QuantiteAchetee} · {c.DateAchat:dd/MM/yyyy}", Status = "normal", Weight = 1 });
                links.Add(new GraphLinkDto { Source = $"m-{materiel.Id}", Target = cId, Label = "commandé via", Strength = 0.4 });
            }

            // Mettre à jour le statut du nœud central
            var incCountTotal = incidents.Count;
            nodes[0].Status = incCountTotal > 2 ? "critical" : incCountTotal > 0 ? "warning" : "normal";

            return new GraphResponseDto { Nodes = nodes, Links = links, Insights = new(), Stats = new() };
        }

        // Graphe d'un utilisateur : matériels affectés, incidents par matériel, commentaires
        public async Task<GraphResponseDto> GetGraphForUtilisateurAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return EmptyGraph();

            var nodes = new List<GraphNodeDto>();
            var links = new List<GraphLinkDto>();

            nodes.Add(new GraphNodeDto
            {
                Id       = $"u-{user.Id}",
                Type     = "utilisateur",
                Label    = $"{user.FirstName} {user.LastName[..1]}.",
                Detail   = $"{user.Department} · {user.Role}",
                Status   = "normal",
                Weight   = 5,
                IsCenter = true
            });

            // Matériels affectés
            var affectations = await _db.Affectations
                .AsNoTracking()
                .Include(a => a.Materiel)
                .Where(a => a.UtilisateurId == userId && a.Etat == EtatAffectation.Courante)
                .OrderByDescending(a => a.DateAffectation)
                .Take(10)
                .ToListAsync();

            var addedMats = new HashSet<int>();
            foreach (var aff in affectations)
            {
                if (addedMats.Contains(aff.MaterielId)) continue;
                addedMats.Add(aff.MaterielId);

                var mId = $"m-{aff.MaterielId}";
                nodes.Add(new GraphNodeDto { Id = mId, Type = "materiel", Label = aff.Materiel.Reference, Detail = $"{aff.Materiel.Designation} · Stock: {aff.Materiel.QuantiteStock}", Status = "normal", Weight = 3 });
                links.Add(new GraphLinkDto { Source = $"u-{user.Id}", Target = mId, Label = "matériel affecté", Strength = 0.7 });

                // Incidents sur ce matériel liés à cet utilisateur
                var incidents = await _db.Incidents
                    .AsNoTracking()
                    .Where(i => i.Affectation!.MaterielId == aff.MaterielId && i.Affectation.UtilisateurId == userId && i.Statut != StatutIncident.Resolu)
                    .OrderByDescending(i => i.DateIncident)
                    .Take(10)
                    .ToListAsync();

                foreach (var inc in incidents)
                {
                    var iId = $"i-{inc.Id}";
                    if (nodes.Any(n => n.Id == iId)) continue;
                    nodes.Add(new GraphNodeDto { Id = iId, Type = "incident", Label = inc.TypeIncident, Detail = $"Urgence {inc.Urgence}/100 · {inc.Statut}", Status = inc.Urgence > 50 ? "critical" : "warning", Weight = inc.Urgence });
                    links.Add(new GraphLinkDto { Source = mId, Target = iId, Label = "incident", Strength = 0.8 });
                }

                // Commentaires de cet utilisateur sur ce matériel
                var comments = await _db.CommentairesMateriel
                .AsNoTracking()
                .Where(c => c.MaterielId == aff.MaterielId && c.UtilisateurId == userId)
                .OrderByDescending(c => c.DateCreation)
                .Take(5)
                .ToListAsync();

            foreach (var cmt in comments)
            {
                var cmtId = $"cmt-{cmt.Id}"; // ← ID unique par commentaire
                var preview = cmt.Contenu.Length > 40 ? cmt.Contenu[..40] + "…" : cmt.Contenu;
                nodes.Add(new GraphNodeDto
                {
                    Id     = cmtId,
                    Type   = "commentaire",
                    Label  = "Commentaire",
                    Detail = preview,
                    Status = "normal",
                    Weight = 1
                });
                links.Add(new GraphLinkDto { Source = mId, Target = cmtId, Label = "commentaire", Strength = 0.4 });
            }
            }

            return new GraphResponseDto { Nodes = nodes, Links = links, Insights = new(), Stats = new() };
        }

        // Graphe d'une demande d'achat : créateur, offres
        public async Task<GraphResponseDto> GetGraphForDemandeAsync(int demandeId)
        {
            var demande = await _db.DemandeAchat
                .AsNoTracking()
                .Include(d => d.User)
                .Include(d => d.Offres)
                .FirstOrDefaultAsync(d => d.IdDemande == demandeId);

            if (demande == null) return EmptyGraph();

            var nodes = new List<GraphNodeDto>();
            var links = new List<GraphLinkDto>();

            nodes.Add(new GraphNodeDto
            {
                Id       = $"d-{demande.IdDemande}",
                Type     = "demande",
                Label    = demande.Reference,
                Detail   = $"{demande.NomProduit} · Qté: {demande.Quantite}",
                Status   = "normal",
                Weight   = 5,
                IsCenter = true
            });

            // Créateur
            if (demande.User != null)
            {
                var uId = $"u-{demande.User.Id}";
                nodes.Add(new GraphNodeDto { Id = uId, Type = "utilisateur", Label = $"{demande.User.FirstName} {demande.User.LastName[..1]}.", Detail = $"{demande.User.Department} · Demandeur", Status = "normal", Weight = 3 });
                links.Add(new GraphLinkDto { Source = $"d-{demande.IdDemande}", Target = uId, Label = "créée par", Strength = 0.7 });
            }
            else if (!string.IsNullOrWhiteSpace(demande.DemandeurNom))
            {
                var uId = "u-ext";
                nodes.Add(new GraphNodeDto { Id = uId, Type = "utilisateur", Label = demande.DemandeurNom, Detail = "Demandeur", Status = "normal", Weight = 3 });
                links.Add(new GraphLinkDto { Source = $"d-{demande.IdDemande}", Target = uId, Label = "créée par", Strength = 0.7 });
            }

            // Offres
            foreach (var offre in demande.Offres.Take(6))
            {
                var oId = $"off-{offre.IdOffre}";
                nodes.Add(new GraphNodeDto
                {
                    Id     = oId,
                    Type   = "commande",
                    Label  = offre.NomFichier.Length > 20 ? offre.NomFichier[..20] + "…" : offre.NomFichier,
                    Detail = $"{(offre.EstChoisie ? "✓ Sélectionnée" : "En attente")}{(offre.PrixTotal != null ? " · " + offre.PrixTotal : "")}",
                    Status = offre.EstChoisie ? "normal" : "warning",
                    Weight = offre.EstChoisie ? 3 : 1
                });
                links.Add(new GraphLinkDto { Source = $"d-{demande.IdDemande}", Target = oId, Label = "offre", Strength = 0.6 });
            }

            return new GraphResponseDto { Nodes = nodes, Links = links, Insights = new(), Stats = new() };
        }

        // Graphe d'un projet : matériels affectés
        public async Task<GraphResponseDto> GetGraphForProjetAsync(int projetId)
        {
            var projet = await _db.Projects.FindAsync(projetId);
            if (projet == null) return EmptyGraph();

            var nodes = new List<GraphNodeDto>();
            var links = new List<GraphLinkDto>();

            nodes.Add(new GraphNodeDto
            {
                Id       = $"p-{projet.Id}",
                Type     = "projet",
                Label    = projet.Nom,
                Detail   = $"{projet.Statut} · {projet.Priorite}",
                Status   = "normal",
                Weight   = 5,
                IsCenter = true
            });

            // Matériels affectés à ce projet
            var affectations = await _db.Affectations
                .AsNoTracking()
                .Include(a => a.Materiel)
                .Include(a => a.Utilisateur)
                .Where(a => a.ProjetId == projetId && a.Etat == EtatAffectation.Courante)
                .OrderByDescending(a => a.DateAffectation)
                .Take(15)
                .ToListAsync();

            var addedMats = new HashSet<int>();
            foreach (var aff in affectations)
            {
                if (addedMats.Contains(aff.MaterielId)) continue;
                addedMats.Add(aff.MaterielId);

                var mId = $"m-{aff.MaterielId}";
                nodes.Add(new GraphNodeDto { Id = mId, Type = "materiel", Label = aff.Materiel.Reference, Detail = $"{aff.Materiel.Designation} · Qté: {aff.QuantiteAffectee}", Status = "normal", Weight = 2 });
                links.Add(new GraphLinkDto { Source = $"p-{projet.Id}", Target = mId, Label = "matériel affecté", Strength = 0.6 });

                // Utilisateur affecté
                if (aff.Utilisateur != null)
                {
                    var uId = $"u-{aff.Utilisateur.Id}";
                    if (!nodes.Any(n => n.Id == uId))
                    {
                        nodes.Add(new GraphNodeDto { Id = uId, Type = "utilisateur", Label = $"{aff.Utilisateur.FirstName} {aff.Utilisateur.LastName[..1]}.", Detail = aff.Utilisateur.Department, Status = "normal", Weight = 2 });
                    }
                    links.Add(new GraphLinkDto { Source = mId, Target = uId, Label = "affecté à", Strength = 0.5 });
                }
            }

            return new GraphResponseDto { Nodes = nodes, Links = links, Insights = new(), Stats = new() };
        }

        // Legacy GetGraphAsync (kept for compatibility)
        public async Task<GraphResponseDto> GetGraphAsync()
        {
            var stats = await GetStatsAsync();
            return new GraphResponseDto { Nodes = new(), Links = new(), Insights = new(), Stats = stats };
        }

        public async Task<GraphInsightDto?> GetInsightForNodeAsync(string nodeId)
        {
            return null;
        }

        private static GraphResponseDto EmptyGraph() => new() { Nodes = new(), Links = new(), Insights = new(), Stats = new() };

        private static int ComputeHealthScore(int totalMateriel, int activeIncidents)
        {
            if (totalMateriel == 0) return 100;
            return Math.Max(0, (int)((1 - (double)activeIncidents / totalMateriel) * 100));
        }
    }
}