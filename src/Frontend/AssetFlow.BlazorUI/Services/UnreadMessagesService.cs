namespace AssetFlow.BlazorUI.Services
{
    /// Service singleton partagé entre toutes les pages et sidebars.
    /// Maintient le compteur de messages non lus en temps réel.
    public class UnreadMessagesService
    {
        private int _unreadCount = 0;

        /// <summary>Nombre total de messages non lus pour l'utilisateur courant.</summary>
        public int UnreadCount => _unreadCount;

        /// Déclenché à chaque changement du compteur.
        /// Les sidebars et composants s'y abonnent pour se re-rendre.
        public event Action? OnChanged;

        /// <summary>Initialise le compteur depuis l'API (appelé au démarrage de chaque sidebar).</summary>
        public void Set(int count)
        {
            _unreadCount = Math.Max(0, count);
            OnChanged?.Invoke();
        }

        /// <summary>Incrémente quand un nouveau message non lu arrive via SignalR.</summary>
        public void Increment(int by = 1)
        {
            _unreadCount += by;
            OnChanged?.Invoke();
        }

        /// <summary>Décrémente quand des messages sont lus (sélection d'une conversation).</summary>
        public void Decrement(int by = 1)
        {
            _unreadCount = Math.Max(0, _unreadCount - by);
            OnChanged?.Invoke();
        }

        /// <summary>Remet à zéro (utilisé après avoir tout lu).</summary>
        public void Reset()
        {
            _unreadCount = 0;
            OnChanged?.Invoke();
        }
    }
}