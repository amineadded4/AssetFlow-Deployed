// src/Frontend/AssetFlow.BlazorUI/Pages/AgentChat.razor.cs
using AssetFlow.BlazorUI.DTOs;
using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AssetFlow.BlazorUI.Pages.Achat
{
    public partial class AgentChat : ComponentBase
    {
        [Inject] private AgentChatService  AgentSvc     { get; set; } = default!;
        [Inject] private IJSRuntime        JS           { get; set; } = default!;
        [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

        // ── ChatMessage ─────────────────────────────────────────────
        private class ChatMessage
        {
            public bool          IsUser          { get; set; }
            public string        Content         { get; set; } = string.Empty;
            public string?       AgentBadge      { get; set; }
            public AgentAction?  Action          { get; set; }
            public bool          ActionProcessed { get; set; }
            public DateTime      Timestamp       { get; set; } = DateTime.Now;

            // Infos du matériel pour le formulaire add_article
            public AgentMaterielInfo? MaterielInfo { get; set; }

            // Erreurs de validation inline
            public List<string>            ValidationErrors { get; set; } = new();
            public Dictionary<string,string> FieldErrors    { get; set; } = new();
        }

        // Infos matériel pour affichage lecture seule dans add_article
        private class AgentMaterielInfo
        {
            public string  Reference     { get; set; } = string.Empty;
            public string  Designation   { get; set; } = string.Empty;
            public string? Description   { get; set; }
            public string  Categorie     { get; set; } = string.Empty;
            public int     QuantiteStock { get; set; }
            public string  Unite         { get; set; } = "pièce";
        }

        // ── State ──────────────────────────────────────────────────
        private List<ChatMessage>       _messages        = new();
        private List<AlerteStock>       _alertes         = new();
        private List<AgentChatHistory>  _history         = new();

        private string  _inputText    = string.Empty;
        private bool    _isLoading    = false;
        private bool    _isApproving  = false;
        private bool    _showAlertes  = true;
        private bool    _sidebarOpen  = false;
        private bool    _chatStarted  = false; // true dès que l'user envoie un message
        private string  _initiales    = "U";
        private string? _username     = "Utilisateur";
        private string? _role         = "EquipeAchat";

        private readonly List<string> _suggestions = new()
        {
            "📦 Liste mes matériels en alerte",
            "📊 Donne-moi les statistiques de stock",
            "🔍 Recherche des fournisseurs de PC",
            "➕ Ajoute un nouveau matériel laptop",
            "📋 Montre mes dernières commandes",
            "⚠️ Quels incidents sont en attente ?"
        };

        // ── Init ───────────────────────────────────────────────────
        protected override async Task OnInitializedAsync()
        {
            try
            {
                var nom  = await JS.InvokeAsync<string?>("eval", "localStorage.getItem('user_name')");
                var role = await JS.InvokeAsync<string?>("eval", "localStorage.getItem('user_role')");
                if (!string.IsNullOrWhiteSpace(nom))  _username = Clean(nom);
                if (!string.IsNullOrWhiteSpace(role)) _role     = Clean(role);

                var parts = (_username ?? "U").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                _initiales = parts.Length >= 2
                    ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                    : (_username ?? "U")[..Math.Min(2, (_username ?? "U").Length)].ToUpper();
            }
            catch { }

            await LoadInitialAlerts();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await ScrollToBottom();
        }

        private async Task LoadInitialAlerts()
        {
            var resp = await AgentSvc.GetInitialAlertsAsync();
            if (resp == null) return;

            _alertes     = resp.Alertes;
            _showAlertes = _alertes.Count > 0;

            // Message d'alerte affiché dans le chat UNIQUEMENT si le chat a déjà démarré
            // Au premier chargement on ne l'ajoute pas (les alertes sont dans la barre sticky)
            if (_chatStarted && !string.IsNullOrEmpty(resp.Message))
            {
                _messages.Add(new ChatMessage
                {
                    IsUser     = false,
                    Content    = resp.Message,
                    AgentBadge = "db",
                    Timestamp  = DateTime.Now
                });
            }
        }

        // ── Envoi message ──────────────────────────────────────────
        private async Task SendMessage()
        {
            var text = _inputText.Trim();
            if (string.IsNullOrEmpty(text) || _isLoading) return;

            // Marquer le chat comme démarré → les suggestions disparaissent
            _chatStarted = true;

            _messages.Add(new ChatMessage { IsUser = true, Content = text });
            _history.Add(new AgentChatHistory { Role = "user", Content = text });
            _inputText = string.Empty;
            _isLoading = true;
            StateHasChanged();
            await ScrollToBottom();

            var request = new AgentChatRequest
            {
                Message = text,
                History = _history.TakeLast(10).ToList()
            };

            var resp = await AgentSvc.ChatAsync(request);
            _isLoading = false;

            if (resp != null)
            {
                var botMsg = new ChatMessage
                {
                    IsUser     = false,
                    Content    = resp.Message,
                    AgentBadge = resp.AgentUsed,
                    Action     = resp.Action,
                    Timestamp  = DateTime.Now
                };

                // Pour add_article : essayer de récupérer les infos du matériel
                if (resp.Action?.Type == "add_article" && resp.Action.ArticleProposal != null)
                {
                    botMsg.MaterielInfo = BuildMaterielInfoFromContext(resp.Action.ArticleProposal.NomMateriel);
                }

                // Pour add_materiel avec matériel existant : initialiser les numéros de série
                if (resp.Action?.Type == "add_materiel" && resp.Action.MaterielProposal?.Commande != null)
                {
                    AjusterArticlesCommande(resp.Action.MaterielProposal.Commande);
                }

                _messages.Add(botMsg);
                _history.Add(new AgentChatHistory { Role = "assistant", Content = resp.Message });

                if (_history.Count > 20) _history = _history.TakeLast(20).ToList();
            }
            else
            {
                _messages.Add(new ChatMessage
                {
                    IsUser    = false,
                    Content   = "❌ Une erreur s'est produite. Veuillez réessayer.",
                    Timestamp = DateTime.Now
                });
            }

            StateHasChanged();
            await ScrollToBottom();
        }

        private async Task SendSuggestion(string text)
        {
            _inputText = text;
            await SendMessage();
        }

        // ── Approbation d'action ───────────────────────────────────
        private async Task ApproveAction(ChatMessage msg, bool approved)
        {
            if (msg.Action == null) return;

            // Validation avant envoi
            if (approved)
            {
                msg.ValidationErrors.Clear();
                msg.FieldErrors.Clear();

                if (!ValidateAction(msg))
                {
                    StateHasChanged();
                    return;
                }
            }

            _isApproving = true;
            StateHasChanged();

            var request = new AgentApprovalRequest
            {
                ActionType       = msg.Action.Type,
                Approved         = approved,
                Utilisateur      = _username ?? "Agent IA",
                MaterielProposal = msg.Action.MaterielProposal,
                CommandeProposal = msg.Action.CommandeProposal,
                ArticleProposal  = msg.Action.ArticleProposal
            };

            var resp = await AgentSvc.ApproveAsync(request, _username ?? "Utilisateur");
            _isApproving     = false;
            msg.ActionProcessed = true;

            var resultMsg = resp?.Message ?? (approved ? "✅ Action effectuée." : "❌ Action annulée.");
            _messages.Add(new ChatMessage
            {
                IsUser     = false,
                Content    = resultMsg,
                AgentBadge = resp?.Succes == true ? "action_success" : "action_error",
                Timestamp  = DateTime.Now
            });

            // Si Commander depuis alerte et succès → retirer l'alerte de la barre
            if (resp?.Succes == true && msg.Action.Type == "add_materiel")
            {
                // Retirer l'alerte correspondante
                var refConcernee = msg.Action.MaterielProposal?.Reference;
                if (!string.IsNullOrEmpty(refConcernee))
                {
                    _alertes.RemoveAll(a =>
                        a.Reference.Equals(refConcernee, StringComparison.OrdinalIgnoreCase));
                    if (_alertes.Count == 0) _showAlertes = false;
                }
                // Recharger les alertes complètes depuis l'API
                await LoadInitialAlerts();
            }

            StateHasChanged();
            await ScrollToBottom();
        }

        // ── Validation inline ──────────────────────────────────────
        private bool ValidateAction(ChatMessage msg)
        {
            if (msg.Action == null) return true;
            var ok = true;

            if (msg.Action.Type == "add_materiel" && msg.Action.MaterielProposal != null)
            {
                var p = msg.Action.MaterielProposal;
                var isExisting = msg.Action.Label?.StartsWith("exists:") == true;

                if (!isExisting)
                {
                    if (string.IsNullOrWhiteSpace(p.Reference))
                    { msg.FieldErrors["Reference"] = "Obligatoire."; ok = false; }
                    if (string.IsNullOrWhiteSpace(p.Designation))
                    { msg.FieldErrors["Designation"] = "Obligatoire."; ok = false; }
                    if (string.IsNullOrWhiteSpace(p.Categorie))
                    { msg.FieldErrors["Categorie"] = "Obligatoire."; ok = false; }
                }

                if (p.Commande != null)
                {
                    if (string.IsNullOrWhiteSpace(p.Commande.NumeroCommande))
                    { msg.FieldErrors["NumeroCommande"] = "Obligatoire."; ok = false; }
                    if (p.Commande.QuantiteAchetee <= 0)
                    { msg.FieldErrors["Quantite"] = "Doit être > 0."; ok = false; }
                }
                else
                {
                    // Pas de commande → erreur car on en a toujours besoin
                    msg.ValidationErrors.Add("Une commande associée est requise.");
                    ok = false;
                }
            }
            else if (msg.Action.Type == "add_commande" && msg.Action.CommandeProposal != null)
            {
                var p = msg.Action.CommandeProposal;
                if (string.IsNullOrWhiteSpace(p.NumeroCommande))
                { msg.FieldErrors["NumeroCommande"] = "Obligatoire."; ok = false; }
                if (p.MaterielId <= 0)
                { msg.FieldErrors["MaterielId"] = "ID matériel requis."; ok = false; }
                if (p.QuantiteAchetee <= 0)
                { msg.FieldErrors["Quantite"] = "Doit être > 0."; ok = false; }
            }
            else if (msg.Action.Type == "add_article" && msg.Action.ArticleProposal != null)
            {
                var p = msg.Action.ArticleProposal;
                if (p.MaterielId <= 0)
                { msg.FieldErrors["MaterielId"] = "ID matériel requis."; ok = false; }
                if (p.CommandeId <= 0)
                { msg.FieldErrors["CommandeId"] = "ID commande requis."; ok = false; }
            }

            if (!ok && msg.FieldErrors.Count > 0)
            {
                msg.ValidationErrors.Add("Veuillez corriger les erreurs ci-dessous.");
            }
            return ok;
        }

        // ── Ajuster les numéros de série selon la quantité ─────────
        private void AjusterArticlesMsg(ChatMessage msg)
        {
            if (msg.Action?.MaterielProposal?.Commande == null) return;
            AjusterArticlesCommande(msg.Action.MaterielProposal.Commande);
            StateHasChanged();
        }

        private void AjusterArticlesCmdMsg(ChatMessage msg)
        {
            if (msg.Action?.CommandeProposal == null) return;
            var p = msg.Action.CommandeProposal;
            while (p.NumerosSerie.Count < p.QuantiteAchetee) p.NumerosSerie.Add(null);
            while (p.NumerosSerie.Count > p.QuantiteAchetee) p.NumerosSerie.RemoveAt(p.NumerosSerie.Count - 1);
            StateHasChanged();
        }

        private static void AjusterArticlesCommande(AgentCommandeProposal p)
        {
            while (p.NumerosSerie.Count < p.QuantiteAchetee) p.NumerosSerie.Add(null);
            while (p.NumerosSerie.Count > p.QuantiteAchetee) p.NumerosSerie.RemoveAt(p.NumerosSerie.Count - 1);
        }

        // ── Construire les infos matériel depuis le contexte ───────
        private AgentMaterielInfo? BuildMaterielInfoFromContext(string nomMateriel)
        {
            if (string.IsNullOrEmpty(nomMateriel)) return null;
            // Chercher dans les alertes existantes
            var alerte = _alertes.FirstOrDefault(a =>
                a.Designation.Equals(nomMateriel, StringComparison.OrdinalIgnoreCase) ||
                a.Reference.Equals(nomMateriel, StringComparison.OrdinalIgnoreCase));
            if (alerte != null)
            {
                return new AgentMaterielInfo
                {
                    Reference     = alerte.Reference,
                    Designation   = alerte.Designation,
                    Categorie     = alerte.Categorie,
                    QuantiteStock = alerte.QuantiteStock,
                    Unite         = "pièce"
                };
            }
            return new AgentMaterielInfo { Designation = nomMateriel };
        }

        // ── Ouvrir proposition depuis alerte ───────────────────────
        private void OpenAlertProposal(AlerteStock alerte)
        {
            if (alerte.Proposition == null) return;

            _chatStarted = true;

            // S'assurer que la commande est initialisée
            if (alerte.Proposition.Commande == null)
                alerte.Proposition.Commande = new AgentCommandeProposal();
            AjusterArticlesCommande(alerte.Proposition.Commande);

            _messages.Add(new ChatMessage
            {
                IsUser     = false,
                Content    = $"📦 Proposition de réapprovisionnement pour **{alerte.Designation}** (stock: {alerte.QuantiteStock}/{alerte.QuantiteMin}). Veuillez vérifier et approuver :",
                AgentBadge = "action",
                Action     = new AgentAction
                {
                    Type             = "add_materiel",
                    Label            = $"exists:{alerte.MaterielId}",
                    MaterielProposal = alerte.Proposition
                },
                Timestamp = DateTime.Now
            });

            // NE PAS cacher la barre d'alertes — elle reste visible jusqu'à clôture ou traitement
            StateHasChanged();
        }

        // ── Keyboard ───────────────────────────────────────────────
        private async Task OnKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !e.ShiftKey)
                await SendMessage();
        }

        // ── Clear ──────────────────────────────────────────────────
        private async Task ClearChat()
        {
            _messages.Clear();
            _history.Clear();
            _chatStarted = false;
            _showAlertes = _alertes.Count > 0;
        }

        // ── Scroll ─────────────────────────────────────────────────
        private async Task ScrollToBottom()
        {
            try
            {
                await JS.InvokeVoidAsync("eval",
                    "setTimeout(()=>{const c=document.getElementById('ai-messages-container');if(c)c.scrollTop=c.scrollHeight;},50)");
            }
            catch { }
        }

        // ── Helpers UI ─────────────────────────────────────────────
        private bool _estAdmin => _role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;

        private static string GetAgentBadgeClass(string badge) => badge switch
        {
            "web"            => "badge-web",
            "db"             => "badge-db",
            "action"         => "badge-action",
            "action_success" => "badge-success",
            "action_error"   => "badge-error",
            _                => "badge-db"
        };

        private static string GetAgentBadgeIcon(string badge) => badge switch
        {
            "web"            => "🌐",
            "db"             => "🗄️",
            "action"         => "⚡",
            "action_success" => "✅",
            "action_error"   => "❌",
            _                => "🤖"
        };

        private static string GetAgentBadgeLabel(string badge) => badge switch
        {
            "web"            => "Recherche Web",
            "db"             => "Base de données",
            "action"         => "Action proposée",
            "action_success" => "Succès",
            "action_error"   => "Erreur",
            _                => "Agent IA"
        };

        private static string FormatMessage(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\*(.+?)\*", "<em>$1</em>");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"`(.+?)`", "<code>$1</code>");
            text = text.Replace("\n", "<br/>");
            return text;
        }

        private static string Clean(string v)
        {
            v = v.Trim();
            if (v.Length >= 2 &&
                ((v.StartsWith('"') && v.EndsWith('"')) ||
                 (v.StartsWith('\'') && v.EndsWith('\''))))
                v = v[1..^1].Trim();
            return v;
        }
    }
}