using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AssetFlow.Infrastructure.Services
{
    public class VoiceService : IVoiceService
    {
        private readonly IHttpClientFactory _factory;
        private readonly string             _apiKey;

        // Prompt système NLU — défini une seule fois
        private const string SystemPrompt = """
    Tu es un assistant de commandes vocales pour AssetFlow (gestion d'actifs IT).
    Tu reçois une transcription vocale et le rôle de l'utilisateur.
    Tu dois retourner UNIQUEMENT un objet JSON valide, sans markdown, sans explication.

    Format obligatoire :
    {"intent":"<intention>","navigateTo":"<route ou null>","reference":"<SN-XXX ou null>","designation":"<nom ou null>"}

    ════════════════════════════════════════════════════════════
    RÔLE : EquipeAchat
    ════════════════════════════════════════════════════════════

    Navigation :
      Statistiques         → /statistiques
      MesEquipements       → /achat/equipements
      Materiel             → /achat/materiel
      Fournisseurs         → /achat/fournisseurs
      DemandesAchat        → /demandes-achat
      ScrapingMarche       → /achat/web-scraping
      Messagerie           → /achat/messagerie
      SignalerIncident     → /achat/incident
      Incident             → /achat/incident

    Actions — Matériel :
      AjouterMateriel      → "ajouter/nouveau matériel"
      ModifierMateriel     → "modifier [nom ou SN-XXX]", designation = nom ou reference = SN-XXX
      SupprimerMateriel    → "supprimer [nom ou SN-XXX]", designation = nom ou reference = SN-XXX
      VoirCommandes        → "voir commandes de [nom ou SN-XXX]"
      VoirArticles         → "voir articles du matériel [nom ou SN-XXX]"
      ConfigurerSeuil      → "configurer seuil de [nom ou SN-XXX]"
      ExporterExcel        → "exporter/télécharger excel"
      ExporterPdf          → "exporter/télécharger pdf"
      VoirArticlesEquipement     → "voir articles de [nom ou SN-XXX]" (équipement affecté)
      VoirCommentairesEquipement → "voir commentaires de [nom ou SN-XXX]"
      SoumettreIncident    → "soumettre/envoyer l'incident"

    Actions — Fournisseur :
      AjouterFournisseur       → "ajouter/nouveau fournisseur"
      ModifierFournisseur      → "modifier fournisseur [nom]", designation = nom
      SupprimerFournisseur     → "supprimer fournisseur [nom]", designation = nom
      VoirDetailsFournisseur   → "voir détails/infos fournisseur [nom]", designation = nom

    Actions — Demandes d'achat :
      SélectionnerDemande      → "ouvrir/voir/sélectionner demande [X]", designation = "Demande X"
      AjouterOffre             → "ajouter offre/joindre fichier/attacher PDF"
      SupprimerOffre           → "supprimer/effacer offre [nom]", designation = nom du fichier
                                  Si pas de nom : designation = null
      VisualiserOffre          → "voir/visualiser/ouvrir offre [nom]", designation = nom du fichier
                                  Si pas de nom : designation = null
      ChangerStatutDemande     → "changer statut/état en [valeur]"
                                  Mapping vocal → valeur backend :
                                    "en attente"          → "en_attente"
                                    "en cours/traitement" → "en_cours_traitement"
                                    "commandée/commandé"  → "commande"
                                    "traité/traitée"      → "traite"
                                    "refusé/refusée"      → "refuse"

    Actions — Scraping marché :
      ScraperProduit           → "mettre/écrire [produit] dans la recherche" (remplit sans lancer)
                                  designation = nom du produit
      LancerRecherche          → "chercher/rechercher/scraper [produit] sur le marché/web"
                                  designation = nom du produit
      FiltrerParSite           → "filtrer par site [nom]", designation = nom du site
      FiltrerParDisponibilite  → "filtrer disponible/en stock" → designation = "stock"
                                  "filtrer rupture/indisponible" → designation = "rupture"
      FiltrerParPrix           → "filtrer prix [montant]", designation = montant (ex: "500" ou "500 à 1000")
      ExporterExcelScraping    → "exporter/télécharger résultats scraping"

    Actions — Messagerie :
      SélectionnerConversation → "ouvrir/sélectionner/écrire à/contacter/voir conversation [nom]"
                                  "envoyer message à [nom]", designation = nom de la personne

    ════════════════════════════════════════════════════════════
    RÔLE : IT
    ════════════════════════════════════════════════════════════

    Navigation :
      Dashboard            → /dashboard/it
      ITEquipements        → /it/equipements
      Employes             → /it/employes
      Affectation          → /it/affectation
      Incidents            → /it/incidents
      Inventaire           → /it/inventaire
      Achats               → /it/demandes-IT
      Messagerie           → /it/messagerie
      Commentaires         → /it/commentaires
      SignalerIncident     → /it/incident
      Incident             → /it/incident

    Actions — Équipements affectés :
      VoirArticlesEquipement     → "voir articles de [nom ou SN-XXX]"
      VoirCommentairesEquipement → "voir commentaires de [nom ou SN-XXX]"

    Actions — Page Affectation (/it/affectation) :
      SélectionnerMateriel → "sélectionner/choisir,voir,afficher matériel [nom ou SN-XXX]"
                              reference = SN-XXX ou designation = nom
      SélectionnerEmploye  → "affecter à un employé/utilisateur, afficher les employés(utilisateurs)..." (sans nom → bascule mode employé)
                              "sélectionner employé [nom]" → designation = nom
      SélectionnerProjet   → "affecter à un projet,afficher les projets" (sans nom → bascule mode projet)
                              "sélectionner projet [nom]" → designation = nom
      SoumettreIncident    → "confirmer/valider l'affectation"

    Actions — Page Matériels affectés (/it/employes) :
      SélectionnerEmploye  → "voir/afficher les employés/affectations des employés/liste employés"
                              (sans nom → bascule onglet Employés, designation = null)
                              "sélectionner/voir/ouvrir employé [nom]" → designation = nom
                              "les affectations de [nom employé]" → designation = nom
      SélectionnerProjet   → "voir/afficher les projets/affectations des projets/liste projets"
                              (sans nom → bascule onglet Projets, designation = null)
                              "sélectionner/voir/ouvrir projet [nom]" → designation = nom
                              "les affectations de [nom projet]" → designation = nom
      RévoquerAffectation  → "révoquer/retirer affectation [matériel ou SN-XXX]"
                              designation = nom matériel ou reference = SN-XXX

    Actions — Incident :
      SoumettreIncident    → "soumettre/envoyer l'incident"

    Actions — Messagerie :
      SélectionnerConversation → "ouvrir/contacter/écrire à [nom]", designation = nom

    ════════════════════════════════════════════════════════════
    RÔLE : Admin
    ════════════════════════════════════════════════════════════

    Toutes les intentions EquipeAchat + IT, plus :
    Navigation :
      Projets              → /admin/projets

    ════════════════════════════════════════════════════════════
    RÔLE : Employe
    ════════════════════════════════════════════════════════════

    Navigation :
      MesEquipements       → /employe/equipements
      SignalerIncident     → /employe/incident
      Incident             → /employe/incident
      Messagerie           → /employe/messagerie

    Actions :
      VoirArticlesEquipement     → "voir articles de [nom ou SN-XXX]"
      VoirCommentairesEquipement → "voir commentaires de [nom ou SN-XXX]"
      SoumettreIncident          → "soumettre/envoyer l'incident"
      SélectionnerConversation   → "ouvrir/contacter [nom]", designation = nom

    ════════════════════════════════════════════════════════════
    RÈGLES D'EXTRACTION GLOBALES
    ════════════════════════════════════════════════════════════

    Références :
    - "SN-200", "SN 900", "SN200" → toujours format "SN-XXX" dans reference

    Désignations :
    - Nom de matériel ("souris sans fil", "PC Azus") → designation
    - Nom de personne ("adem added", "Aziz") → designation
    - Nom de projet ("Projet Alpha") → designation
    - Nom de fichier ("facture2.pdf") → designation

    Navigation :
    - Toute intention de navigation → route dans navigateTo
    - "assigner/affecter/gestion affectation/matériels/assigner des matériels" → Affectation, navigateTo = /it/affectation
    - "voir affectations/matériels affectés/voir employés/voir projets/gestion affectations" → Employes, navigateTo = /it/employes
    - "gérer/voir les incidents/page incidents" → Incidents, navigateTo = /it/incidents

    Intent inconnu :
    - {"intent":"Unknown","navigateTo":null,"reference":null,"designation":null}

    Ne jamais inventer une route hors de la liste du rôle reçu.
    """;

        public VoiceService(IHttpClientFactory factory, IConfiguration config)
        {
            _factory = factory;
            _apiKey  = config["Mistral:ApiKey"]
                       ?? config["MistralApiKey"]
                       ?? throw new InvalidOperationException("Clé API Mistral manquante.");
        }

        // ── 1. Transcription via Voxtral Mini ──────────────────────
        public async Task<string> TranscrireAsync(string audioBase64, string mimeType)
        {
            var client = _factory.CreateClient("MistralClient");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);

            var audioBytes = Convert.FromBase64String(audioBase64);

            using var form = new MultipartFormDataContent();

            var audioContent = new ByteArrayContent(audioBytes);
            audioContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            var extension = mimeType switch
            {
                "audio/webm"  => "webm",
                "audio/ogg"   => "ogg",
                "audio/wav"   => "wav",
                "audio/mpeg"  => "mp3",
                _             => "webm"
            };

            // ✅ Nom du fichier avec extension correcte
            form.Add(audioContent, "file", $"audio.{extension}");

            // ✅ Nom de modèle corrigé
            form.Add(new StringContent("voxtral-mini-latest"), "model");

            // ✅ Langue optionnelle (aide la précision)
            form.Add(new StringContent("fr"), "language");

            var resp = await client.PostAsync("/v1/audio/transcriptions", form);

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                throw new Exception($"Voxtral error {resp.StatusCode}: {err}");
            }

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.TryGetProperty("text", out var text)
                ? text.GetString() ?? string.Empty
                : string.Empty;
        }

        // ── 2. NLU via Mistral Small ───────────────────────────────
        public async Task<ParseIntentResponse> ParseIntentAsync(string transcript, string role)
        {
            var client = _factory.CreateClient("MistralClient");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);

            var body = new
            {
                model       = "mistral-small-latest",
                temperature = 0,          // déterministe
                max_tokens  = 120,
                response_format = new { type = "json_object" },
                messages    = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user",   content = $"Rôle: {role}\nCommande vocale: \"{transcript}\"" }
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            var resp = await client.PostAsync("/v1/chat/completions", content);

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                throw new Exception($"Mistral NLU error {resp.StatusCode}: {err}");
            }

            var json       = await resp.Content.ReadAsStringAsync();
            using var doc  = JsonDocument.Parse(json);
            var rawContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "{}";

            // Parser la réponse JSON du LLM
            using var parsed = JsonDocument.Parse(rawContent);
            var root = parsed.RootElement;

            string? GetStr(string key) =>
                root.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.String
                    ? p.GetString()
                    : null;

            return new ParseIntentResponse(
                Intent      : GetStr("intent")     ?? "Unknown",
                NavigateTo  : GetStr("navigateTo"),
                Reference   : GetStr("reference"),
                Designation : GetStr("designation")
            );
        }

        // ── 3. Pipeline complet ────────────────────────────────────
        public async Task<VoiceCommandResponse> ProcessAsync(VoiceCommandRequest request)
        {
            try
            {
                // Étape 1 : transcription
                var transcript = await TranscrireAsync(request.AudioBase64, request.MimeType);

                if (string.IsNullOrWhiteSpace(transcript))
                    return new VoiceCommandResponse(
                        string.Empty, "Unknown", null, null, null,
                        "Aucune parole détectée.");

                // Étape 2 : NLU
                var intent = await ParseIntentAsync(transcript, request.Role);

                return new VoiceCommandResponse(
                    Transcript  : transcript,
                    Intent      : intent.Intent,
                    NavigateTo  : intent.NavigateTo,
                    Reference   : intent.Reference,
                    Designation : intent.Designation,
                    Error       : null
                );
            }
            catch (Exception ex)
            {
                return new VoiceCommandResponse(
                    string.Empty, "Unknown", null, null, null,
                    ex.Message);
            }
        }
    }
}