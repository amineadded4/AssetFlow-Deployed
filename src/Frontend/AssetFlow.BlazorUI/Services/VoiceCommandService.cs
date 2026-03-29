// Services/VoiceCommandService.cs
using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Services
{
    public enum VoiceCommandType
    {
        Navigation,
        AjouterMateriel,
        ModifierMateriel,
        SupprimerMateriel,
        VoirCommandes,
        VoirArticles,
        ConfigurerSeuil,
        ExporterExcel,
        ExporterPdf,
        Unknown
    }

    public class VoiceCommand
    {
        public VoiceCommandType Type { get; set; }
        public string? NavigateTo   { get; set; }
        public string? Reference    { get; set; } // ex: "SN-200"
        public string  RawText      { get; set; } = string.Empty;
    }

    public class VoiceCommandService
    {
        public event Func<VoiceCommand, Task>? OnCommand;
        public event Action<string>?           OnTranscript;
        public event Action<bool>?             OnListeningChanged;

        private bool _isListening = false;
        public bool IsListening => _isListening;

        public void SetListening(bool v)
        {
            _isListening = v;
            OnListeningChanged?.Invoke(v);
        }

        public void NotifyTranscript(string text)
            => OnTranscript?.Invoke(text);

        public async Task ProcessCommand(string text)
        {
            var cmd = Parse(text.ToLower().Trim());
            cmd.RawText = text;
            if (OnCommand != null)
                await OnCommand.Invoke(cmd);
        }

        private static VoiceCommand Parse(string t)
        {
            // ── Navigation ──────────────────────────────────────────
            if (t.Contains("statistique") || t.Contains("tableau de bord") || t.Contains("dashboard"))
                return new() { Type = VoiceCommandType.Navigation, NavigateTo = "/statistiques" };

            if (t.Contains("matériel") && !t.Contains("ajouter") && !t.Contains("modifier")
                && !t.Contains("supprimer") && !t.Contains("voir"))
                return new() { Type = VoiceCommandType.Navigation, NavigateTo = "/achat/materiel" };

            if (t.Contains("fournisseur"))
                return new() { Type = VoiceCommandType.Navigation, NavigateTo = "/achat/fournisseurs" };

            if (t.Contains("demande"))
                return new() { Type = VoiceCommandType.Navigation, NavigateTo = "/demandes-achat" };

            if (t.Contains("scraping") || t.Contains("marché"))
                return new() { Type = VoiceCommandType.Navigation, NavigateTo = "/achat/web-scraping" };

            if (t.Contains("messagerie"))
                return new() { Type = VoiceCommandType.Navigation, NavigateTo = "/achat/messagerie" };

            if (t.Contains("équipement") || t.Contains("equipement"))
                return new() { Type = VoiceCommandType.Navigation, NavigateTo = "/achat/equipements" };

            // ── Actions Matériel ────────────────────────────────────
            var refMatch = System.Text.RegularExpressions.Regex.Match(t, @"sn[- ]?(\d+)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            string? reference = refMatch.Success ? $"SN-{refMatch.Groups[1].Value}" : null;

            if (t.Contains("ajouter") && t.Contains("matériel"))
                return new() { Type = VoiceCommandType.AjouterMateriel };

            if ((t.Contains("modifier") || t.Contains("éditer") || t.Contains("editer")) && reference != null)
                return new() { Type = VoiceCommandType.ModifierMateriel, Reference = reference };

            if ((t.Contains("supprimer") || t.Contains("effacer")) && reference != null)
                return new() { Type = VoiceCommandType.SupprimerMateriel, Reference = reference };

            if ((t.Contains("commande") || t.Contains("voir les commandes")) && reference != null)
                return new() { Type = VoiceCommandType.VoirCommandes, Reference = reference };

            if ((t.Contains("article") || t.Contains("voir les articles")) && reference != null)
                return new() { Type = VoiceCommandType.VoirArticles, Reference = reference };

            if ((t.Contains("seuil") || t.Contains("configurer")) && reference != null)
                return new() { Type = VoiceCommandType.ConfigurerSeuil, Reference = reference };

            if (t.Contains("excel") || t.Contains("exporter excel"))
                return new() { Type = VoiceCommandType.ExporterExcel };

            if (t.Contains("pdf") || t.Contains("exporter pdf"))
                return new() { Type = VoiceCommandType.ExporterPdf };

            return new() { Type = VoiceCommandType.Unknown };
        }
    }
}