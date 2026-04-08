namespace AssetFlow.BlazorUI.Services
{
    public enum CbState { Closed, Open, HalfOpen }

    /// Circuit indépendant (OCR ou Chat)
    public class OffreCircuit
    {
        private CbState  _state        = CbState.Closed;
        private int      _failures     = 0;
        private DateTime? _openedAt    = null;

        public const int TimeoutSeconds = 20;   // délai avant HalfOpen
        public const int FailureThreshold = 3;  // 3 échecs → OPEN

        public CbState State => _state;

        public int SecondsRemaining
        {
            get
            {
                if (_state != CbState.Open || _openedAt == null) return 0;
                var remaining = TimeoutSeconds - (int)(DateTime.UtcNow - _openedAt.Value).TotalSeconds;
                return remaining > 0 ? remaining : 0;
            }
        }

        /// <summary>Tente la transition Open → HalfOpen si le timeout est expiré.</summary>
        public bool TryTransitionHalfOpen()
        {
            if (_state == CbState.Open &&
                DateTime.UtcNow - _openedAt > TimeSpan.FromSeconds(TimeoutSeconds))
            {
                _state = CbState.HalfOpen;
                return true;
            }
            return false;
        }

        /// <summary>Indique si une requête peut être envoyée.</summary>
        public bool CanSend()
        {
            if (_state == CbState.Open)
            {
                // Vérifier si on peut passer en HalfOpen
                if (DateTime.UtcNow - _openedAt > TimeSpan.FromSeconds(TimeoutSeconds))
                {
                    _state = CbState.HalfOpen;
                    return true;
                }
                return false;
            }
            return true; // Closed ou HalfOpen
        }

        public void RecordSuccess()
        {
            _failures = 0;
            _state    = CbState.Closed;
            _openedAt = null;
        }

        public void RecordFailure()
        {
            _failures++;
            if (_state == CbState.HalfOpen || _failures >= FailureThreshold)
            {
                _state    = CbState.Open;
                _openedAt = DateTime.UtcNow;
                _failures = 0;
            }
        }

        public void Reset()
        {
            _state    = CbState.Closed;
            _failures = 0;
            _openedAt = null;
        }
    }

    /// Service Scoped — contient 2 circuits : OCR et Chatbot.
    public class OffreCircuitBreakerService
    {
        public OffreCircuit Ocr  { get; } = new();
        public OffreCircuit Chat { get; } = new();
    }
}