using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Interfaces
{
    public interface ISentimentService
    {
        // Analyse les commentaires d'un matériel via Mistral ai
        // et retourne les pourcentages positif/négatif/neutre + résumé.
        Task<SentimentMaterielDto> AnalyserSentimentMaterielAsync(int materielId);
        
        // Analyse tous les matériels qui ont au moins 1 commentaire.
        Task<List<SentimentMaterielDto>> AnalyserTousMaterielAsync();
    }
}
