using AssetFlow.Application.DTOs;
using AssetFlow.Application.DTOs.AgentDtos;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class AgentService : IAgentService
    {
        private readonly IOrchestratorAgentService _orchestrator;
        private readonly IWebSearchAgentService    _webSearch;
        private readonly IDatabaseAgentService     _dbAgent;
        private readonly ICommandeService          _commandeService;
        private readonly IMaterielService          _materielService;
        private readonly IDemandeAchatService      _demandeService; // ← NOUVEAU
        private readonly AppDbContext              _db;

        public AgentService(
            IOrchestratorAgentService orchestrator,
            IWebSearchAgentService    webSearch,
            IDatabaseAgentService     dbAgent,
            ICommandeService          commandeService,
            IMaterielService          materielService,
            IDemandeAchatService      demandeService, // ← NOUVEAU
            AppDbContext              db)
        {
            _orchestrator    = orchestrator;
            _webSearch       = webSearch;
            _dbAgent         = dbAgent;
            _commandeService = commandeService;
            _materielService = materielService;
            _demandeService  = demandeService;
            _db              = db;
        }

        // ── Traitement principal d'un message ─────────────────────────────
        public async Task<AgentChatResponse> ProcessMessageAsync(AgentChatRequest request)
        {
            var history   = request.History;
            var agentType = await _orchestrator.DetermineAgentAsync(request.Message, history);
            var response  = new AgentChatResponse { AgentUsed = agentType };

            if (agentType == "web")
            {
                response.Message   = await _webSearch.SearchAsync(request.Message, history);
                response.AgentUsed = "web";
            }
            else if (agentType == "db")
            {
                var msg = request.Message.ToLower();
                var isCreationAttempt = (msg.Contains("ajoute") || msg.Contains("crée") || msg.Contains("créer") || msg.Contains("nouveau") || msg.Contains("nouvelle") || msg.Contains("add") || msg.Contains("insert"))
                    && (msg.Contains("matériel") || msg.Contains("materiel") || msg.Contains("équipement") || msg.Contains("equipement"));

                if (isCreationAttempt)
                {
                    response.Message   = "❌ Je ne suis pas autorisé à créer de nouveaux matériels. Seules les commandes sur des matériels existants sont permises. Si vous souhaitez commander un matériel existant, précisez sa référence.";
                    response.AgentUsed = "db";
                    return response;
                }

                response.Message   = await _dbAgent.QueryAsync(request.Message, history);
                response.AgentUsed = "db";
            }
            else if (agentType.StartsWith("action_"))
            {
                var action = await _orchestrator.ExtractActionAsync(request.Message, history);
                response.AgentUsed = "action";
                response.Action    = action;

                if (action?.Type == "add_materiel" && action.MaterielProposal != null)
                {
                    var ref_ = action.MaterielProposal.Reference?.Trim();
                    if (!string.IsNullOrWhiteSpace(ref_))
                    {
                        var existant = await _db.Materiels
                            .FirstOrDefaultAsync(m => m.Reference.ToLower() == ref_.ToLower());

                        if (existant != null)
                        {
                            action.MaterielProposal.Reference     = existant.Reference;
                            action.MaterielProposal.Designation   = existant.Designation;
                            action.MaterielProposal.Description   = existant.Description;
                            action.MaterielProposal.Categorie     = existant.Categorie;
                            action.MaterielProposal.QuantiteStock = existant.QuantiteStock;
                            action.MaterielProposal.QuantiteMin   = existant.QuantiteMin;
                            action.MaterielProposal.Unite         = existant.Unite;
                            action.MaterielProposal.Emplacement   = existant.Emplacement;
                            action.Label = $"exists:{existant.Id}";
                        }
                        else
                        {
                            response.AgentUsed = "db";
                            response.Action    = null;
                            response.Message   = "❌ La création de nouveaux matériels n'est pas autorisée. Seuls les matériels existants peuvent recevoir une commande. Vérifiez la référence ou contactez un administrateur.";
                            return response;
                        }
                    }
                    else
                    {
                        response.AgentUsed = "db";
                        response.Action    = null;
                        response.Message   = "❌ Impossible de créer un matériel sans référence. Précisez la référence du matériel existant.";
                        return response;
                    }
                }

                response.Message = action?.Type switch
                {
                    "add_materiel"  => action.Label.StartsWith("exists:")
                        ? $"Le matériel **{action.MaterielProposal?.Designation}** existe déjà. Voici le formulaire pour ajouter une commande à ce matériel."
                        : $"J'ai préparé une proposition pour créer le matériel **{action.MaterielProposal?.Designation}**. Veuillez vérifier et approuver les informations ci-dessous.",
                    "add_commande"  => $"J'ai préparé une proposition de commande **{action.CommandeProposal?.NumeroCommande}**. Veuillez vérifier et approuver les informations.",
                    "add_article"   => $"J'ai préparé une proposition pour ajouter un article. Veuillez vérifier les informations.",
                    _               => "Voici la proposition générée. Veuillez l'approuver ou la modifier."
                };
            }
            else
            {
                response.Message   = await _dbAgent.QueryAsync(request.Message, history);
                response.AgentUsed = "db";
            }

            return response;
        }

        // ════════════════════════════════════════════════════════════════════
        //  ── NOUVEAU : Workflow Demande d'achat ─────────────────────────────
        // ════════════════════════════════════════════════════════════════════

        // Statuts considérés comme "terminés" et qu'on n'affiche pas dans le dropdown
        private static readonly HashSet<string> StatutsTermines = new(StringComparer.OrdinalIgnoreCase)
        {
            "traite", "traitee", "traité", "traitée",
            "commande", "commandée", "commandé",
            "refuse",  "refusée", "refusé"
        };

        public async Task<List<DemandePendingDto>> GetPendingDemandesAsync()
        {
            var demandes = await _demandeService.GetAllAsync();

            return demandes
                .Where(d => !StatutsTermines.Contains(d.Statut?.Trim() ?? ""))
                .Select(d => new DemandePendingDto
                {
                    IdDemande    = d.IdDemande,
                    Reference    = d.Reference,
                    NomProduit   = d.NomProduit,
                    Quantite     = d.Lignes.Any() ? d.Lignes.Sum(l => l.Quantite) : d.Quantite,
                    Description  = d.Description,
                    Statut       = d.Statut,
                    DateCreation = d.DateCreation,
                    DemandeurNom = d.DemandeurNom,
                    Lignes       = d.Lignes.Select(l => new LigneDemandeMini
                    {
                        Reference   = l.Reference,
                        NomProduit  = l.NomProduit,
                        Quantite    = l.Quantite,
                        Description = l.Description
                    }).ToList()
                })
                .ToList();
        }

        // ── Étape 1 : Recherche web → 5 offres ─────────────────────────────
        public async Task<AgentChatResponse> StartDemandeWorkflowAsync(int idDemande)
        {
            var demande = await _demandeService.GetByIdAsync(idDemande);
            if (demande == null)
            {
                return new AgentChatResponse
                {
                    AgentUsed = "db",
                    Message   = $"❌ Demande d'achat #{idDemande} introuvable."
                };
            }

            // Si la demande contient des lignes multiples, on prend la première comme produit principal.
            // Pour une recherche plus complète, on pourra étendre à toutes les lignes.
            var nomProduit = demande.Lignes.Any()
                ? demande.Lignes.First().NomProduit
                : demande.NomProduit;

            var quantite = demande.Lignes.Any()
                ? demande.Lignes.Sum(l => l.Quantite)
                : demande.Quantite;

            var description = demande.Description
                ?? (demande.Lignes.FirstOrDefault()?.Description);

            // Étape 1 : appel WebSearch agent qui retourne 5 offres structurées
            var offres = await _webSearch.SearchOffersAsync(nomProduit, quantite, description);

            var introMsg = $"📋 **Demande d'achat {demande.Reference}** — {nomProduit} (×{quantite})\n\n" +
                           $"🔎 **Étape 1 — Recherche Web** : j'ai trouvé **{offres.Count} offre(s)** correspondant à votre besoin. " +
                           "Cliquez sur une carte pour la sélectionner. L'**Agent Base de données** prendra ensuite le relais pour préparer la commande.";

            return new AgentChatResponse
            {
                AgentUsed     = "web",
                Message       = introMsg,
                OffresWeb     = offres,
                IdDemande     = demande.IdDemande,
                ReferenceDemande = demande.Reference,
                Etape         = 1
            };
        }

        // ── Étape 2 : Offre choisie → pré-remplit le formulaire matériel + commande ──
        public async Task<AgentChatResponse> SelectOfferAsync(int idDemande, OffreSearchResultDto offre)
        {
            var demande = await _demandeService.GetByIdAsync(idDemande);
            if (demande == null)
            {
                return new AgentChatResponse
                {
                    AgentUsed = "db",
                    Message   = $"❌ Demande d'achat #{idDemande} introuvable."
                };
            }

            // On essaie d'identifier le matériel concerné depuis la première ligne (référence)
            var ligne   = demande.Lignes.FirstOrDefault();
            var refMat  = ligne?.Reference?.Trim();
            var qte     = demande.Lignes.Any()
                ? demande.Lignes.Sum(l => l.Quantite)
                : demande.Quantite;

            Materiel? materiel = null;
            if (!string.IsNullOrWhiteSpace(refMat))
            {
                materiel = await _db.Materiels
                    .FirstOrDefaultAsync(m => m.Reference.ToLower() == refMat.ToLower());
            }

            // Si la référence ne correspond à aucun matériel : refuser la création
            if (materiel == null)
            {
                return new AgentChatResponse
                {
                    AgentUsed = "db",
                    Message   = $"❌ La référence **{refMat ?? "(vide)"}** de la demande {demande.Reference} ne correspond à aucun matériel existant. " +
                                "Seules les commandes sur des matériels existants sont autorisées."
                };
            }

            // Construire la proposition matériel (read-only) + commande (éditable)
            var numCommande = $"CMD-{DateTime.UtcNow:yyyy}-{demande.Reference.Replace("SN-", "")}-{DateTime.UtcNow:HHmmss}";

            var proposal = new AgentMaterielProposal
            {
                Reference     = materiel.Reference,
                Designation   = materiel.Designation,
                Description   = materiel.Description,
                Categorie     = materiel.Categorie,
                QuantiteStock = materiel.QuantiteStock,
                QuantiteMin   = materiel.QuantiteMin,
                Unite         = materiel.Unite,
                Emplacement   = materiel.Emplacement,
                Commande = new AgentCommandeProposal
                {
                    NumeroCommande  = numCommande,
                    MaterielId      = materiel.Id,
                    NomMateriel     = materiel.Designation,
                    NomFournisseur  = offre.Fournisseur,
                    QuantiteAchetee = qte,
                    DateAchat       = DateTime.UtcNow,
                    DateLivraison   = TryParseDelai(offre.DelaiLivraison),
                    DateFinGarantie = TryParseGarantie(offre.Garantie)
                }
            };

            var action = new AgentAction
            {
                Type             = "add_materiel",
                Label            = $"exists:{materiel.Id}",
                MaterielProposal = proposal
            };

            var msg = $"🤖 **Étape 2 — Agent Base de données**\n\n" +
                      $"Offre sélectionnée : **{offre.Fournisseur}** — {offre.PrixTotal ?? offre.PrixUnitaire ?? "prix N/A"}.\n\n" +
                      $"J'ai pré-rempli la commande pour le matériel existant **{materiel.Designation}** ({materiel.Reference}). " +
                      "Vérifiez les informations puis approuvez ou refusez.";

            return new AgentChatResponse
            {
                AgentUsed        = "action",
                Message          = msg,
                Action           = action,
                IdDemande        = demande.IdDemande,
                ReferenceDemande = demande.Reference,
                Etape            = 2
            };
        }

        // ── Helpers parsing dates depuis offre ─────────────────────────────
        private static DateTime? TryParseDelai(string? delai)
        {
            if (string.IsNullOrWhiteSpace(delai)) return null;
            // Cherche un nombre suivi de "jour", "j", "semaine", "sem"
            var match = System.Text.RegularExpressions.Regex.Match(
                delai.ToLower(),
                @"(\d+)\s*(jour|j\b|semaine|sem|month|mois)");
            if (!match.Success) return null;
            if (!int.TryParse(match.Groups[1].Value, out var n)) return null;
            var unit = match.Groups[2].Value;
            return unit.StartsWith("sem")  ? DateTime.UtcNow.AddDays(n * 7)
                 : unit.StartsWith("mois") || unit.StartsWith("month") ? DateTime.UtcNow.AddMonths(n)
                 : DateTime.UtcNow.AddDays(n);
        }

        private static DateTime? TryParseGarantie(string? garantie)
        {
            if (string.IsNullOrWhiteSpace(garantie)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(
                garantie.ToLower(),
                @"(\d+)\s*(an|année|year|mois|month)");
            if (!match.Success) return null;
            if (!int.TryParse(match.Groups[1].Value, out var n)) return null;
            return match.Groups[2].Value.StartsWith("mois") || match.Groups[2].Value.StartsWith("month")
                ? DateTime.UtcNow.AddMonths(n)
                : DateTime.UtcNow.AddYears(n);
        }

        // ── Alertes initiales à l'ouverture du chat ───────────────────────
        public async Task<AgentChatResponse> GetInitialAlertsAsync()
        {
            var alertes = await _dbAgent.GetStockAlertsAsync();
            var response = new AgentChatResponse
            {
                AgentUsed = "db",
                Alertes   = alertes
            };

            if (alertes.Count == 0)
            {
                response.Message = "✅ Tous les niveaux de stock sont OK. Aucune alerte détectée.";
                return response;
            }

            var tasks = alertes.Select(async a =>
            {
                a.Proposition = await _orchestrator.GenerateMaterielProposalAsync(a);
            });
            await Task.WhenAll(tasks);

            response.Alertes = alertes;
            response.Message = $"⚠️ **{alertes.Count} alerte(s) de stock détectée(s)** :\n\n" +
                string.Join("\n", alertes.Select(a =>
                    $"• **{a.Designation}** ({a.Reference}) : stock actuel **{a.QuantiteStock}** / minimum **{a.QuantiteMin}**"));

            return response;
        }

        // ── Approbation d'une action ──────────────────────────────────────
        public async Task<AgentApprovalResponse> ApproveActionAsync(AgentApprovalRequest request)
        {
            if (!request.Approved)
                return new AgentApprovalResponse { Succes = false, Message = "Action annulée." };

            try
            {
                switch (request.ActionType)
                {
                    case "add_materiel":
                    {
                        if (request.MaterielProposal == null)
                            return Fail("Données matériel manquantes.");

                        var p = request.MaterielProposal;

                        var existant = await _db.Materiels
                            .FirstOrDefaultAsync(m => m.Reference.ToLower() == p.Reference.Trim().ToLower());

                        if (existant == null)
                            return Fail("❌ Création de nouveaux matériels non autorisée. Le matériel doit déjà exister en base.");

                        int materielId = existant.Id;

                        if (p.Commande != null && !string.IsNullOrWhiteSpace(p.Commande.NumeroCommande))
                        {
                            var doublonCmd = await _db.Commandes
                                .FirstOrDefaultAsync(c => c.NumeroCommande.ToLower() == p.Commande.NumeroCommande.Trim().ToLower());

                            if (doublonCmd != null)
                                return new AgentApprovalResponse
                                {
                                    Succes  = false,
                                    Message = $"duplicate_commande:{p.Commande.NumeroCommande}"
                                };

                            var fournisseurId = p.Commande.FournisseurId;
                            if (fournisseurId == 0 && !string.IsNullOrWhiteSpace(p.Commande.NomFournisseur))
                            {
                                var f = await _db.Fournisseurs.FirstOrDefaultAsync(x =>
                                    x.Nom.ToLower() == p.Commande.NomFournisseur.Trim().ToLower());
                                if (f == null)
                                {
                                    var newF = new Domain.Entities.Fournisseur { Nom = p.Commande.NomFournisseur.Trim() };
                                    _db.Fournisseurs.Add(newF);
                                    await _db.SaveChangesAsync();
                                    fournisseurId = newF.IdFournisseur;
                                }
                                else fournisseurId = f.IdFournisseur;
                            }

                            await _commandeService.CreerAsync(new CreerCommandeDto
                            {
                                Utilisateur         = request.Utilisateur,
                                NumeroCommande      = p.Commande.NumeroCommande,
                                MaterielId          = materielId,
                                FournisseurId       = fournisseurId,
                                NomFournisseurLibre = p.Commande.NomFournisseur,
                                QuantiteAchetee     = p.Commande.QuantiteAchetee,
                                DateAchat           = p.Commande.DateAchat,
                                DateLivraison       = p.Commande.DateLivraison,
                                DateFinGarantie     = p.Commande.DateFinGarantie,
                                NumerosSerie        = p.Commande.NumerosSerie
                            });

                            // ── NOUVEAU : si la commande provient d'une demande d'achat, marquer comme "commande" ──
                            if (request.IdDemandeOrigine.HasValue && request.IdDemandeOrigine.Value > 0)
                            {
                                try
                                {
                                    await _demandeService.ChangerStatutAsync(
                                        request.IdDemandeOrigine.Value,
                                        "commande",
                                        request.Utilisateur);
                                }
                                catch { /* on ne bloque pas le succès commande pour cela */ }
                            }
                        }

                        return new AgentApprovalResponse
                        {
                            Succes  = true,
                            Message = $"✅ Commande ajoutée au matériel existant **{existant.Designation}** !",
                            Id      = materielId
                        };
                    }

                    case "add_commande":
                    {
                        if (request.CommandeProposal == null)
                            return Fail("Données commande manquantes.");

                        var p = request.CommandeProposal;

                        var doublon = await _db.Commandes
                            .FirstOrDefaultAsync(c => c.NumeroCommande.ToLower() == p.NumeroCommande.Trim().ToLower());

                        if (doublon != null)
                            return new AgentApprovalResponse
                            {
                                Succes  = false,
                                Message = $"duplicate_commande:{p.NumeroCommande}"
                            };

                        var fournisseurId = p.FournisseurId;
                        if (fournisseurId == 0 && !string.IsNullOrWhiteSpace(p.NomFournisseur))
                        {
                            var f = await _db.Fournisseurs.FirstOrDefaultAsync(x =>
                                x.Nom.ToLower() == p.NomFournisseur.Trim().ToLower());
                            if (f == null)
                            {
                                var newF = new Domain.Entities.Fournisseur { Nom = p.NomFournisseur.Trim() };
                                _db.Fournisseurs.Add(newF);
                                await _db.SaveChangesAsync();
                                fournisseurId = newF.IdFournisseur;
                            }
                            else fournisseurId = f.IdFournisseur;
                        }

                        var result = await _commandeService.CreerAsync(new CreerCommandeDto
                        {
                            Utilisateur     = request.Utilisateur,
                            NumeroCommande  = p.NumeroCommande,
                            MaterielId      = p.MaterielId,
                            FournisseurId   = fournisseurId,
                            QuantiteAchetee = p.QuantiteAchetee,
                            DateAchat       = p.DateAchat,
                            DateLivraison   = p.DateLivraison,
                            DateFinGarantie = p.DateFinGarantie,
                            NumerosSerie    = p.NumerosSerie
                        });

                        if (!result.Succes) return Fail(result.Message);
                        return new AgentApprovalResponse
                        {
                            Succes  = true,
                            Message = $"✅ Commande **{p.NumeroCommande}** créée avec succès !",
                            Id      = result.IdCommande
                        };
                    }

                    case "add_article":
                    {
                        if (request.ArticleProposal == null)
                            return Fail("Données article manquantes.");

                        var p = request.ArticleProposal;
                        var commande = await _db.Commandes
                            .Include(c => c.Materiel)
                            .FirstOrDefaultAsync(c => c.Id == p.CommandeId);

                        if (commande == null)
                            return Fail("Commande introuvable.");

                        var article = new Domain.Entities.ArticleIndividuel
                        {
                            NumeroSerie = string.IsNullOrWhiteSpace(p.NumeroSerie) ? null : p.NumeroSerie.Trim(),
                            Statut      = Domain.Entities.StatutArticle.Disponible,
                            MaterielId  = p.MaterielId > 0 ? p.MaterielId : commande.MaterielId,
                            CommandeId  = p.CommandeId
                        };
                        _db.ArticlesIndividuels.Add(article);

                        if (commande.Materiel != null) commande.Materiel.QuantiteStock++;

                        _db.ArticleHistoriques.Add(new Domain.Entities.ArticleHistorique
                        {
                            ArticleId     = article.Id,
                            TypeEvenement = Domain.Entities.TypeEvenementArticle.Acquisition,
                            DateEvenement = DateTime.UtcNow,
                            Description   = $"Ajout via agent IA — commande {commande.NumeroCommande}"
                        });

                        await _db.SaveChangesAsync();
                        return new AgentApprovalResponse
                        {
                            Succes  = true,
                            Message = $"✅ Article ajouté avec succès à la commande {commande.NumeroCommande} !",
                            Id      = article.Id
                        };
                    }

                    default:
                        return Fail($"Type d'action inconnu : {request.ActionType}");
                }
            }
            catch (Exception ex)
            {
                return Fail($"Erreur : {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private static AgentApprovalResponse Fail(string msg)
            => new() { Succes = false, Message = msg };
    }
}
