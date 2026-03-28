// ============================================================
// AssetFlow.Application / Interfaces / ISentimentService.cs
// ============================================================

using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface ISentimentService
    {
        /// <summary>
        /// Analyse les commentaires d'un matériel via Anthropic Claude
        /// et retourne les pourcentages positif/négatif/neutre + résumé.
        /// </summary>
        Task<SentimentMaterielDto> AnalyserSentimentMaterielAsync(int materielId);

        /// <summary>
        /// Analyse tous les matériels qui ont au moins 1 commentaire.
        /// </summary>
        Task<List<SentimentMaterielDto>> AnalyserTousMaterielAsync();
    }
}
