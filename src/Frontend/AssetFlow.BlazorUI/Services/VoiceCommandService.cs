using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Services
{
    public enum VoiceCommandType
    {
        Navigation,
        // ── Achat / Matériel ──
        AjouterMateriel,
        ModifierMateriel,
        SupprimerMateriel,
        VoirCommandes,
        VoirArticles,
        ConfigurerSeuil,
        ExporterExcel,
        ExporterPdf,
        // ── MesEquipements ──
        VoirArticlesEquipement,
        VoirCommentairesEquipement,
        // ── Générique ──
        Unknown
    }

    public class VoiceCommand
    {
        public VoiceCommandType Type        { get; set; }
        public string?          NavigateTo  { get; set; }
        public string?          Reference   { get; set; }
        public string?          Designation { get; set; }
        public string           RawText     { get; set; } = string.Empty;
    }

    public class VoiceCommandService
    {
        public event Func<VoiceCommand, Task>? OnCommand;
        public event Action<string>?           OnTranscript;
        public event Action<bool>?             OnListeningChanged;

        private bool   _isListening  = false;
        private string _currentRole  = string.Empty;

        public bool   IsListening  => _isListening;
        public string CurrentRole  => _currentRole;

        public void SetRole(string role)     => _currentRole = role.Trim().Trim('"', '\'');
        public void SetListening(bool v)     { _isListening = v; OnListeningChanged?.Invoke(v); }
        public void NotifyTranscript(string text) => OnTranscript?.Invoke(text);

        public async Task ProcessCommand(string text)
        {
            var cmd = Parse(text.ToLower().Trim(), _currentRole);
            cmd.RawText = text;
            if (OnCommand != null)
                await OnCommand.Invoke(cmd);
        }

        // ── Routes selon rôle ──────────────────────────────────────
        private static Dictionary<string, string> GetRoutes(string role)
        {
            var r = role.ToLower();

            if (r == "equipeachat" || r == "achat")
                return new()
                {
                    ["statistiques"]  = "/statistiques",
                    ["equipements"]   = "/achat/equipements",
                    ["materiel"]      = "/achat/materiel",
                    ["fournisseurs"]  = "/achat/fournisseurs",
                    ["demandes"]      = "/demandes-achat",
                    ["scraping"]      = "/achat/web-scraping",
                    ["messagerie"]    = "/achat/messagerie",
                };

            if (r == "it" || r == "equipe it")
                return new()
                {
                    ["dashboard"]     = "/dashboard/it",
                    ["equipements"]   = "/it/equipements",
                    ["employes"]      = "/it/employes",
                    ["affectation"]   = "/it/affectation",
                    ["incidents"]     = "/it/incidents",
                    ["inventaire"]    = "/it/inventaire",
                    ["achats"]        = "/it/demandes-IT",
                    ["messagerie"]    = "/it/messagerie",
                    ["commentaires"]  = "/it/commentaires",
                };

            if (r == "admin" || r == "administrateur")
                return new()
                {
                    ["statistiques"]  = "/statistiques",
                    ["equipements"]   = "/achat/equipements",
                    ["materiel"]      = "/achat/materiel",
                    ["fournisseurs"]  = "/achat/fournisseurs",
                    ["demandes"]      = "/demandes-achat",
                    ["scraping"]      = "/achat/web-scraping",
                    ["messagerie"]    = "/achat/messagerie",
                    ["dashboard"]     = "/dashboard/it",
                    ["incidents"]     = "/it/incidents",
                    ["inventaire"]    = "/it/inventaire",
                    ["projets"]       = "/admin/projets",
                };

            // Employé
            return new()
            {
                ["equipements"]   = "/employe/equipements",
                ["incident"]      = "/employe/incident",
                ["messagerie"]    = "/employe/messagerie",
            };
        }

        // ── Parser principal ───────────────────────────────────────
        private static VoiceCommand Parse(string t, string role)
        {
            var routes = GetRoutes(role);

            // ── Navigation ─────────────────────────────────────────
            if (t.Contains("statistique") || t.Contains("tableau de bord"))
                return Nav(routes, "statistiques");

            if ((t.Contains("mes équipement") || t.Contains("mes equipement") || t.Contains("équipement") || t.Contains("equipement"))
                && !t.Contains("article") && !t.Contains("comment") && !t.Contains("voir"))
                return Nav(routes, "equipements");

            if (t.Contains("matériel") && !HasAction(t))
                return Nav(routes, "materiel");

            if (t.Contains("fournisseur"))
                return Nav(routes, "fournisseurs");

            if (t.Contains("demande"))
                return Nav(routes, "demandes");

            if (t.Contains("scraping") || t.Contains("marché"))
                return Nav(routes, "scraping");

            if (t.Contains("messagerie"))
                return Nav(routes, "messagerie");

            if (t.Contains("dashboard") || t.Contains("tableau it"))
                return Nav(routes, "dashboard");

            if (t.Contains("incident"))
                return Nav(routes, "incidents");

            if (t.Contains("inventaire"))
                return Nav(routes, "inventaire");

            if (t.Contains("affectation") || t.Contains("assigner"))
                return Nav(routes, "affectation");

            if (t.Contains("commentaire") && !t.Contains("sn"))
                return Nav(routes, "commentaires");

            if (t.Contains("projet"))
                return Nav(routes, "projets");

            // ── Extraction référence (SN-XXX) ──────────────────────
            var refMatch = System.Text.RegularExpressions.Regex.Match(t, @"sn[- ]?(\d+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            string? reference = refMatch.Success ? $"SN-{refMatch.Groups[1].Value}" : null;

            // ── Actions Matériel (Achat) ────────────────────────────
            if (t.Contains("ajouter") && t.Contains("matériel"))
                return new() { Type = VoiceCommandType.AjouterMateriel };

            if ((t.Contains("modifier") || t.Contains("éditer")) && reference != null)
                return new() { Type = VoiceCommandType.ModifierMateriel, Reference = reference };

            if ((t.Contains("supprimer") || t.Contains("effacer")) && reference != null)
                return new() { Type = VoiceCommandType.SupprimerMateriel, Reference = reference };

            if (t.Contains("commande") && reference != null)
                return new() { Type = VoiceCommandType.VoirCommandes, Reference = reference };

            if (t.Contains("article") && reference != null && !t.Contains("équipement"))
                return new() { Type = VoiceCommandType.VoirArticles, Reference = reference };

            if (t.Contains("seuil") && reference != null)
                return new() { Type = VoiceCommandType.ConfigurerSeuil, Reference = reference };

            if (t.Contains("excel"))
                return new() { Type = VoiceCommandType.ExporterExcel };

            if (t.Contains("pdf"))
                return new() { Type = VoiceCommandType.ExporterPdf };

            // ── Actions MesEquipements ──────────────────────────────
            // "voir les articles de la souris sans fil" ou "voir les articles SN-900"
            if (t.Contains("article") && (t.Contains("équipement") || reference != null || t.Length > 20))
            {
                var nom = ExtractNom(t);
                return new() { Type = VoiceCommandType.VoirArticlesEquipement, Reference = reference, Designation = nom };
            }

            // "commenter la souris sans fil" / "commentaire SN-900"
            if (t.Contains("comment"))
            {
                var nom = ExtractNom(t);
                return new() { Type = VoiceCommandType.VoirCommentairesEquipement, Reference = reference, Designation = nom };
            }

            return new() { Type = VoiceCommandType.Unknown };
        }

        private static bool HasAction(string t)
            => t.Contains("ajouter") || t.Contains("modifier") || t.Contains("supprimer")
            || t.Contains("voir") || t.Contains("article") || t.Contains("commande");

        private static VoiceCommand Nav(Dictionary<string, string> routes, string key)
        {
            routes.TryGetValue(key, out var path);
            return new() { Type = VoiceCommandType.Navigation, NavigateTo = path };
        }

        // Extrait le nom du matériel après des mots-clés ("de la", "du", "le", "la", "les")
        private static string? ExtractNom(string t)
        {
            var patterns = new[] { "de la ", "du ", "le ", "la ", "les ", "pour ", "sur " };
            foreach (var p in patterns)
            {
                var idx = t.IndexOf(p, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    var nom = t[(idx + p.Length)..].Trim();
                    if (nom.Length > 2) return nom;
                }
            }
            return null;
        }
    }
}