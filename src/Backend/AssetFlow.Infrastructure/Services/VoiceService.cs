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
        Tu es un assistant de commandes vocales pour une application de gestion d'actifs IT appelée AssetFlow.
        Tu reçois une transcription vocale et le rôle de l'utilisateur.
        Tu dois retourner UNIQUEMENT un objet JSON valide, sans markdown, sans explication.

        Format de réponse obligatoire :
        {"intent":"<intention>","navigateTo":"<route ou null>","reference":"<SN-XXX ou null>","designation":"<nom ou null>"}

        Intentions disponibles selon le rôle :

        EquipeAchat → Navigation :
        Statistiques,Dashboard        → /statistiques
        MesEquipements      → /achat/equipements
        Materiel            → /achat/materiel
        Fournisseurs        → /achat/fournisseurs
        DemandesAchat       → /demandes-achat
        ScrapingMarche      → /achat/web-scraping
        Messagerie          → /achat/messagerie
        SignalerIncident    → /achat/incident
        Incident            → /achat/incident
        EquipeAchat → Actions :
        AjouterMateriel, ModifierMateriel, SupprimerMateriel,
        VoirCommandes, VoirArticles, ConfigurerSeuil,
        ExporterExcel, ExporterPdf,
        VoirArticlesEquipement, VoirCommentairesEquipement,SoumettreIncident
            AjouterFournisseur,
            ModifierFournisseur,
            SupprimerFournisseur,
            VoirDetailsFournisseur,

            SélectionnerDemande,   → désignation = nom de la demande ("Demande 1", "Demande 3"...)
            ScraperProduit,        → désignation = nom du produit à scraper ("MacBook", "souris"...)
            AjouterOffre,          → navigateTo null, ouvre l'explorateur de fichiers
            ChangerStatutDemande,  → désignation = nouveau statut ("en attente", "en cours", "commandée", "traitée", "archivée"),
            SupprimerOffre,    → désignation = nom du fichier ("facture2", "facture 2.pdf"...)
            VisualiserOffre,   → désignation = nom du fichier

            FiltrerParSite,          → désignation = nom du site ("MyTek", "Spacenet", "Tunisianet")
            FiltrerParDisponibilite, → désignation = "stock" ou "rupture"
            FiltrerParPrix,          → désignation = montant ex: "500" ou "500 à 1000"
            LancerRecherche,         → désignation = nom du produit à rechercher

            SélectionnerConversation, → désignation = nom complet ou partiel de la personne

        IT → Navigation :
        Dashboard,Statistiques           → /dashboard/it
        ITEquipements       → /it/equipements
        Employes            → /it/employes
        Affectation         → /it/affectation
        Incidents,gérer les incident(s),voir les incident(s),page incident(s)          → /it/incidents
        Inventaire          → /it/inventaire
        Achats              → /it/demandes-IT
        Messagerie          → /it/messagerie
        Commentaires        → /it/commentaires
        SignalerIncident    → /it/incident
        Incident            → /it/incident

        IT → Actions :
        SélectionnerEmploye,    → désignation = nom de l'employé ("adem added", "Aziz"...)
        SélectionnerProjet,    → désignation = nom du projet ("Projet 1", "Projet 2"...)
        RévoquerAffectation,    → désignation = nom du matériel ou référence ("Souris sans fil", "SN-900")

        Admin → Toutes les intentions EquipeAchat + IT + :
        Projets             → /admin/projets

        Employe → Navigation :
        MesEquipements      → /employe/equipements
        Incident            → /employe/incident
        SignalerIncident    → /employe/incident
        Messagerie          → /employe/messagerie
            Employe → Actions :
            VoirArticlesEquipement, VoirCommentairesEquipement

        Règles d'extraction :
        - Si la phrase contient une référence type "SN-200", "SN 900", extrais-la en format "SN-XXX"
        - Si la phrase contient un nom de matériel ("souris sans fil", "PC Azus"), mets-le dans "designation"
        - Pour les actions de navigation, mets la route dans "navigateTo"
        - Si l'intention est inconnue : {"intent":"Unknown","navigateTo":null,"reference":null,"designation":null}

        - "sélectionner/ouvrir/voir demande [X]" → SélectionnerDemande, désignation = "Demande X"
        - "scraper/chercher/rechercher [produit]" → ScraperProduit, désignation = nom du produit
        - "ajouter offre/joindre fichier/attacher PDF" → AjouterOffre
        - "changer statut/état en [valeur]" → ChangerStatutDemande, désignation = valeur normalisée
        Valeurs acceptées : "EnAttente", "EnCours", "Commandee", "Traitee", "Archivee"
        Mapping vocal → valeur :
            "en attente"            → "EnAttente"
            "en cours/traitement"   → "EnCours"
            "commandée/commandé"    → "Commandee"
            "traité/traitée"        → "Traitee"
            "archivé/archivée"      → "Archivee"
        - "supprimer/effacer offre [nom]" → SupprimerOffre, désignation = nom du fichier
        - "voir/visualiser/ouvrir offre [nom]" → VisualiserOffre, désignation = nom du fichier
        - Si pas de nom précisé : désignation = null (prendra la première offre)

        - "chercher/rechercher/scraper [produit] sur le marché/web" → LancerRecherche, désignation = produit
        - "mettre/écrire [produit] dans la recherche" → ScraperProduit, désignation = produit (remplit sans lancer)
        - "filtrer par site [nom]" → FiltrerParSite, désignation = nom du site
        - "filtrer disponible/en stock" → FiltrerParDisponibilite, désignation = "stock"
        - "filtrer rupture/indisponible" → FiltrerParDisponibilite, désignation = "rupture"
        - "filtrer prix [montant]" → FiltrerParPrix, désignation = montant brut
        
        - "ouvrir/sélectionner/écrire à/contacter/voir conversation [nom]" → SélectionnerConversation, désignation = nom complet de la personne
        - "envoyer message à [nom]" → SélectionnerConversation, désignation = nom complet de la personne

        - "assigner/affecter/gestion affectation" → Affectation, navigateTo = /it/affectation
        - "voir affectations/employes/projets/voir employés/voir projets/gestion affectations des employés/gestion affectations des projets/affectations employés/affectations projets/matériels affectés" → Employes, navigateTo = /it/employes

        - "sélectionner/voir/ouvrir employé [nom]" → SélectionnerEmploye, désignation = nom
        - "révoquer/retirer affectation [matériel]" → RévoquerAffectation, désignation = nom matériel ou référence
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