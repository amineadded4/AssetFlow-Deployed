// src/Frontend/AssetFlow.BlazorUI/Pages/Achat/AgentChat.razor.cs
using AssetFlow.BlazorUI.DTOs;
using AssetFlow.BlazorUI.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Text.RegularExpressions;

namespace AssetFlow.BlazorUI.Pages.Achat
{
    public partial class AgentChat : ComponentBase
    {
        [Inject] private AgentChatService     AgentSvc      { get; set; } = default!;
        [Inject] private ConversationService  ConvSvc       { get; set; } = default!;
        [Inject] private IJSRuntime           JS            { get; set; } = default!;
        [Inject] private ILocalStorageService LocalStorage  { get; set; } = default!;
        [Inject] private StockAlertService    StockAlertSvc { get; set; } = default!;
        [Inject] private FournisseurService   FournisseurSvc { get; set; } = default!;

        private List<FournisseurDto> _fournisseurs = new();

        // ── ChatMessage local ────────────────────────────────────────────────
        private class ChatMessage
        {
            public bool          IsUser          { get; set; }
            public string        Content         { get; set; } = string.Empty;
            public string?       AgentBadge      { get; set; }
            public AgentAction?  Action          { get; set; }
            public bool          ActionProcessed { get; set; }
            public DateTime      Timestamp       { get; set; } = DateTime.Now;
            public AgentMaterielInfo? MaterielInfo { get; set; }
            public List<string>              ValidationErrors { get; set; } = new();
            public Dictionary<string,string> FieldErrors      { get; set; } = new();
        }

        private class AgentMaterielInfo
        {
            public string  Reference     { get; set; } = string.Empty;
            public string  Designation   { get; set; } = string.Empty;
            public string? Description   { get; set; }
            public string  Categorie     { get; set; } = string.Empty;
            public int     QuantiteStock { get; set; }
            public string  Unite         { get; set; } = "pièce";
        }

        // ── State chat ───────────────────────────────────────────────────────
        private List<ChatMessage>       _messages        = new();
        private List<AlerteStock>       _alertes         = new();
        private List<AgentChatHistory>  _history         = new();

        private string  _inputText    = string.Empty;
        private bool    _isLoading    = false;
        private bool    _isApproving  = false;
        private bool    _showAlertes  = true;
        private bool    _sidebarOpen  = false;
        private bool    _chatStarted  = false;
        private string  _initiales    = "U";
        private string? _username     = "Utilisateur";
        private string? _role         = "EquipeAchat";
        private int     _userId       = 0;

        // ── State historique ─────────────────────────────────────────────────
        private bool   _historyOpen          = false;
        private bool   _loadingHistory       = false;
        private string? _activeConversationId = null;
        private string  _editingConvId        = string.Empty;
        private string  _editingTitle         = string.Empty;
        private List<ConversationSummary> _conversations = new();

        private readonly List<string> _suggestions = new()
        {
            "📦 Liste mes matériels en alerte",
            "📊 Donne-moi les statistiques de stock",
            "🔍 Recherche des fournisseurs de PC",
            "➕ Ajoute un nouveau matériel laptop",
            "📋 Montre mes dernières commandes",
            "⚠️ Quels incidents sont en attente ?"
        };

        // ── Init ─────────────────────────────────────────────────────────────
        protected override async Task OnInitializedAsync()
        {
            try
            {
                var nom    = await JS.InvokeAsync<string?>("eval", "localStorage.getItem('user_name')");
                var role   = await JS.InvokeAsync<string?>("eval", "localStorage.getItem('user_role')");
                var userIdStr = await JS.InvokeAsync<string?>("eval", "localStorage.getItem('user_id')");

                if (!string.IsNullOrWhiteSpace(nom))  _username = Clean(nom);
                if (!string.IsNullOrWhiteSpace(role)) _role     = Clean(role);
                if (!string.IsNullOrWhiteSpace(userIdStr))
                    int.TryParse(Clean(userIdStr), out _userId);

                var parts = (_username ?? "U").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                _initiales = parts.Length >= 2
                    ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                    : (_username ?? "U")[..Math.Min(2, (_username ?? "U").Length)].ToUpper();
            }
            catch { }

            await LoadInitialAlerts();

            try { _fournisseurs = await FournisseurSvc.GetAllAsync(); }
            catch { }

            // Charger la liste des conversations depuis Redis
            if (_userId > 0)
                await LoadConversationList();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await ScrollToBottom();
        }

        // ── Alertes stock ────────────────────────────────────────────────────
        private async Task LoadInitialAlerts()
        {
            var resp = await AgentSvc.GetInitialAlertsAsync();
            if (resp == null) return;

            _alertes     = resp.Alertes;
            _showAlertes = _alertes.Count > 0;
            StockAlertSvc.Set(_alertes.Count);
        }

        // ════════════════════════════════════════════════════════════════════
        //  HISTORIQUE — méthodes
        // ════════════════════════════════════════════════════════════════════

        private void ToggleHistory()
        {
            _historyOpen = !_historyOpen;
        }

        private void CloseAllOverlays()
        {
            _sidebarOpen = false;
            _historyOpen = false;
        }

        private async Task LoadConversationList()
        {
            if (_userId == 0) return;
            _loadingHistory = true;
            StateHasChanged();

            _conversations = await ConvSvc.GetListAsync(_userId);
            _loadingHistory = false;
            StateHasChanged();
        }

        private async Task LoadConversation(string convId)
        {
            _activeConversationId = convId;
            _historyOpen          = false;
            _isLoading            = true;
            StateHasChanged();

            // Récupérer les messages depuis Redis
            var msgs = await ConvSvc.GetMessagesAsync(convId);

            _messages.Clear();
            _history.Clear();

            foreach (var m in msgs)
            {
                _messages.Add(new ChatMessage
                {
                    IsUser          = m.Role == "user",
                    Content         = m.Content,
                    AgentBadge      = m.AgentUsed,
                    ActionProcessed = true, // les actions déjà traitées ne sont plus actives
                    Timestamp       = m.Timestamp
                });
                _history.Add(new AgentChatHistory { Role = m.Role, Content = m.Content });
            }

            _chatStarted = _messages.Count > 0;
            _isLoading   = false;
            StateHasChanged();
            await ScrollToBottom();
        }

        private async Task StartNewConversation()
        {
            _messages.Clear();
            _history.Clear();
            _chatStarted          = false;
            _activeConversationId = null;
            _historyOpen          = false;
            StateHasChanged();
        }

        // ── Groupement temporel des conversations (comme Claude) ─────────────
        private Dictionary<string, List<ConversationSummary>> GetGroupedConversations()
        {
            var now    = DateTime.UtcNow;
            var result = new Dictionary<string, List<ConversationSummary>>();

            foreach (var conv in _conversations)
            {
                var diff = now - conv.UpdatedAt;
                string group;

                if (diff.TotalHours < 24)
                    group = "Aujourd'hui";
                else if (diff.TotalDays < 2)
                    group = "Hier";
                else if (diff.TotalDays <= 7)
                    group = "Cette semaine";
                else if (diff.TotalDays <= 30)
                    group = "Ce mois-ci";
                else
                    group = conv.UpdatedAt.ToString("MMMM yyyy");

                if (!result.ContainsKey(group))
                    result[group] = new List<ConversationSummary>();
                result[group].Add(conv);
            }

            return result;
        }

        // ── Titre ────────────────────────────────────────────────────────────
        private void StartEditTitle(string convId, string currentTitle)
        {
            _editingConvId = convId;
            _editingTitle  = currentTitle;
        }

        private async Task SaveTitle(string convId)
        {
            if (!string.IsNullOrWhiteSpace(_editingTitle))
            {
                await ConvSvc.UpdateTitleAsync(convId, _editingTitle);
                var conv = _conversations.FirstOrDefault(c => c.Id == convId);
                if (conv != null) conv.Title = _editingTitle;
            }
            _editingConvId = string.Empty;
            StateHasChanged();
        }

        private async Task OnTitleKeyDown(KeyboardEventArgs e, string convId)
        {
            if (e.Key == "Enter")  await SaveTitle(convId);
            if (e.Key == "Escape") _editingConvId = string.Empty;
        }

        // ── Supprimer ────────────────────────────────────────────────────────
        private async Task DeleteConversation(string convId)
        {
            await ConvSvc.DeleteAsync(convId, _userId);
            _conversations.RemoveAll(c => c.Id == convId);

            if (_activeConversationId == convId)
                await StartNewConversation();

            StateHasChanged();
        }

        private async Task DeleteAllConversations()
        {
            await ConvSvc.DeleteAllAsync(_userId);
            _conversations.Clear();
            await StartNewConversation();
        }

        // ── Générer un titre automatique depuis le premier message ───────────
        private static string GenerateTitleFromMessage(string msg)
        {
            var cleaned = msg.Replace("📦", "").Replace("📊", "").Replace("🔍", "")
                            .Replace("➕", "").Replace("📋", "").Replace("⚠️", "").Trim();
            return cleaned.Length > 40 ? cleaned[..37] + "..." : cleaned;
        }

        // ════════════════════════════════════════════════════════════════════
        //  ENVOI DE MESSAGE
        // ════════════════════════════════════════════════════════════════════

        private async Task SendMessage()
        {
            var text = _inputText.Trim();
            if (string.IsNullOrEmpty(text) || _isLoading) return;

            // Créer la conversation dans Redis au premier message
            if (!_chatStarted && _userId > 0)
            {
                var title  = GenerateTitleFromMessage(text);
                var created = await ConvSvc.CreateAsync(_userId, title);
                if (created != null)
                {
                    _activeConversationId = created.ConversationId;
                    _conversations.Insert(0, new ConversationSummary
                    {
                        Id        = created.ConversationId,
                        Title     = title,
                        CreatedAt = created.CreatedAt,
                        UpdatedAt = created.CreatedAt
                    });
                }
            }

            _chatStarted = true;

            _messages.Add(new ChatMessage { IsUser = true, Content = text, Timestamp = DateTime.Now });
            _history.Add(new AgentChatHistory { Role = "user", Content = text });
            _inputText = string.Empty;
            _isLoading = true;
            StateHasChanged();
            await ScrollToBottom();

            // Persister le message user dans Redis
            if (_activeConversationId != null)
                await ConvSvc.AddMessageAsync(_activeConversationId, "user", text);

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

                if (resp.Action?.Type == "add_article" && resp.Action.ArticleProposal != null)
                    botMsg.MaterielInfo = BuildMaterielInfoFromContext(resp.Action.ArticleProposal.NomMateriel);

                if (resp.Action?.Type == "add_materiel" && resp.Action.MaterielProposal?.Commande != null)
                    AjusterArticlesCommande(resp.Action.MaterielProposal.Commande);

                _messages.Add(botMsg);
                _history.Add(new AgentChatHistory { Role = "assistant", Content = resp.Message });

                // Persister la réponse assistant dans Redis
                if (_activeConversationId != null)
                    await ConvSvc.AddMessageAsync(_activeConversationId, "assistant",
                        resp.Message, agentUsed: resp.AgentUsed);

                // Mettre à jour la preview dans la liste locale
                var convItem = _conversations.FirstOrDefault(c => c.Id == _activeConversationId);
                if (convItem != null)
                {
                    convItem.UpdatedAt   = DateTime.UtcNow;
                    convItem.LastMessage = resp.Message.Length > 60
                        ? resp.Message[..57] + "..." : resp.Message;
                    convItem.MessageCount += 2;
                }

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

        // ── Approbation d'action ─────────────────────────────────────────────
        private async Task ApproveAction(ChatMessage msg, bool approved)
        {
            if (msg.Action == null) return;

            if (approved)
            {
                msg.ValidationErrors.Clear();
                msg.FieldErrors.Clear();
                if (!ValidateAction(msg)) { StateHasChanged(); return; }
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
            _isApproving        = false;
            msg.ActionProcessed = true;

            var resultMsg = resp?.Message ?? (approved ? "✅ Action effectuée." : "❌ Action annulée.");
            _messages.Add(new ChatMessage
            {
                IsUser     = false,
                Content    = resultMsg,
                AgentBadge = resp?.Succes == true ? "action_success" : "action_error",
                Timestamp  = DateTime.Now
            });

            // Persister le résultat dans Redis
            if (_activeConversationId != null)
                await ConvSvc.AddMessageAsync(_activeConversationId, "assistant", resultMsg,
                    agentUsed: resp?.Succes == true ? "action_success" : "action_error",
                    actionProcessed: true);

            if (resp?.Succes == true)
            {
                var refConcernee = msg.Action.MaterielProposal?.Reference;
                if (!string.IsNullOrEmpty(refConcernee))
                {
                    _alertes.RemoveAll(a =>
                        a.Reference.Equals(refConcernee, StringComparison.OrdinalIgnoreCase));
                    if (_alertes.Count == 0) _showAlertes = false;
                }
                await LoadInitialAlerts();
            }

            StateHasChanged();
            await ScrollToBottom();
        }

        // ── Validation ───────────────────────────────────────────────────────
        private bool ValidateAction(ChatMessage msg)
        {
            if (msg.Action == null) return true;
            var ok = true;

            if (msg.Action.Type == "add_materiel" && msg.Action.MaterielProposal != null)
            {
                var p          = msg.Action.MaterielProposal;
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
                else { msg.ValidationErrors.Add("Une commande associée est requise."); ok = false; }
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
                msg.ValidationErrors.Add("Veuillez corriger les erreurs ci-dessous.");
            return ok;
        }

        // ── Helpers articles ─────────────────────────────────────────────────
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

        private AgentMaterielInfo? BuildMaterielInfoFromContext(string nomMateriel)
        {
            if (string.IsNullOrEmpty(nomMateriel)) return null;
            var alerte = _alertes.FirstOrDefault(a =>
                a.Designation.Equals(nomMateriel, StringComparison.OrdinalIgnoreCase) ||
                a.Reference.Equals(nomMateriel, StringComparison.OrdinalIgnoreCase));
            return alerte != null
                ? new AgentMaterielInfo { Reference = alerte.Reference, Designation = alerte.Designation,
                    Categorie = alerte.Categorie, QuantiteStock = alerte.QuantiteStock, Unite = "pièce" }
                : new AgentMaterielInfo { Designation = nomMateriel };
        }

        private void OpenAlertProposal(AlerteStock alerte)
        {
            if (alerte.Proposition == null) return;
            _chatStarted = true;
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
            StateHasChanged();
        }

        // ── Keyboard / scroll ────────────────────────────────────────────────
        private async Task OnKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !e.ShiftKey) await SendMessage();
        }

        private async Task ScrollToBottom()
        {
            try
            {
                await JS.InvokeVoidAsync("eval",
                    "setTimeout(()=>{const c=document.getElementById('ai-messages-container');if(c)c.scrollTop=c.scrollHeight;},50)");
            }
            catch { }
        }

        // ── Helpers UI ───────────────────────────────────────────────────────
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
            text = Regex.Replace(text,
                @"\[([^\]]+)\]\((https?://[^\)]+)\)",
                "<a href=\"$2\" target=\"_blank\" rel=\"noopener noreferrer\" class=\"ai-link\">$1</a>");
            text = Regex.Replace(text, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            text = Regex.Replace(text, @"\*(.+?)\*", "<em>$1</em>");
            text = Regex.Replace(text, @"`(.+?)`", "<code>$1</code>");
            text = Regex.Replace(text, @"^## (.+)$", "<div class=\"ai-sources-title\">$1</div>", RegexOptions.Multiline);
            text = Regex.Replace(text, @"^- (.+)$", "<div class=\"ai-list-item\">$1</div>", RegexOptions.Multiline);
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