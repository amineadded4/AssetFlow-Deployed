using AssetFlow.BlazorUI.DTOs;
namespace AssetFlow.BlazorUI.Services
{
    public enum VoiceCommandType
    {
        // Navigation
        Navigation,
        MesEquipements,
        Statistiques,
        Materiel,
        Fournisseurs,
        DemandesAchat,
        ScrapingMarche,
        Messagerie,
        Dashboard,
        ITEquipements,
        Employes,
        Affectation,
        Incidents,
        Inventaire,
        Achats,
        Commentaires,
        Projets,
        Incident,
        SignalerIncident,

        // Actions achat
        AjouterMateriel,
        ModifierMateriel,
        SupprimerMateriel,
        VoirCommandes,
        VoirArticles,
        ConfigurerSeuil,
        ExporterExcel,
        ExporterPdf,
        VoirArticlesEquipement,
        VoirCommentairesEquipement,
        SoumettreIncident,

        Unknown
    }

    public class VoiceCommand
    {
        public VoiceCommandType Type        { get; set; }
        public string?          NavigateTo  { get; set; }
        public string?          Reference   { get; set; }
        public string?          Designation { get; set; }
        public string           RawText     { get; set; } = string.Empty;
        public string           Transcript  { get; set; } = string.Empty;
    }

    public class VoiceCommandService
    {
        public event Func<VoiceCommand, Task>? OnCommand;
        public event Action<string>?           OnTranscript;
        public event Action<bool>?             OnListeningChanged;

        private bool   _isListening = false;
        private string _currentRole = string.Empty;

        public bool   IsListening => _isListening;
        public string CurrentRole => _currentRole;

        public void SetRole(string role)
        {
            _currentRole = role.Trim().Trim('"', '\'');
        }

        public void SetListening(bool v)
        {
            _isListening = v;
            OnListeningChanged?.Invoke(v);
        }

        public void NotifyTranscript(string text)
            => OnTranscript?.Invoke(text);

        /// <summary>
        /// Appelé par VoiceButton après réception de la réponse backend
        /// </summary>
        public async Task DispatchResponse(VoiceCommandResponse response)
        {
            if (!string.IsNullOrEmpty(response.Transcript))
                OnTranscript?.Invoke(response.Transcript);

            var cmd = MapToCommand(response);
            if (OnCommand != null)
                await OnCommand.Invoke(cmd);
        }

        // Intents qui sont des navigations (ont un navigateTo)
        private static readonly HashSet<VoiceCommandType> NavigationIntents = new()
        {
            VoiceCommandType.MesEquipements,
            VoiceCommandType.Statistiques,
            VoiceCommandType.Materiel,
            VoiceCommandType.Fournisseurs,
            VoiceCommandType.DemandesAchat,
            VoiceCommandType.ScrapingMarche,
            VoiceCommandType.Messagerie,
            VoiceCommandType.Dashboard,
            VoiceCommandType.ITEquipements,
            VoiceCommandType.Employes,
            VoiceCommandType.Affectation,
            VoiceCommandType.Incidents,
            VoiceCommandType.Inventaire,
            VoiceCommandType.Achats,
            VoiceCommandType.Commentaires,
            VoiceCommandType.Projets,
            VoiceCommandType.Incident,
            VoiceCommandType.SignalerIncident, 
        };

        private static VoiceCommand MapToCommand(VoiceCommandResponse r)
        {
            var parsed = Enum.TryParse<VoiceCommandType>(r.Intent, true, out var t);
            
            VoiceCommandType type;
            if (!parsed)
            {
                // Intent inconnu mais navigateTo présent → traiter comme Navigation
                type = !string.IsNullOrEmpty(r.NavigateTo)
                    ? VoiceCommandType.Navigation
                    : VoiceCommandType.Unknown;
            }
            else if (NavigationIntents.Contains(t))
            {
                // Intent connu de type navigation → normaliser en Navigation
                type = VoiceCommandType.Navigation;
            }
            else
            {
                type = t;
            }

            return new VoiceCommand
            {
                Type        = type,
                NavigateTo  = r.NavigateTo,
                Reference   = r.Reference,
                Designation = r.Designation,
                RawText     = r.Transcript,
                Transcript  = r.Transcript
            };
        }
    }
}